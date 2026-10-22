using System.Collections;
using Trapple.Hub;
using Trapple.Player;
using UnityEngine;

namespace Trapple.Workshop
{
    public enum WorkshopTransitionState { Outside, Entering, Inside, Exiting }

    public sealed class WorkshopTransitionController : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private HubUIController hub;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Behaviour cameraFollow;
        [SerializeField] private Transform[] entrancePath;
        [SerializeField] private Transform doorFocus;
        [SerializeField] private WorkshopDoorAnimator door;
        [SerializeField] private IrisTransitionController iris;
        [SerializeField] private WorkshopUIController view;
        [SerializeField, Min(.1f)] private float walkingSpeed=3.3f;
        [SerializeField, Min(.01f)] private float doorPassDuration=.4f;
        [SerializeField, Min(.01f)] private float cameraDuration=1.4f;
        [SerializeField, Min(.1f)] private float closeSize=2.8f;
        [SerializeField] private AnimationCurve easing=AnimationCurve.EaseInOut(0,0,1,1);
        private Rigidbody2D body;
        private SpriteRenderer hero;
        private PlayerSpriteAnimator animator;
        private Coroutine sequence;
        private Vector3 savedPosition, savedCamera;
        private Vector2 savedVelocity;
        private Color savedColor;
        private float savedSize, cameraClock;
        private bool savedControl, savedSimulation, savedFollow, savedRenderer, captured;
        public WorkshopTransitionState State { get; private set; }
        public bool Active => State!=WorkshopTransitionState.Outside;
        public bool IsBusy => State==WorkshopTransitionState.Entering || State==WorkshopTransitionState.Exiting;
        public WorkshopUIController View => view;
        public IrisTransitionController Iris => iris;

