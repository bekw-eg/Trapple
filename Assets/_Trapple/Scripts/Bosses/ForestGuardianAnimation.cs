using UnityEngine;

namespace Trapple.Bosses
{
    public enum GuardianAnimationState { Idle, Intro, CombatIdle, Windup, Attack, Hurt, Angry, Death, Retreat, Recovery }

    /// <summary>Layered sprite rig animation, matching Appi's existing procedural animation architecture.</summary>
    [DisallowMultipleComponent]
    public sealed class ForestGuardianAnimation : MonoBehaviour
    {
        [SerializeField] private Transform body, leftArm, rightArm, leftRoot, rightRoot, eyes;
        [SerializeField] private Transform crown;
        [SerializeField] private bool articulatedArms;
        [SerializeField, Min(.1f)] private float stanceSpeed = 3f;
        [SerializeField, Range(.01f, .2f)] private float swayAmount = .065f;
        [SerializeField, Min(1f)] private float blendSpeed = 12f;
        private Pose bodyPose, leftArmPose, rightArmPose, leftRootPose, rightRootPose, eyePose, crownPose;
        private SpriteRenderer[] artwork;
        private Color[] originalColors;
        private bool captured;
        private float age, clock, duration = .65f, contactTime = .2f, leafPulse;
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
            crownPose = new Pose(crown);
            artwork = body != null ? body.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
            originalColors = new Color[artwork.Length];
            for (int i = 0; i < artwork.Length; i++) originalColors[i] = artwork[i].color;
            captured = true;
        }

        public void Play(GuardianAnimationState state, GuardianAttack currentAttack = GuardianAttack.RootStrike,
            float seconds = .65f, float contactDelay = .2f)
        {
            if (State == state && attack == currentAttack) return;
            State = state;
            attack = currentAttack;
            duration = Mathf.Max(.01f, seconds);
            contactTime = Mathf.Max(.06f, contactDelay);
            age = 0f;
            leafPulse = 0f;
        }

        public void PulseLeaves() => leafPulse = 1f;

