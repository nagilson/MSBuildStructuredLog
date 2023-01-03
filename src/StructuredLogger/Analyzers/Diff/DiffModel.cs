using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Logging.StructuredLogger;
using StructuredLogViewer;
using static StructuredLogger.Analyzers.Diff.DiffModel;

namespace StructuredLogger.Analyzers.Diff
{
    public class DiffModel
    {
        /// <summary>
        /// Similar to the Build class, but with dictionaries so differences can be efficiently computed. 
        /// </summary>
        private struct DiffableBuild
        {
            public Build _build;
            public Dictionary<Project, ProjectData> Projects = new Dictionary<Project, ProjectData>(new ProjectComparer());
            public Dictionary<string, string> Environment = new Dictionary<string, string>();

            public DiffableBuild(Build build)
            {
                _build = build;
            }
        }

        private struct ProjectData
        {
            public Dictionary<string, string> Properties = new Dictionary<string, string>();
            public Dictionary<string, string> Globals = new Dictionary<string, string>();

            public ProjectData()
            {
            }
        }


        /// <summary>
        /// Used to compare projects across binlogs, which may not have the same ID but will technically be the same.
        /// It is a best effort approach to find a match based on project name and global properties.
        /// </summary>
        private struct ProjectComparer : IEqualityComparer<Project>
        {

            private bool _UseProjectPathsOverNamesToIdentify; // probably get rid of this

            public ProjectComparer(bool useProjectPathsOverNamesToIdentifier = false)
            {
                _UseProjectPathsOverNamesToIdentify = useProjectPathsOverNamesToIdentifier;
            }

            private bool AreEquivalentDictionariesAssumingKVImplementComparator<K, V>(IDictionary<K, V> dict1, IDictionary<K, V> dict2)
            {
                return dict1.Count == dict2.Count && !dict1.Except(dict2).Any();
            }

