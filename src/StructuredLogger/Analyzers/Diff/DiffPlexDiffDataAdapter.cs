using System;
using System.Collections.Generic;
using System.Text;
using static StructuredLogger.Analyzers.Diff.DiffModel;

namespace StructuredLogger.Analyzers.Diff
{
    public class DiffPlexDiffDataAdapter : BinlogDiffDataAdapter<BuildDifference, List<Tuple<string, string>>>
    {
        public DiffPlexDiffDataAdapter()
        {
            filter = new LinchpinHeuristicDiffFilter();
        }

        public override List<Tuple<string, string>> Adapt(BuildDifference buildDifference)
        {
            List<Tuple<string, string>> diffWindows = new();

            // environment setup
            StringBuilder firstEnvironmentDiffFlat = new();
            StringBuilder secondEnvironmentDiffFlat = new();

            firstEnvironmentDiffFlat.AppendLine("Environment");
            secondEnvironmentDiffFlat.AppendLine("Environment");

            foreach (var kvp in buildDifference.environmentDifference)
            {
                WriteDifference(firstEnvironmentDiffFlat, secondEnvironmentDiffFlat, kvp);
            }

            // TODO: normally append and reset string builder, but currently only 1 diff can be displayed. For demo, we will show all diffs.

            foreach (var kvp in buildDifference.projectDifferences)
            {
                firstEnvironmentDiffFlat.AppendLine(kvp.Key.Name + kvp.Key.EvaluationText);
                var propertyDifference = kvp.Value.propertyDifference;
                if (propertyDifference == null)
                {
                    if (kvp.Value._soleOwner == buildDifference.binlogAName)
                    {
                        firstEnvironmentDiffFlat.AppendLine("Project Exists.");
                        secondEnvironmentDiffFlat.AppendLine("Project Doesn't Exist.");
                    }
                    else
                    {
                        firstEnvironmentDiffFlat.AppendLine("Project Doesn't Exist.");
                        secondEnvironmentDiffFlat.AppendLine("Project Exists.");
                    }
                }
                else
                {
                    foreach (var propDiff in propertyDifference)
                    {
                        WriteDifference(firstEnvironmentDiffFlat, secondEnvironmentDiffFlat, propDiff);
                    }
                }
            }

            // TODO: again, we need to separate the stringBuilders but not now.
            diffWindows.Add(new Tuple<string, string>(firstEnvironmentDiffFlat.ToString(), secondEnvironmentDiffFlat.ToString()));

            return diffWindows;
        }

        private void WriteDifference(StringBuilder stringA, StringBuilder stringB, Difference<Tuple<string, string>> d)
        {
            var firstEnvVarKey = d.binlogAValue?.Item1;
            var secondEnvVarKey = d.binlogBValue?.Item1;

            var firstEnvVarValue = d.binlogAValue?.Item2;
            var secondEnvVarVar = d.binlogBValue?.Item2;

            if (Include(d.binlogAValue))
            {
                stringA.AppendLine($"\t{firstEnvVarKey ?? "*NOT_DEFINED"}");
                stringA.AppendLine($"\t\t{firstEnvVarValue ?? "*IS_LEGIT_NULL"}");

                stringB.AppendLine($"\t{secondEnvVarKey ?? "*NOT_DEFINED"}");
                stringB.AppendLine($"\t\t{secondEnvVarVar ?? "*IS_LEGIT_NULL"}");
            }
        }
    }
}