        private void LateUpdate()
        {
            if (!captured) Capture();
            // A pause requested during Update must also stop this frame's LateUpdate pose.
            if (Time.timeScale <= 0f) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            clock += dt;
            age += dt;
            leafPulse = Mathf.Max(0f, leafPulse - dt * 4.5f);
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
            Vector3 leftOffset = Vector3.zero, rightOffset = Vector3.zero;
            Vector3 armStretch = Vector3.one;
            float crownAngle = -sway * 2f;
            Vector3 crownOffset = new Vector3(-sway * .04f, bounce * .025f, 0f);
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
                    float windup = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(age / duration));
                    bodyOffset.x += .1f * windup;
                    bodyAngle = -3f * windup;
                    leftAngle = attack == GuardianAttack.BranchSlam ? 100f * windup : 32f * windup;
                    rightAngle = attack == GuardianAttack.LeafStorm ? -60f * windup : -24f * windup;
                    eyeOpen = .72f;
                    if (articulatedArms)
                    {
                        float tremble = Mathf.Sin(age * 35f) * .025f * windup * windup;
                        if (attack == GuardianAttack.BranchSlam)
                        {
                            leftAngle = -140f * windup; rightAngle = 30f * windup;
                            bodyOffset = new Vector3(.22f * windup, -.12f * windup, 0f);
                            bodyAngle = -5f * windup; crownAngle = 7f * windup;
                            leftOffset = new Vector3(0f, .25f * windup + tremble, 0f);
                        }
                        else if (attack == GuardianAttack.RootStrike || attack == GuardianAttack.RootWave)
                        {
                            bodyOffset.y = -.3f * windup;
                            bodyScale = new Vector3(1f + .05f * windup, 1f - .065f * windup, 1f);
                            leftAngle = -65f * windup; rightAngle = 65f * windup;
                            leftRootAngle = 8f * windup; rightRootAngle = -8f * windup;
                            crownOffset.y = -.12f * windup + tremble;
                        }
                        else
                        {
                            bodyOffset.x = .25f * windup; bodyAngle = -4f * windup;
                            bodyScale = new Vector3(1f + .06f * windup, 1f + .035f * windup, 1f);
                            leftAngle = 30f * windup; rightAngle = -40f * windup;
                            crownAngle = 6f * windup; crownOffset.y = .14f * windup;
                        }
                    }
                    if (attack == GuardianAttack.BranchRain)
                    {
                        leftAngle = -155f * windup; rightAngle = 155f * windup;
                        bodyOffset.y = .15f * windup; bodyScale.y = 1f + .035f * windup;
                        crownAngle = Mathf.Sin(age * 22f) * 3f * windup;
                        eyeOpen = .55f;
                    }
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
                    if (articulatedArms)
                    {
                        float sweep = Mathf.Pow(Mathf.Clamp01(age / contactTime), 2.4f);
                        float settle = Mathf.Exp(-Mathf.Max(0f, age - contactTime) * 7f);
                        float vibration = Mathf.Sin(Mathf.Max(0f, age - contactTime) * 38f) * settle;
                        if (attack == GuardianAttack.BranchSlam)
                        {
                            leftAngle = Mathf.Lerp(-140f, -12f, sweep) + vibration * 3f;
                            rightAngle = Mathf.Lerp(30f, -25f, sweep);
                            leftOffset = new Vector3(-.28f * sweep, .25f - .7f * sweep, 0f);
                            armStretch = new Vector3(1f + .05f * sweep, 1f + .28f * sweep, 1f);
                            bodyOffset = new Vector3(.22f - .55f * sweep, -.12f - .1f * sweep, 0f);
                            bodyAngle = Mathf.Lerp(-5f, 8f, sweep);
                            crownAngle = Mathf.Lerp(7f, -9f, sweep) + vibration * 2f;
                        }
                        else if (attack == GuardianAttack.RootStrike || attack == GuardianAttack.RootWave)
                        {
                            leftAngle = Mathf.Lerp(-65f, 16f, sweep); rightAngle = -leftAngle;
                            bodyOffset.y = Mathf.Lerp(-.3f, -.12f, sweep) + vibration * .035f;
                            bodyScale = new Vector3(1.06f, .94f + .045f * sweep, 1f);
                            leftRootAngle = Mathf.Lerp(8f, -14f, sweep); rightRootAngle = -leftRootAngle;
                            crownOffset.y = -.14f + Mathf.Sin(age * 23f) * .13f * settle;
                        }
                        else
                        {
                            bodyOffset.x = -.16f + leafPulse * .17f;
                            bodyScale = new Vector3(1f - .05f * leafPulse, 1f + .035f * leafPulse, 1f);
                            bodyAngle = 4f + leafPulse * 3f;
                            leftAngle = -70f - leafPulse * 20f;
                            rightAngle = -65f - leafPulse * 25f;
                            crownAngle = -7f + Mathf.Sin(age * 22f) * 5f;
                            crownOffset = new Vector3(-leafPulse * .14f, leafPulse * .11f, 0f);
                        }
                    }
                    if (attack == GuardianAttack.BranchRain)
                    {
                        leftAngle = -155f + leafPulse * 38f; rightAngle = 155f - leafPulse * 38f;
                        bodyOffset.y = .12f - leafPulse * .16f;
                        bodyAngle = Mathf.Sin(age * 8f) * 1.5f;
                        crownAngle = Mathf.Sin(age * 18f) * (2f + leafPulse * 3f);
                    }
                    else if (attack == GuardianAttack.RootWave)
                    {
                        bodyOffset.y = -.08f - leafPulse * .17f;
                        leftRootAngle = -18f * leafPulse; rightRootAngle = 12f * leafPulse;
                        crownOffset.y = -.1f * leafPulse;
                    }
                    break;
                case GuardianAnimationState.Recovery:
                    float recovery = Mathf.Pow(1f - Mathf.Clamp01(age / duration), 2f);
                    if (articulatedArms)
                    {
                        bool slam = attack == GuardianAttack.BranchSlam;
                        bodyOffset = new Vector3((slam ? -.33f : -.1f) * recovery, -.14f * recovery, 0f);
                        bodyAngle = (slam ? 8f : 3f) * recovery;
                        leftAngle = (slam ? -12f : -25f) * recovery;
                        rightAngle = -25f * recovery;
                        leftOffset.y = slam ? -.45f * recovery : 0f;
                        armStretch.y += slam ? .28f * recovery : 0f;
                        crownAngle = -9f * recovery;
                        crownOffset.y = Mathf.Sin(age * 17f) * .06f * recovery;
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
            float blend = 1f - Mathf.Exp(-(State == GuardianAnimationState.Attack ? 50f : blendSpeed) * dt);
            bodyPose.Apply(bodyOffset, bodyAngle, bodyScale, blend);
            leftArmPose.Apply(leftOffset, leftAngle, armStretch, blend);
            rightArmPose.Apply(rightOffset, rightAngle, Vector3.one, blend);
            crownPose.Apply(crownOffset, crownAngle, Vector3.one, blend);
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
