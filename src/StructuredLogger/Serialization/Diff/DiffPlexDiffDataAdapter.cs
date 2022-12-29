using System;
using System.Collections.Generic;
using System.Text;
using static StructuredLogger.Analyzers.DiffModel;

namespace StructuredLogger.Serialization.Diff
{
    internal class DiffPlexDiffDataAdapter : BinlogDiffDataAdapter<BuildDifference, string>
    {
        DiffPlexDiffDataAdapter()
        {
            filter = new LinchpinHeuristicDiffFilter();
        }

        public override string Adapt(BuildDifference buildDifference)
        {
            throw new NotImplementedException();
        }
    }
}
