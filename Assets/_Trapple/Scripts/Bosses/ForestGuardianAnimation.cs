using UnityEngine;

namespace Trapple.Bosses
{
    public enum GuardianAnimationState { Idle, Intro, CombatIdle, Windup, Attack, Hurt, Angry, Death, Retreat }

    /// <summary>Layered sprite rig animation, matching Appi's existing procedural animation architecture.</summary>
    [DisallowMultipleComponent]
    public sealed class ForestGuardianAnimation : MonoBehaviour
    {
        [SerializeField] private Transform body, leftArm, rightArm, leftRoot, rightRoot, eyes;
        [SerializeField, Min(.1f)] private float stanceSpeed = 3f;
        [SerializeField, Range(.01f, .2f)] private float swayAmount = .065f;
        [SerializeField, Min(1f)] private float blendSpeed = 12f;
        private Pose bodyPose, leftArmPose, rightArmPose, leftRootPose, rightRootPose, eyePose;
        private SpriteRenderer[] artwork;
        private Color[] originalColors;
        private bool captured;
        private float age, clock;
        private GuardianAttack attack;
        public GuardianAnimationState State { get; private set; }

        private sealed class Pose
        {
            public readonly Transform transform;
            public readonly Vector3 position, scale;
            public readonly Quaternion rotation;
            public Pose(Transform value)
            {
                transform = value;
                if (value == null) return;
                position = value.localPosition;
                scale = value.localScale;
                rotation = value.localRotation;
            }