            public bool Equals(Project x, Project y) // This compares projects across machines/binlogs, must be ID agnostic
            {
                if (x.Name == y.Name)
                {
                    if (AreEquivalentDictionariesAssumingKVImplementComparator(x.GlobalProperties, y.GlobalProperties))
                    {
                        if (_UseProjectPathsOverNamesToIdentify)
                        {
                            return x.ProjectFile == y.ProjectFile;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public int GetHashCode(Project obj)
            {
                if (!_UseProjectPathsOverNamesToIdentify)
                {
                    return $"{obj.Name + obj.GlobalProperties.Count()}".GetHashCode();
                }
                else
                {
                    return $"{obj.ProjectDirectory + obj.Name + obj.GlobalProperties.Count()}".GetHashCode();
                }
            }
        }

        /// <summary>
        /// Used to ID tasks across different binlogs where the ID might not be the same despite the task call being the same.
        /// A best effort approach.
        /// </summary>
        private struct TaskComparer : IEqualityComparer<Task>
        {
            public TaskComparer(bool useProjectPathsOverNamesToIdentifier = false)
            {
            }

            public bool Equals(Task x, Task y) // This compares projects across machines/binlogs, must be ID agnostic
            {
                if (x.Name == y.Name)
                {
                    if (((Target)x.Parent).Name == ((Target)y.Parent).Name)
                    {
                        var xGlobals = x.FindChild<NamedNode>(n => n.Name == "Global Properties");
                        var yGlobals = y.FindChild<NamedNode>(n => n.Name == "Global Properties");
                        if (xGlobals == yGlobals)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public int GetHashCode(Task obj)
            {
                return $"{obj.Name}".GetHashCode();
            }
        }

        /// <summary>
        /// The over-arching data storage mechanism containing all of the diff information.
        /// </summary>
        public struct BuildDifference
        {
            public List<Difference<Tuple<string, string>>> environmentDifference = new();
            public string binlogAName;
            public string binlogBName;
            public Dictionary<Project, ProjectDifference> projectDifferences = new Dictionary<Project, ProjectDifference>(new ProjectComparer());

            public BuildDifference()
            {
            }
        }

        /// <summary>
        /// Stores the property differences and (soon) target differences between projects.
        /// </summary>
        public struct ProjectDifference
        {
            public List<Difference<Tuple<string, string>>> propertyDifference = new();
            public List<Difference<Tuple<string, string>>> globalDifference = new(); // This is currently useless because globalproperties are used to ID projects. They will show up as distinct projects with a _soleOwner if differences exist.

            public string _soleOwner;

            public ProjectDifference()
            {

            }

            public ProjectDifference(string soleOwner)
            {
                propertyDifference = null;
                _soleOwner = soleOwner;
            }
        }

        /// <summary>
        /// A wrapper that stores the same data from one binlog and another that is different across the two. The members may be null if undefined or null in the other binlog. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public struct Difference<T>
        {
            public T binlogAValue;
            public T binlogBValue;

            public Difference(T a, T b)
            {
                binlogAValue = a;
                binlogBValue = b;
            }

        }

        private DiffableBuild _firstBuildReference;
        private DiffableBuild _secondBuildReference;
        public BuildDifference difference = new BuildDifference();

        /// <summary>
        /// Analyzes a set of two builds and puts their differences into a searchable data type of BuildDifference.
        /// </summary>
        /// <param name="buildsForDiff">A list containing two builds to diff in arbitrary order.</param>
        public DiffModel(List<Build> buildsForDiff)
        {
            Debug.Assert(buildsForDiff != null);
            Debug.Assert(buildsForDiff.Count == 2);

            _firstBuildReference = new DiffableBuild(buildsForDiff.First());
            _secondBuildReference = new DiffableBuild(buildsForDiff.ElementAt(1));

            BuildAnalyzer.AnalyzeBuild(_firstBuildReference._build);
            BuildAnalyzer.AnalyzeBuild(_secondBuildReference._build);

            DiscoverAllProjects(_firstBuildReference);
            DiscoverAllProjects(_secondBuildReference);

            difference.binlogAName = _firstBuildReference._build.LogFilePath;
            difference.binlogBName = _secondBuildReference._build.LogFilePath;

            FindDifferences();
        }

        /// <summary>
        /// Finds all of the projects from a binlog using a Depth First Search of MSBuild Tasks.
        /// </summary>
        /// <param name="buildProjectStore">The build object to put the discovered projects into for later work.</param>
        void DiscoverAllProjects(DiffableBuild buildProjectStore)
        {
            var topLevelProjects = buildProjectStore._build.Children.OfType<Project>();
            var visitedProjectIds = new HashSet<int>();
            var projectsToVisit = new Stack<Project>();

            foreach (var project in topLevelProjects)
            {
                projectsToVisit.Push(project);
            }

            while (projectsToVisit.Any())
            {
                var projectToVisit = projectsToVisit.Pop();
                buildProjectStore.Projects[projectToVisit] = new ProjectData();
                visitedProjectIds.Add(projectToVisit.EvaluationId);
                ProjectEvaluation projectData = buildProjectStore._build.FindEvaluation(projectToVisit.EvaluationId);
                var MSBuildTasks = projectToVisit.FindChildrenRecursive<NamedNode>(n => n.Name == "MSBuild" && n.TypeName == "Task");
                foreach (var msbuildInvoke in MSBuildTasks)
                {
                    var discoveredProjects = msbuildInvoke.Children.OfType<Project>().ToList();
                    foreach (var project in discoveredProjects == null ? new List<Project>() : discoveredProjects)
                    {
                        if (!visitedProjectIds.Contains(project.EvaluationId) && !projectsToVisit.Any(p => p.EvaluationId == project.EvaluationId))
                        {
                            projectsToVisit.Push(project);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Invokes all of the collection functions for different types of differences.
        /// </summary>
        void FindDifferences()
        {
            CollectPropertyAndEnvironmentDiff();
            CollectTaskDiff();
            CollectTargetDiff();
        }

        void CollectPropertyAndEnvironmentDiff()
        {
            const string grabAllPropertiesQuery = "$property";

            // First, find all of the properties 
            var firstBinlogProperties = ExecuteSearch(_firstBuildReference._build, grabAllPropertiesQuery);
            var secondBinlogProperties = ExecuteSearch(_secondBuildReference._build, grabAllPropertiesQuery);

            // Next, map the properties to their respective projects or environment
            foreach (var property in firstBinlogProperties)
            {
                PopulateBuildWithPropertyInfo(_firstBuildReference, property);
            }
            foreach (var property in secondBinlogProperties)
            {
                PopulateBuildWithPropertyInfo(_secondBuildReference, property);
            }

            // Finally, collect the diff
            ParsePropertyDifference(_firstBuildReference.Environment, _secondBuildReference.Environment, difference.environmentDifference);
            ParseProjectPropertyDifference(false);
            ParseProjectPropertyDifference(true);
        }

        void ParseProjectPropertyDifference(bool parseLocalProperties)
        {
            foreach (var kvp in _firstBuildReference.Projects)
            {
                Project projectReference = kvp.Key;
                ProjectData projectProperties = kvp.Value;
                ProjectData otherProjectProperties;
                if (_secondBuildReference.Projects.TryGetValue(projectReference, out otherProjectProperties))
                {
                    if (!difference.projectDifferences.ContainsKey(projectReference))
                    {
                        difference.projectDifferences[projectReference] = new ProjectDifference();
                    }
                    ParsePropertyDifference(
                        parseLocalProperties ? projectProperties.Properties : projectProperties.Globals,
                        parseLocalProperties ? otherProjectProperties.Properties : otherProjectProperties.Globals,
                        parseLocalProperties ? difference.projectDifferences[projectReference].propertyDifference : difference.projectDifferences[projectReference].globalDifference
                    );
                }
                else // The project doesn't exist in the other binlog.
                {
                    // Create a project difference with empty lists indicating with binlog owns it.
                    difference.projectDifferences[projectReference] = new ProjectDifference(_firstBuildReference._build.LogFilePath);
                }
            }

            foreach (var kvp in _secondBuildReference.Projects)
            {
                if (!difference.projectDifferences.ContainsKey(kvp.Key))
                {
                    difference.projectDifferences[kvp.Key] = new ProjectDifference(_secondBuildReference._build.LogFilePath);
                }
            }
        }

        void ParsePropertyDifference(Dictionary<string, string> PropertySetA, Dictionary<string, string> PropertySetB, List<Difference<Tuple<string, string>>> differenceStorage)
        {
            foreach (var kvp in PropertySetA)
            {
                var propertyName = kvp.Key;
                var propertyValue = kvp.Value;
                string otherValue;
                if (PropertySetB.TryGetValue(propertyName, out otherValue))
                {
                    if (otherValue != propertyValue)
                    {
                        differenceStorage.Add(
                            new Difference<Tuple<string, string>>(
                                new Tuple<string, string>(propertyName, propertyValue),
                                new Tuple<string, string>(propertyName, otherValue)
                            )
                       );
                    }
                    else // If the items are identical, they don't belong in the diff. We remove it to prevent searching again when we scan the other binlog.
                    {
                        PropertySetB.Remove(propertyName);
                    }
                }
                else // The key doesn't exist in the other binlog.
                {
                    differenceStorage.Add(
                          new Difference<Tuple<string, string>>(
                              new Tuple<string, string>(propertyName, propertyValue),
                              null
                          )
                     );
                }
            }

            foreach (var kvp in PropertySetB)
            {
                // We already removed all similarities.
                // All that's left will be a diff where the key doesn't exist in the other binlog.
                differenceStorage.Add(
                  new Difference<Tuple<string, string>>(
                      null,
                      new Tuple<string, string>(kvp.Key, kvp.Value)
                  )
             );
            }
        }

        void PopulateBuildWithPropertyInfo(DiffableBuild build, SearchResult propertyResult)
        {
            BaseNode iterParentNode = propertyResult.Node;
            while (iterParentNode.Parent != null)
            {
                iterParentNode = iterParentNode.Parent;
                if (iterParentNode.GetType().GetProperty("Name") != null)
                {
                    var namedNode = (NamedNode)iterParentNode;
                    if (namedNode.TypeName == "ProjectEvaluation")
                    {
                        var propertyNode = (Property)propertyResult.Node;
                        var ghostProject = GhostProject(build, (ProjectEvaluation)iterParentNode);
                        if (ghostProject == null)
                        {
                            return;
                        }
                        build.Projects[ghostProject].Properties[propertyNode.Name] = propertyNode.Value;
                        return;
                    }
                    else if (namedNode.Name == "Environment")
                    {
                        var propertyNode = (Property)propertyResult.Node;
                        build.Environment[propertyNode.Name] = propertyNode.Value;
                        return;
                    }
                    else if (namedNode.Name == "Global")
                    {
                        var propertyNode = (Property)propertyResult.Node;
                        ProjectEvaluation projectEvalOwner = iterParentNode.GetNearestParent<ProjectEvaluation>();
                        if (projectEvalOwner != null) // Global properties can be owned by a "Project" Node or a "ProjectEvaluation" Node
                        {
                            var ghostProject = GhostProject(build, projectEvalOwner);
                            if (ghostProject == null)
                            {
                                return;
                            }
                            build.Projects[ghostProject].Globals[propertyNode.Name] = propertyNode.Value;
                        }
                        else // The global property belongs to a Project node. (Did it not get evaluated due to msbuild error, or maybe this is a distinct node?)
                        {
                            Project projectOwner = iterParentNode.GetNearestParent<Project>();
                            if (projectOwner != null)
                            {
                                build.Projects[projectOwner].Globals[propertyNode.Name] = propertyNode.Value;
                            }
                        }
                        return;
                    }
                }
                else
                {
                    Debug.Assert(false); // we don't expect to get here: how could a property not belong to a project or env var?
                    return;
                }
            }
            return;
        }

        /// <summary>
        /// Return a reference to an existing project class for a given project evaluation based on that evaluation project's ID. 
        /// </summary>
        /// <param name="build">The build that should contain the project that was evaluated</param>
        /// <param name="evaluatedProject">The evaluated project node</param>
        /// <returns>The project that was evaluated by the evaluated project node.</returns>
        Project GhostProject(DiffableBuild build, ProjectEvaluation evaluatedProject)
        {
            var ghost = build.Projects.Where(proj => proj.Key.EvaluationText == evaluatedProject.EvaluationText).FirstOrDefault().Key;
            if (ghost == null)
            {
                Debug.Assert(false, $"A matching project could not be found for the id {evaluatedProject.EvaluationText}." +
                    $"Build Projects contains: {(from p in build.Projects.Keys select p.EvaluationText).ToList()}");
            }
            return ghost;
        }

        IEnumerable<SearchResult> ExecuteSearch(Build build, string query)
        {
            var search = new Search(
                 new[] { build },
                 build.StringTable.Instances,
                 int.MaxValue,
                 false
              );
            return search.FindNodes(query, CancellationToken.None);
        }

        void CollectTargetDiff()
        {

        }

        void CollectTaskDiff()
        {
            foreach (var kvp in _firstBuildReference.Projects)
            {
                Project projectReference = kvp.Key;
                if (_secondBuildReference.Projects.ContainsKey(projectReference))
                {
                    Project twinProject = _secondBuildReference.Projects.Keys.Where(k => new ProjectComparer().Equals(k, projectReference)).First();

                    var AllTasks = projectReference.FindChildrenRecursive<Task>();
                    foreach (var task in AllTasks)
                    {
                        var twinTask = twinProject.FindChild<Task>(t => new TaskComparer().Equals(t, task));
                    }
                }
            }
        }


    }
}
