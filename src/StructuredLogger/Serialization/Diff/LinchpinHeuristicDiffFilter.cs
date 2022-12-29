using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Serialization.Diff
{
    internal class LinchpinHeuristicDiffFilter : IDiffFilter
    {
        public bool ShouldIncludeInDiff<T>(T item)
        {
            return false;
        }

        public bool ShouldIncludeInDiff(string item)
        {
            throw new NotImplementedException();
        }
    }
}
