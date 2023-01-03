using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Analyzers.Diff
{
    public abstract class BinlogDiffDataAdapter<BuildDifference, T>
    {
        internal DiffFilter filter;

        public abstract T Adapt(BuildDifference buildDifference);
    }
}
