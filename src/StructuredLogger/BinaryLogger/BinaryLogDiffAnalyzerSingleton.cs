using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Build.Logging.StructuredLogger;

namespace StructuredLogger.BinaryLogger
{
    public class BinaryLogDiffAnalyzer
    {
        private List<Build> _buildsForDiff = new List<Build>();
        private List<Project> _buildAProjects = new List<Project>();
        private List<Project> _buildBProjects = new List<Project>();

        public BinaryLogDiffAnalyzer(List<Build> buildsForDiff)
        {
            Debug.Assert(buildsForDiff != null);
            Debug.Assert(buildsForDiff.Count == 2);

            _buildsForDiff = buildsForDiff;

            _buildAProjects = _buildsForDiff.First().Children.OfType<Project>().ToList();
            _buildBProjects = _buildsForDiff.ElementAt(1).Children.OfType<Project>().ToList();

            FindDifferences();
        }

        void FindDifferences()
        {
            CollectPropertyDiff();
            CollectTaskDiff();
            CollectTargetDiff();
        }

        void CollectPropertyDiff()
        {

        }

        void CollectTargetDiff()
        {

        }

        void CollectTaskDiff()
        {

        }

    }
}
