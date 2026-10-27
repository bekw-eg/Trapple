using System;
using System.IO;
using System.Linq;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Trapple.Editor
{
    [InitializeOnLoad]
    public static class ForestPortalSetup
    {
        private const string Request = "Temp/forest-portal-apply.txt";
        private static readonly string[] Worlds = { "forest", "factory", "lava" };
        static ForestPortalSetup() => EditorApplication.update += ApplyRequested;

        private static void ApplyRequested()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            File.Delete(Request);
            try { Apply(); }
            catch (Exception exception)
            {
                File.WriteAllText("Temp/forest-portal-result.txt", "FAILED\n" + exception);
                Debug.LogException(exception);
            }
        }

        [MenuItem("Tools/Trapple/Connect Hub World Portals")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before updating hub assets.");
            var config = AssetDatabase.LoadAssetAtPath<HubConfiguration>(HubBuilder.ConfigPath);
            var forest = config.World("forest");
            forest.initiallyUnlocked = true;
            if (forest.levelScenes == null || forest.levelScenes.Length == 0)
                forest.levelScenes = new[] { ForestGoal.ScenePath };
            EditorUtility.SetDirty(config);

            // Existing Hub and TrappleUI scene instances inherit these fields.
            // Leave the user's open scene and unsaved scene edits untouched.
            for (int i = 0; i < Worlds.Length; i++)
            {
                string path = "Assets/_Trapple/Prefabs/Hub/Portal" + i + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    root.GetComponent<HubInteractable>().SetWorld(Worlds[i]);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:BuildProfile", new[] { "Assets/_Trapple/Settings" }))
            {
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (!profile.overrideGlobalScenes) continue;
                var scenes = profile.scenes.ToList();
                if (scenes.Any(scene => scene.path == ForestGoal.ScenePath && scene.enabled)) continue;
                scenes.RemoveAll(scene => scene.path == ForestGoal.ScenePath);
                scenes.Add(new EditorBuildSettingsScene(ForestGoal.ScenePath, true));
                profile.scenes = scenes.ToArray();
                EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/forest-portal-result.txt", "APPLIED: forest unlocked, existing level assigned, portals linked, build profiles updated. No tests run.");
        }
    }
}

