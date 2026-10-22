using System.Collections;
using UnityEngine;

namespace Trapple.Workshop
{
    // Optional authored 2D frames replace the temporary opening/light overlay without changing the sequence.
    public sealed class WorkshopDoorAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer door, light;
        [SerializeField] private Sprite[] openingFrames;
        [SerializeField, Min(.01f)] private float duration = .35f;
        private Sprite closedSprite;
        public float Openness { get; private set; }
        public bool HasAuthoredFrames => openingFrames != null && openingFrames.Length > 1;
        public void Configure(SpriteRenderer door, SpriteRenderer light) { this.door=door; this.light=light; }
        private void Awake() { if(door != null) closedSprite=door.sprite; Set(0); }
        public IEnumerator Animate(bool open)
        {
            float start=Openness, elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Time.unscaledDeltaTime;
                Set(Mathf.Lerp(start,open?1:0,Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,duration))));
                yield return null;
            }
            Set(open?1:0);
        }
        public void Set(float value)
        {
            Openness=value;
            if(door != null)
            {
                door.enabled=value>0;
                if(HasAuthoredFrames)
                {
                    door.sprite=openingFrames[Mathf.RoundToInt(value*(openingFrames.Length-1))];
                    door.color=Color.white;
                }
                else { door.sprite=closedSprite ?? door.sprite; door.color=new Color(.12f,.065f,.025f,value); }
            }
            if(light != null) light.color=new Color(1,.65f,.23f,.5f*value);
        }
    }
}
