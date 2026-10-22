using Trapple.Hub;
using Trapple.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Workshop
{
    public sealed class UpgradePreviewController : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        [SerializeField] private Image actor, obstacle;
        [SerializeField] private Image[] trails;
        [SerializeField] private Image[] dust;
        [SerializeField] private PlayerAnimationSet animations;
        private HubConfiguration config;
        private IProgressionService progression;
        private float cycle, frameClock, x=-90, smoothLevel;
        private int targetLevel;
        public UpgradeKind Kind { get; private set; }
        public float DisplayedLevel => smoothLevel;
        public float RunSpeed => SpeedPreviewAnimator.PixelsPerSecond(smoothLevel);
        public float JumpHeight => JumpPreviewAnimator.Height(smoothLevel);
        public Vector2 ActorPosition => actor.rectTransform.anchoredPosition;
        public int FrameIndex { get; private set; }
        public int WrapCount { get; private set; }

        public void Configure(RectTransform viewport,Image actor,Image obstacle,Image[] trails,Image[] dust,PlayerAnimationSet animations)
        { this.viewport=viewport; this.actor=actor; this.obstacle=obstacle; this.trails=trails; this.dust=dust; this.animations=animations; }
        public void Bind(HubConfiguration config,IProgressionService progression)
        {
            this.config=config; this.progression=progression; Select(UpgradeKind.Speed);
        }
        public void Select(UpgradeKind kind)
        {
            Kind=kind; cycle=0; x=-90; frameClock=0; WrapCount=0;
            targetLevel=progression?.Level(kind)??0; smoothLevel=targetLevel;
        }
        public void Refresh() => targetLevel=progression?.Level(Kind)??0;
        private void Update()
        {
            if(actor==null || viewport==null || progression==null) return;
            float dt=Time.unscaledDeltaTime;
            smoothLevel=Mathf.MoveTowards(smoothLevel,targetLevel,dt*3);
            var skin=config.Cosmetic(progression.EquippedCosmetic);
            var set=skin?.previewAnimations!=null && skin.previewAnimations.IsValid?skin.previewAnimations:animations;
            bool staticSkin=skin!=null && skin.previewAnimations==null && skin.sprite!=null && skin.sprite!=config.heroSprite;
            actor.color=skin?.tint??Color.white;
            float width=viewport.rect.width;
            bool airborne=false, landing=false;
            Vector2 position;
            float rate=SpeedPreviewAnimator.FramesPerSecond(smoothLevel);
            switch(Kind)
            {
                case UpgradeKind.Agility:
                    cycle=Mathf.Repeat(cycle+dt/AgilityPreviewAnimator.Duration(smoothLevel),1);
                    position=new Vector2(width*.32f,20)+AgilityPreviewAnimator.Offset(cycle,smoothLevel);
                    obstacle.rectTransform.anchoredPosition=new Vector2(Mathf.Lerp(width+50,-50,cycle),20);
                    rate=10+smoothLevel*5; break;
                case UpgradeKind.Jump:
                    float air=JumpPreviewAnimator.AirTime(smoothLevel);
                    cycle=Mathf.Repeat(cycle+dt,air+.65f);
                    float phase=cycle/air;
                    airborne=phase<1; landing=phase>=1 && cycle<air+.14f;
                    position=new Vector2(width*.5f,20+(airborne?JumpPreviewAnimator.Altitude(phase,smoothLevel):0));
                    break;
                default:
                    x+=RunSpeed*dt;
                    if(x>width+90) { x=-90; WrapCount++; }
                    position=new Vector2(x,20); break;
            }
            obstacle.enabled=Kind==UpgradeKind.Agility;
            frameClock+=dt*rate;
            if(set!=null && set.IsValid && !staticSkin)
            {
                Sprite[] frames=Kind==UpgradeKind.Jump?set.idle:set.walkRight;
                FrameIndex=Mathf.FloorToInt(frameClock)%frames.Length;
                actor.sprite=airborne?set.jump[cycle/JumpPreviewAnimator.AirTime(smoothLevel)<.5f?1:2]:landing?set.jump[3]:frames[FrameIndex];
            }
            else actor.sprite=skin?.sprite??config.heroSprite;
            actor.rectTransform.anchoredPosition=position;
            float bob=staticSkin && Kind!=UpgradeKind.Jump?Mathf.Sin(frameClock*Mathf.PI)*.035f:0;
            actor.rectTransform.localScale=new Vector3(1+bob,1-bob,1);
            for(int i=0;i<trails.Length;i++)
            {
                bool visible=Kind!=UpgradeKind.Jump && i<Mathf.RoundToInt(smoothLevel+1);
                trails[i].enabled=visible; trails[i].sprite=actor.sprite;
                trails[i].color=new Color(actor.color.r,actor.color.g,actor.color.b,(.18f+.035f*smoothLevel)/(i+1));
                trails[i].rectTransform.anchoredPosition=position-Vector2.right*(i+1)*(12+smoothLevel*5);
            }
            for(int i=0;i<dust.Length;i++)
            {
                dust[i].enabled=Kind==UpgradeKind.Speed && i<2+Mathf.RoundToInt(smoothLevel*2);
                float t=Mathf.Repeat(Time.unscaledTime*(1+smoothLevel*.2f)+i*.19f,1);
                dust[i].rectTransform.anchoredPosition=position+new Vector2(-25-t*(40+smoothLevel*15),4+Mathf.Sin(t*Mathf.PI)*14);
                dust[i].color=new Color(.61f,.39f,.17f,(1-t)*.45f);
                dust[i].rectTransform.localScale=Vector3.one*(.5f+t);
            }
        }
    }
}
