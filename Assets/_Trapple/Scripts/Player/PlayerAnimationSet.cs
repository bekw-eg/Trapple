using UnityEngine;

namespace Trapple.Player
{
    [CreateAssetMenu(menuName = "Trapple/Player animation set")]
    public sealed class PlayerAnimationSet : ScriptableObject
    {
        public Sprite[] idle;
        public Sprite[] walkRight;
        public Sprite[] walkLeft;
        public Sprite[] jump;
        public AnimationClip idleClip;
        public AnimationClip runRightClip;
        public AnimationClip runLeftClip;
        [Min(0.1f)] public float idleFramesPerSecond = 4f;
        [Min(0.1f)] public float walkFramesPerSecond = 12f;
        [Min(0.01f)] public float takeoffDuration = 0.08f;
        [Min(0.01f)] public float landingDuration = 0.12f;
        public bool IsValid => idle != null && idle.Length > 0 && walkRight != null && walkRight.Length > 0 &&
            walkLeft != null && walkLeft.Length > 0 && jump != null && jump.Length >= 4;
    }
}
