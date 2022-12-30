using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Analyzers.Diff
{
    internal interface IDiffFilter
    {
        bool ShouldIncludeInDiff<T>(T item);
    }
}
