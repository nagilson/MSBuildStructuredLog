using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Logging.StructuredLogger;
using StructuredLogViewer;

namespace StructuredLogger.BinaryLogger
{
    public class BinaryLogDiffAnalyzer
    {
        private struct DiffableBuild
        {
            public Build _build;
            public Dictionary<Project, ProjectProperties> Projects = new Dictionary<Project, ProjectProperties>(new ProjectComparer());
            public Dictionary<string, string> Environment = new Dictionary<string, string>();
            public Dictionary<string, string> Globals = new Dictionary<string, string>();

            public DiffableBuild(Build build)
            {
                _build = build;
            }
        }

        private struct ProjectProperties
        {
            public Dictionary<string, string> Properties = new Dictionary<string, string>();

            public ProjectProperties()
            {
            }
        }

        /// <summary>
        ///  If there are 2+ projects with the same name, we can use the path.
        /// We don't want to avoid diffing two projects just because their path is different unless necessary.
        /// </summary>
        private struct ProjectComparer : IEqualityComparer<Project>
        {

            private bool _UseProjectPathsOverNamesToIdentifier;

            public ProjectComparer(bool _UseProjectPathsOverNamesToIdentifier = false)
            {
                // TODO: intelligent logic for _USEPPONTI
            }    

            public bool Equals(Project x, Project y)
            {
                if(x.Name == y.Name)
                {
                    if(x.TargetsText == y.TargetsText)
                    {
                        _UseProjectPathsOverNamesToIdentifier = true;
                        if (_UseProjectPathsOverNamesToIdentifier)
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
                if (!_UseProjectPathsOverNamesToIdentifier)
                {
                    return $"{obj.Name + obj.TargetsText}".GetHashCode();
                }
                else
                {
                    return $"{obj.ProjectDirectory + obj.Name + obj.TargetsText}".GetHashCode();
                }
            }
        }

        private struct BuildDifference
        {
        }

        private List<Build> _buildsForDiff = new List<Build>();
        private DiffableBuild _firstBuildReference;
        private DiffableBuild _secondBuildReference;

        public BinaryLogDiffAnalyzer(List<Build> buildsForDiff)
        {
            Debug.Assert(buildsForDiff != null);
            Debug.Assert(buildsForDiff.Count == 2);

            _buildsForDiff = buildsForDiff;

            _firstBuildReference = new DiffableBuild(_buildsForDiff.First());
            _secondBuildReference = new DiffableBuild(_buildsForDiff.ElementAt(1));

            BuildAnalyzer.AnalyzeBuild(_firstBuildReference._build);
            BuildAnalyzer.AnalyzeBuild(_secondBuildReference._build);

            _firstBuildReference.Projects = (from project in _firstBuildReference._build.Children.OfType<Project>() select project)
                .ToDictionary(
                    p => p,
                    p => new ProjectProperties()
                );
            _secondBuildReference.Projects = (from project in _secondBuildReference._build.Children.OfType<Project>() select project)
                .ToDictionary(
                    p => p,
                    p => new ProjectProperties()
                );

            FindDifferences();
        }

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
            var secondBinlogProperties = ExecuteSearch(_firstBuildReference._build, grabAllPropertiesQuery);

            // Next, map the properties to their respective projects or environment
            foreach(var property in firstBinlogProperties)
            {
                PopulateBuildWithPropertyInfo(_firstBuildReference, property);
            }
            foreach (var property in secondBinlogProperties)
            {
                PopulateBuildWithPropertyInfo(_firstBuildReference, property);
            }

            // Finally, collect the diff. you can erase proprety from the dictionary if same, else add it to diff class. anything remaining in the other dictionary will be a diff also.
            // everything not in the diff would be the same after.


        }

        void PopulateBuildWithPropertyInfo(DiffableBuild build, SearchResult propertyResult)
        {
            BaseNode iterParentNode = propertyResult.Node;
            while (iterParentNode.Parent != null)
            {
                iterParentNode = iterParentNode.Parent;
                if (iterParentNode.GetType().GetProperty("Name") != null)
                {
                    NamedNode namedNode = (NamedNode)iterParentNode;
                    if (namedNode.TypeName == "ProjectEvaluation")
                    {
                        var propertyNode = (Property)propertyResult.Node;
                        build.Projects[GhostProject(build, (ProjectEvaluation)iterParentNode)].Properties[propertyNode.Name] = propertyNode.Value;
                        return;
                    }
                    else if(namedNode.Name == "Environment") // TODO: handle the bottom two cases
                    {
                        var propertyNode = (Property)propertyResult.Node;
                        build.Environment[propertyNode.Name] = propertyNode.Value;
                        return;
                    }
                    else if (namedNode.Name == "Global")
                    {
                        var propertyNode = (Property)propertyResult.Node;
                        build.Globals[propertyNode.Name] = propertyNode.Value;
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

        Project GhostProject(DiffableBuild build, ProjectEvaluation evaluatedProject)
        {
            return build.Projects.Where(proj => proj.Key.EvaluationText == evaluatedProject.EvaluationText).FirstOrDefault().Key;
        }

        IEnumerable<SearchResult> ExecuteSearch(Build build, string query)
        {
           var search = new Search(
                new[] { build },
                build.StringTable.Instances,
                5000,
                false
             );
            return search.FindNodes(query, CancellationToken.None);
        }

        void CollectTargetDiff()
        {

        }

        void CollectTaskDiff()
        {

        }

    }
}