        public void Configure(PlayerController player,HubUIController hub,Camera camera,Behaviour cameraFollow,
            Transform[] path,Transform focus,WorkshopDoorAnimator door,IrisTransitionController iris,WorkshopUIController view)
        {
            this.player=player; this.hub=hub; worldCamera=camera; this.cameraFollow=cameraFollow;
            entrancePath=path; doorFocus=focus; this.door=door; this.iris=iris; this.view=view;
        }
        public bool Navigate(HubScreen destination)
        {
            if(IsBusy) return false;
            if(State==WorkshopTransitionState.Inside)
            {
                if(destination==HubScreen.Workshop) return false;
                State=WorkshopTransitionState.Exiting; sequence=StartCoroutine(Exit(destination)); return true;
            }
            if(destination!=HubScreen.Workshop || !isActiveAndEnabled || player==null || worldCamera==null || !worldCamera.orthographic ||
                entrancePath==null || entrancePath.Length<2 || view==null || iris==null) return false;
            var death=player.GetComponent<PlayerDeath>();
            if(death!=null && death.IsDead) return false;
            Capture(); State=WorkshopTransitionState.Entering;
            hub.BeginWorkshopSession();
            sequence=StartCoroutine(Enter()); return true;
        }
        private void Capture()
        {
            body=player.GetComponent<Rigidbody2D>(); hero=player.GetComponent<SpriteRenderer>();
            animator=player.GetComponent<PlayerSpriteAnimator>();
            savedPosition=player.transform.position; savedVelocity=body.linearVelocity;
            savedControl=hub.ControlBeforeModal; savedSimulation=body.simulated; savedRenderer=hero.enabled; savedColor=hero.color;
            savedCamera=worldCamera.transform.position; savedSize=worldCamera.orthographicSize;
            savedFollow=cameraFollow!=null && cameraFollow.enabled; captured=true;
            player.SetControlEnabled(false); body.linearVelocity=Vector2.zero; body.simulated=false;
            if(cameraFollow!=null) cameraFollow.enabled=false;
        }
        private IEnumerator Enter()
        {
            cameraClock=0;
            // These are authored foot/step waypoints, traversed continuously in world space.
            for(int i=0;i<entrancePath.Length-1;i++) yield return Walk(entrancePath[i].position,true);
            yield return door.Animate(true);
            yield return Walk(entrancePath[entrancePath.Length-1].position,true,true);
            SetMotion(Vector2.zero);
            while(cameraClock<cameraDuration) { UpdateCamera(true); yield return null; }
            yield return iris.Animate(true,DoorViewport());
            // World/UI visibility changes only while the aperture is fully closed.
            hero.enabled=false;
            yield return door.Animate(false);
            view.ShowLocked();
            yield return iris.Animate(false,new Vector2(.5f,.5f));
            yield return view.Reveal();
            State=WorkshopTransitionState.Inside; sequence=null;
        }
        private IEnumerator Exit(HubScreen destination)
        {
            yield return view.Conceal();
            yield return iris.Animate(true,new Vector2(.5f,.5f));
            view.Hide(); hero.enabled=savedRenderer; hero.color=new Color(savedColor.r,savedColor.g,savedColor.b,0);
            yield return iris.Animate(false,DoorViewport());
            yield return door.Animate(true);
            cameraClock=0;
            for(int i=entrancePath.Length-2;i>=0;i--) yield return Walk(entrancePath[i].position,false,i==entrancePath.Length-2);
            yield return door.Animate(false);
            // Short paths still complete the camera's return before input is released.
            while(cameraClock<cameraDuration) { UpdateCamera(false); yield return null; }
            hub.EndWorkshopSession(); Restore(false);
            State=WorkshopTransitionState.Outside; sequence=null;
            if(destination!=HubScreen.None) hub.Open(destination);
        }
        private IEnumerator Walk(Vector3 target,bool entering,bool passingDoor=false)
        {
            Vector3 start=player.transform.position;
            float duration=Mathf.Max(.12f,Vector2.Distance(start,target)/Mathf.Max(.1f,walkingSpeed));
            if(passingDoor) duration=Mathf.Max(duration,doorPassDuration);
            float elapsed=0;
            while(elapsed<duration)
            {
                elapsed=Mathf.Min(duration,elapsed+Time.unscaledDeltaTime);
                Vector3 next=Vector3.Lerp(start,target,easing.Evaluate(elapsed/duration));
                SetMotion((next-player.transform.position)/Mathf.Max(.0001f,Time.unscaledDeltaTime));
                body.position=next; player.transform.position=next;
                if(passingDoor)
                {
                    float alpha=entering?1-Mathf.SmoothStep(0,1,elapsed/duration):Mathf.SmoothStep(0,1,elapsed/duration);
                    hero.color=new Color(savedColor.r,savedColor.g,savedColor.b,savedColor.a*alpha);
                }
                UpdateCamera(entering); yield return null;
            }
            SetMotion(Vector2.zero);
        }
        private void SetMotion(Vector2 velocity) { if(animator!=null) animator.SetCinematicMotion(true,velocity); }
        private void UpdateCamera(bool entering)
        {
            cameraClock=Mathf.Min(cameraDuration,cameraClock+Time.unscaledDeltaTime);
            float t=easing.Evaluate(cameraClock/Mathf.Max(.01f,cameraDuration));
            Vector3 focus=new Vector3(doorFocus.position.x,doorFocus.position.y+.8f,savedCamera.z);
            worldCamera.transform.position=Vector3.Lerp(entering?savedCamera:focus,entering?focus:savedCamera,t);
            worldCamera.orthographicSize=Mathf.Lerp(entering?savedSize:closeSize,entering?closeSize:savedSize,t);
        }
        private Vector2 DoorViewport() => worldCamera.WorldToViewportPoint(doorFocus.position);
        public void Cancel()
        {
            if(sequence!=null) StopCoroutine(sequence);
            sequence=null;
            if(!captured) return;
            view.Hide(); iris.Clear(); door.Set(0); hub.EndWorkshopSession(); Restore(true);
            State=WorkshopTransitionState.Outside;
        }
        private void Restore(bool interrupted)
        {
            if(!captured) return;
            captured=false;
            if(worldCamera!=null) { worldCamera.transform.position=savedCamera; worldCamera.orthographicSize=savedSize; }
            if(cameraFollow!=null) cameraFollow.enabled=savedFollow;
            if(player==null) return;
            if(interrupted) { body.position=savedPosition; player.transform.position=savedPosition; }
            if(animator!=null) animator.SetCinematicMotion(false,Vector2.zero);
            if(hero!=null) { hero.enabled=savedRenderer; hero.color=savedColor; }
            var death=player.GetComponent<PlayerDeath>();
            if(death!=null && death.IsDead) return;
            body.simulated=savedSimulation; body.linearVelocity=interrupted?savedVelocity:Vector2.zero;
            player.SetControlEnabled(savedControl);
        }
        private void OnDisable() => Cancel();
    }
}
