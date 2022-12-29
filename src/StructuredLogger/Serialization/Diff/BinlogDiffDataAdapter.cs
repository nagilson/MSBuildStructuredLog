using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Serialization.Diff
{
    internal abstract class BinlogDiffDataAdapter<BuildDifference, T>
    {
        internal IDiffFilter filter;

        public abstract T Adapt(BuildDifference buildDifference);
    }
}
