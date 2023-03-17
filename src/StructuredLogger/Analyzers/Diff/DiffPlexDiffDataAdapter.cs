using System;
using System.Collections.Generic;
using System.Text;
using static StructuredLogger.Analyzers.Diff.DiffModel;

namespace StructuredLogger.Analyzers.Diff
{
    public class DiffPlexDiffDataAdapter : BinlogDiffDataAdapter<BuildDifference, List<Tuple<string, string>>>
    {
        private const string undefined = "*undefined*";
        private const string trueNull = "*null*";
        private const string environment = "Environment";
        private const string projectExists = "Project Exists.";
        private const string projectUnavailable = "Project doesn't exist.";

        public DiffPlexDiffDataAdapter(bool useFilter)
        {
            this.useFilter = useFilter;
            filter = new LinchpinHeuristicDiffFilter();
        }

        public override List<Tuple<string, string>> Adapt(BuildDifference buildDifference)
        {
            List<Tuple<string, string>> diffWindows = new();

            // Environment diffs

            StringBuilder flatDiffContentA = new();
            StringBuilder flatDiffContentB = new();

            flatDiffContentA.AppendLine(environment);
            flatDiffContentB.AppendLine(environment);

            foreach (var kvp in buildDifference.environmentDifference)
            {
                WriteDifference(flatDiffContentA, flatDiffContentB, kvp);
            }

            diffWindows.Add(new Tuple<string, string>(flatDiffContentA.ToString(), flatDiffContentB.ToString()));
            flatDiffContentA = new();
            flatDiffContentB = new();
            GC.Collect();

            // Project diffs

            foreach (var kvp in buildDifference.projectDifferences)
            {
                string projectId = $"{kvp.Key.Name} {kvp.Key.TargetFramework}  {kvp.Key.Configuration}";
                flatDiffContentA.AppendLine(projectId);
                flatDiffContentB.AppendLine(projectId);

                var propertyDifference = kvp.Value.propertyDifference;
                if (propertyDifference == null)
                {
                    if (kvp.Value._soleOwner == buildDifference.binlogAName)
                    {
                        flatDiffContentA.AppendLine(projectExists);
                        flatDiffContentB.AppendLine(projectUnavailable);
                    }
                    else
                    {
                        flatDiffContentA.AppendLine(projectUnavailable);
                        flatDiffContentB.AppendLine(projectExists);
                    }
                }
                else
                {
                    foreach (var propDiff in propertyDifference)
                    {
                        WriteDifference(flatDiffContentA, flatDiffContentB, propDiff);
                    }
                }
                diffWindows.Add(new Tuple<string, string>(flatDiffContentA.ToString(), flatDiffContentB.ToString()));
                flatDiffContentA = new();
                flatDiffContentB = new();
            }

            return diffWindows;
        }

        private void WriteDifference(StringBuilder stringA, StringBuilder stringB, Difference<Tuple<string, string>> d)
        {
            var firstEnvVarKey = d.binlogAValue?.Item1;
            var secondEnvVarKey = d.binlogBValue?.Item1;

            var firstEnvVarValue = d.binlogAValue?.Item2;
            var secondEnvVarVar = d.binlogBValue?.Item2;

            if (!useFilter || (useFilter && ((LinchpinHeuristicDiffFilter)filter).ShouldIncludeInDiff(d.binlogAValue)))
            {
                stringA.AppendLine($"{firstEnvVarKey ?? undefined}");
                stringA.AppendLine($"\t{firstEnvVarValue ?? trueNull}");

                stringB.AppendLine($"{secondEnvVarKey ?? undefined}");
                stringB.AppendLine($"\t{secondEnvVarVar ?? trueNull}");
            }
        }
    }
}
