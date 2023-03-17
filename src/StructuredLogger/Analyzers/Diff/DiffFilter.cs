using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Analyzers.Diff
{
    internal abstract class DiffFilter
    {
        public abstract bool ShouldIncludeInDiff<T>(T item);
    }
}
