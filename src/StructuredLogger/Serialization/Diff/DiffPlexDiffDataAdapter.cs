using System;
using System.Collections.Generic;
using System.Text;
using static StructuredLogger.Analyzers.DiffModel;

namespace StructuredLogger.Serialization.Diff
{
    internal class DiffPlexDiffDataAdapter : BinlogDiffDataAdapter<BuildDifference, List<string>>
    {
        DiffPlexDiffDataAdapter()
        {
            filter = new LinchpinHeuristicDiffFilter();
        }

        public override List<string> Adapt(BuildDifference buildDifference)
        {
            List<string> diffWindows = new();
            string f = "f";
            if ( Include(f) )
            {

            }
            return diffWindows;
        }
    }
}
