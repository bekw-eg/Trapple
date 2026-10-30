using System;
using Trapple.Hub;
using UnityEditor;
using UnityEngine;

namespace Trapple.Editor
{
    public static class WorldCompletionSetup
    {
        public const string ConfigurationPath = "Assets/_Trapple/Settings/HubConfiguration.asset";

        public static void ApplyConfiguration()
        {
            var config = AssetDatabase.LoadAssetAtPath<HubConfiguration>(ConfigurationPath);
            if (config == null) throw new InvalidOperationException("Existing HubConfiguration is required.");
            var meadow = config.World("meadow");
            var forest = config.World("forest");
            if (meadow == null || forest == null) throw new InvalidOperationException("The configured meadow and forest worlds are required.");
            meadow.completionRequiresBoss = true;
            meadow.boss = "Ancient Tree";
            // Forest currently has one real, continuous scene. Planned future slots must not prevent completion.
            forest.plannedLevels = Math.Max(1, Array.FindAll(forest.levelScenes ?? Array.Empty<string>(),
                path => !string.IsNullOrEmpty(path)).Length);
            forest.completionRequiresBoss = true;
            forest.boss = "Forest Guardian";
            EditorUtility.SetDirty(config);
        }
    }
}