            public void Apply(Vector3 offset, float angle, Vector3 stretch, float blend)
            {
                if (transform == null) return;
                transform.localPosition = Vector3.Lerp(transform.localPosition, position + offset, blend);
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    rotation * Quaternion.Euler(0f, 0f, angle), blend);
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.Scale(scale, stretch), blend);
            }
        }

        private void Awake() => Capture();

        private void Capture()
        {
            if (captured) return;
            bodyPose = new Pose(body);
            leftArmPose = new Pose(leftArm);
            rightArmPose = new Pose(rightArm);
            leftRootPose = new Pose(leftRoot);
            rightRootPose = new Pose(rightRoot);
            eyePose = new Pose(eyes);
            artwork = body != null ? body.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
            originalColors = new Color[artwork.Length];
            for (int i = 0; i < artwork.Length; i++) originalColors[i] = artwork[i].color;
            captured = true;
        }

        public void Play(GuardianAnimationState state, GuardianAttack currentAttack = GuardianAttack.RootStrike)
        {
            if (State == state && attack == currentAttack) return;
            State = state;
            attack = currentAttack;
            age = 0f;
        }

        private void LateUpdate()
        {
            if (!captured) Capture();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            clock += dt;
            age += dt;
            bool angry = State == GuardianAnimationState.Angry;
            float phase = clock * stanceSpeed * (angry ? 1.25f : 1f);
            float sway = Mathf.Sin(phase), bounce = Mathf.Sin(phase * 2f);
            Vector3 bodyOffset = new Vector3(sway * swayAmount, Mathf.Abs(bounce) * .045f, 0f);
            Vector3 bodyScale = new Vector3(1f + bounce * .012f, 1f - bounce * .012f, 1f);
            float bodyAngle = sway * 1.3f;
            float leftAngle = -8f + Mathf.Sin(phase + .5f) * 5f;
            float rightAngle = 8f + Mathf.Sin(phase + 2f) * 5f;
            float leftRootAngle = sway * 2.5f, rightRootAngle = -sway * 2.5f;
            float eyeOpen = Mathf.Repeat(clock + .7f, 4.1f) < .12f ? .12f : 1f;
            float flash = 0f;
            switch (State)
            {
                case GuardianAnimationState.Idle:
                    bodyOffset *= .4f;
                    bodyAngle *= .35f;
                    leftAngle = -22f + sway * 2f;
                    rightAngle = 22f - sway * 2f;
                    eyeOpen = .15f;
                    break;
                case GuardianAnimationState.Intro:
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 1.7f));
                    bodyOffset.y -= (1f - rise) * .55f;
                    bodyScale.y = Mathf.Lerp(.82f, 1f, rise);
                    leftAngle = Mathf.Lerp(-35f, 35f, rise);
                    rightAngle = Mathf.Lerp(35f, -35f, rise);
                    eyeOpen = Mathf.Clamp01((age - .4f) / .5f);
                    bodyAngle += Mathf.Sin(age * 13f) * (1f - rise) * 2f;
                    break;
                case GuardianAnimationState.Windup:
                    float windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / .65f));
                    bodyOffset.x += .1f * windup;
                    bodyAngle = -3f * windup;
                    leftAngle = attack == GuardianAttack.BranchSlam ? 100f * windup : 32f * windup;
                    rightAngle = attack == GuardianAttack.LeafStorm ? -60f * windup : -24f * windup;
                    eyeOpen = .72f;
                    break;
                case GuardianAnimationState.Attack:
                    float strike = Mathf.Sin(Mathf.Clamp01(age / .32f) * Mathf.PI);
                    if (attack == GuardianAttack.BranchSlam)
                    {
                        bodyOffset.x -= .2f * strike;
                        bodyAngle = 7f * strike;
                        leftAngle = -40f;
                        rightAngle = -22f;
                    }
                    else if (attack == GuardianAttack.RootStrike)
                    {
                        bodyOffset.y -= .16f * strike;
                        leftRootAngle = -12f * strike;
                        rightRootAngle = 12f * strike;
                        leftAngle = -26f;
                        rightAngle = 26f;
                    }
                    else
                    {
                        leftAngle = 35f + sway * 8f;
                        rightAngle = -55f - sway * 8f;
                        bodyOffset.x += Mathf.Sin(age * 15f) * .025f;
                    }
                    break;
                case GuardianAnimationState.Hurt:
                    float recoil = Mathf.Exp(-age * 10f);
                    bodyOffset.x += Mathf.Sin(age * 35f) * .18f * recoil;
                    bodyAngle -= 7f * recoil;
                    leftAngle = -35f * recoil;
                    rightAngle = 35f * recoil;
                    eyeOpen = .3f;
                    flash = recoil * .8f;
                    break;
                case GuardianAnimationState.Angry:
                    leftAngle += 15f;
                    rightAngle -= 15f;
                    eyeOpen *= .68f;
                    bodyScale.y += .025f;
                    break;
                case GuardianAnimationState.Death:
                    float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 1.4f));
                    bodyOffset = new Vector3(.3f * fall, -.75f * fall, 0f);
                    bodyAngle = -17f * fall;
                    leftAngle = -65f * fall;
                    rightAngle = 65f * fall;
                    leftRootAngle = 12f * fall;
                    rightRootAngle = -12f * fall;
                    eyeOpen = 1f - fall;
                    break;
                case GuardianAnimationState.Retreat:
                    float rest = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / 1.7f));
                    bodyOffset.x = rest * .65f;
                    bodyOffset.y = -.35f * rest + Mathf.Sin(age * 6f) * .045f * (1f - rest);
                    bodyAngle = -6f * rest;
                    leftAngle = -55f * rest;
                    rightAngle = 55f * rest;
                    leftRootAngle = Mathf.Sin(age * 6f) * 6f * (1f - rest);
                    rightRootAngle = -leftRootAngle;
                    eyeOpen = 1f - rest;
                    break;
            }
            float blend = 1f - Mathf.Exp(-blendSpeed * dt);
            bodyPose.Apply(bodyOffset, bodyAngle, bodyScale, blend);
            leftArmPose.Apply(Vector3.zero, leftAngle, Vector3.one, blend);
            rightArmPose.Apply(Vector3.zero, rightAngle, Vector3.one, blend);
            leftRootPose.Apply(new Vector3(0f, Mathf.Max(0f, sway) * .025f, 0f), leftRootAngle, Vector3.one, blend);
            rightRootPose.Apply(new Vector3(0f, Mathf.Max(0f, -sway) * .025f, 0f), rightRootAngle, Vector3.one, blend);
            eyePose.Apply(Vector3.zero, angry ? -3f : 0f, new Vector3(1f, Mathf.Max(.02f, eyeOpen), 1f), blend);
            for (int i = 0; i < artwork.Length; i++)
            {
                if (artwork[i] == null) continue;
                artwork[i].color = Color.Lerp(originalColors[i], Color.white, flash);
            }
        }
    }
}
