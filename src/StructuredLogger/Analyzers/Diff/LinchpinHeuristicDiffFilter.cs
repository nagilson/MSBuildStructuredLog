using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Analyzers.Diff
{
    internal class LinchpinHeuristicDiffFilter : IDiffFilter
    {
        public bool ShouldIncludeInDiff<T>(T item)
        {
            return true;
        }

        public bool ShouldIncludeInDiff(string item)
        {
            return true;
        }
    }
}
