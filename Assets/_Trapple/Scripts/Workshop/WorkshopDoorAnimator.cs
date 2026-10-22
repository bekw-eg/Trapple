using System.Collections;
using UnityEngine;

namespace Trapple.Workshop
{
    // Authored frames include the whole building. The closed frame stays visible between visits.
    public sealed class WorkshopDoorAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer door, light;
        [SerializeField] private Sprite[] openingFrames;
        [SerializeField, Min(.01f)] private float duration = .64f;
        private Sprite closedSprite;
        private int sequenceVersion;
        public float Openness { get; private set; }
        public bool HasAuthoredFrames => openingFrames != null && openingFrames.Length > 1;
        public int FrameCount => openingFrames?.Length ?? 0;
        public int FrameIndex { get; private set; }
        public bool IsAnimating { get; private set; }
        public SpriteRenderer Presentation => door;
        public void Configure(SpriteRenderer door, SpriteRenderer light, Sprite[] frames=null)
        {
            this.door=door; this.light=light; openingFrames=frames;
            if(door!=null) closedSprite=door.sprite;
            Set(0);
        }
        private void Awake() { if(door != null) closedSprite=door.sprite; Set(0); }
        public IEnumerator Animate(bool open)
        {
            int version=++sequenceVersion;
            IsAnimating=true;
            try
            {
                if(HasAuthoredFrames)
                {
                    int target=open?FrameCount-1:0;
                    if(FrameIndex==target) { Apply(open?1:0); yield break; }
                    int step=target>=FrameIndex?1:-1;
                    // Give every supplied frame its own hold; easing a frame index can skip middle frames.
                    for(int index=FrameIndex;;index+=step)
                    {
                        if(version!=sequenceVersion) yield break;
                        Apply(index/(float)(FrameCount-1));
                        yield return new WaitForSecondsRealtime(Mathf.Max(.01f,duration)/FrameCount);
                        if(index==target) break;
                    }
                }
                else
                {
                    float start=Openness, elapsed=0;
                    while(elapsed<duration && version==sequenceVersion)
                    {
                        elapsed+=Time.unscaledDeltaTime;
                        Apply(Mathf.Lerp(start,open?1:0,Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,duration))));
                        yield return null;
                    }
                    if(version==sequenceVersion) Apply(open?1:0);
                }
            }
            finally { if(version==sequenceVersion) IsAnimating=false; }
        }
        public void Set(float value)
        {
            sequenceVersion++; IsAnimating=false; Apply(value);
        }
        private void Apply(float value)
        {
            value=Mathf.Clamp01(value); Openness=value;
            if(door != null)
            {
                door.enabled=HasAuthoredFrames || value>0;
                if(HasAuthoredFrames)
                {
                    FrameIndex=Mathf.RoundToInt(value*(FrameCount-1));
                    door.sprite=openingFrames[FrameIndex];
                    door.color=Color.white;
                }
                else { door.sprite=closedSprite ?? door.sprite; door.color=new Color(.12f,.065f,.025f,value); }
            }
            if(light != null) light.color=new Color(1,.65f,.23f,.5f*value);
        }
        private void OnDisable() { sequenceVersion++; IsAnimating=false; }
    }
}
