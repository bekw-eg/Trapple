using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine.SceneManagement;

namespace Trapple.Editor
{
    // SceneManager (including the existing PlayerDeath) needs scenes in the active build list.
    // Select our asset only for this Play session; the shared global list stays untouched.
    [InitializeOnLoad]
    public static class BossPrototypePlayMode
    {
        private const string PreviousProfile = "Trapple.PreviousPrototypeProfile";
        private const string RestoreProfile = "Trapple.RestorePrototypeProfile";

        static BossPrototypePlayMode() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode &&
                SceneManager.GetActiveScene().path.StartsWith("Assets/_Trapple/Scenes/"))
            {
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(BossPrototypeBuilder.ProfilePath);
                if (profile == null) return;
                var previous = BuildProfile.GetActiveBuildProfile();
                SessionState.SetString(PreviousProfile, previous == null ? "" : AssetDatabase.GetAssetPath(previous));
                SessionState.SetBool(RestoreProfile, true);
                BuildProfile.SetActiveBuildProfile(profile);
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RestoreProfile, false))
            {
                SessionState.SetBool(RestoreProfile, false);
                string path = SessionState.GetString(PreviousProfile, "");
                BuildProfile.SetActiveBuildProfile(string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<BuildProfile>(path));
            }
        }
    }
}
