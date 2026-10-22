using UnityEngine;

namespace Trapple.Workshop
{
    // Presentation units (Canvas pixels). These never touch a gameplay Rigidbody2D.
    public static class SpeedPreviewAnimator
    {
        public static float PixelsPerSecond(float level) => 150+85*Mathf.Clamp(level,0,3);
        public static float FramesPerSecond(float level) => 8+4*Mathf.Clamp(level,0,3);
    }
    public static class AgilityPreviewAnimator
    {
        public static Vector2 Offset(float phase,float level)
        {
            float dodge=Mathf.Sin(Mathf.Clamp01((phase-.18f)/.64f)*Mathf.PI);
            float hesitation=Mathf.Sin(phase*48)*(1-Mathf.Clamp01(level/3))*dodge*8;
            return new Vector2(dodge*(22+level*22)+hesitation,dodge*(18+level*14));
        }
        public static float Duration(float level) => 2.6f-.35f*Mathf.Clamp(level,0,3);
    }
    public static class JumpPreviewAnimator
    {
        public static float Height(float level) => 45+32*Mathf.Clamp(level,0,3);
        public static float AirTime(float level) => 1+.12f*Mathf.Clamp(level,0,3);
        public static float Altitude(float phase,float level) => 4*Height(level)*Mathf.Clamp01(phase)*(1-Mathf.Clamp01(phase));
    }
}
