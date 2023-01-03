using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredLogger.Analyzers.Diff
{
    internal class LinchpinHeuristicDiffFilter : IDiffFilter
    {
        private Dictionary<string, string> unimportantProperties = new Dictionary<string, string>
        {
            {"DOTNET_CLI_TELEMETRY_SESSIONID", ""},
            {"MSBuildExtensionsPath", "" },
            {"MSBuildExtensionsPath32", "" },
            {"BundledNETCoreAppPackageVersion", ""},
            {"BundledNETCorePlatformsPackageVersion", "" },
            {"CodeAnalysisTargets", "" },
            {"ILCompilerTargetsPath", "" },
            {"ILLinkTargetsPath", "" },
            {"ILLinkTasksAssembly", "" },
            {"MSBuildSDKsPath", "" },
            {"MSBuildSemanticVersion", "" },
            {"MSBuildToolsPath", "" },
            {"NetCoreSDKBundledVersionProps", "" },
            {"NuGetBuildTasksPackTargets", ""},
            {"RoslynTargetsPath", "" },
            {"CommonXamlResourcesDirectory", "" },
            {"LanguageTargets", "" },
            {"MSBuildBinPath", "" },
            {"MSBuildFileVersion", "" },
            {"MSTestToolsTargets", "" },
            {"NetCoreSDKBundledCliToolsProps", "" },
            {"TizenProjectExtensionsPath", "" },
            {"MsAppxPackageTargets", "" },
            {"_ResizetizerTaskAssemblyName", "" },
            {"MSBuildAllProjects", "" },
            {"_RuntimePackInWorkloadVersion7", "" },
            { "", "" }
        };

        private bool LooksLikeAPath(string potentialPath)
        {
            return Uri.IsWellFormedUriString(potentialPath, UriKind.RelativeOrAbsolute);
        }

        public bool ShouldIncludeInDiff<T>(T item)
        {
            return true;
        }

        public bool ShouldIncludeInDiff(string item)
        {
            return true;
        }

        public bool ShouldIncludeInDiff(Tuple<string, string> propertySet)
        {
            if (propertySet == null)
            {
                return false;
            }

            var propName = propertySet.Item1;
            var propValue = propertySet.Item2;

            if (unimportantProperties.ContainsKey(propName))
            {
                return false;
            }

            if (LooksLikeAPath(propValue))
            {
                return false;
            }

            return true;
        }
    }
}
