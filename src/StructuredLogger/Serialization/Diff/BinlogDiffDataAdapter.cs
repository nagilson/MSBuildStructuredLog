using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Serialization.Diff
{
    internal abstract class BinlogDiffDataAdapter<BuildDifference, T>
    {
        internal IDiffFilter filter;
        public bool useFilter = true;

        public abstract T Adapt(BuildDifference buildDifference);

        internal bool Include<V>(V obj)
        {
            return useFilter && filter.ShouldIncludeInDiff(obj);
        }
    }
}
