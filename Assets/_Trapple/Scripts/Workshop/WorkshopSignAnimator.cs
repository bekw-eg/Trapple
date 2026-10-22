using System.Collections;
using UnityEngine;

namespace Trapple.Workshop
{
    public sealed class WorkshopSignAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform sign;
        [SerializeField] private CanvasGroup cards;
        [SerializeField, Min(.01f)] private float duration=.7f;
        [SerializeField] private AnimationCurve rise=AnimationCurve.EaseInOut(0,0,1,1);
        private Vector2 resting;
        private bool initialized;
        public void Configure(RectTransform sign,CanvasGroup cards) { this.sign=sign; this.cards=cards; }
        private void Initialize() { if(initialized) return; resting=sign.anchoredPosition; initialized=true; }
        public void ResetHidden()
        {
            Initialize(); sign.anchoredPosition=resting+Vector2.down*400; sign.localRotation=Quaternion.identity;
            cards.alpha=0; cards.interactable=false; cards.blocksRaycasts=false;
        }
        public IEnumerator Animate(bool show)
        {
            Initialize(); cards.interactable=false; cards.blocksRaycasts=false;
            float elapsed=0;
            do
            {
                float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration));
                float amount=show?rise.Evaluate(t):1-rise.Evaluate(t);
                sign.anchoredPosition=resting+Vector2.down*(1-amount)*400;
                sign.localRotation=Quaternion.Euler(0,0,show?Mathf.Sin(t*Mathf.PI*3)*(1-t)*3:0);
                cards.alpha=show?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,t)):1-t;
                if(elapsed>=duration) break;
                elapsed=Mathf.Min(duration,elapsed+Time.unscaledDeltaTime); yield return null;
            } while(true);
            cards.interactable=show; cards.blocksRaycasts=show;
        }
    }
}
