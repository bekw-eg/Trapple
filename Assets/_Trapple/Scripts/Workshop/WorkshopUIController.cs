using System.Collections;
using TMPro;
using Trapple.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Trapple.Workshop
{
    public sealed class WorkshopUIController : MonoBehaviour
    {
        [SerializeField] private GameObject surface;
        [SerializeField] private CanvasGroup input;
        [SerializeField] private WorkshopSignAnimator sign;
        [SerializeField] private UpgradePreviewController preview;
        [SerializeField] private Button[] selections;
        [SerializeField] private Button purchase, exit;
        [SerializeField] private TMP_Text[] rowLevels;
        [SerializeField] private Image[] pips;
        [SerializeField] private TMP_Text title, description, stats, cost, balance, feedback;
        private IProgressionService progression;
        private HubConfiguration config;
        private UpgradeKind selected;
        private int purchasedFrame=-1;
        private Coroutine reaction;
        private System.Action close;
        public bool IsOpen => surface!=null && surface.activeSelf;
        public bool Interactable => IsOpen && input.interactable;
        public UpgradePreviewController Preview => preview;
        public string Feedback => feedback.text;
        public void Configure(GameObject surface,CanvasGroup input,WorkshopSignAnimator sign,UpgradePreviewController preview,
            Button[] selections,Button purchase,Button exit,TMP_Text[] rowLevels,Image[] pips,
            TMP_Text title,TMP_Text description,TMP_Text stats,TMP_Text cost,TMP_Text balance,TMP_Text feedback)
        {
            this.surface=surface; this.input=input; this.sign=sign; this.preview=preview; this.selections=selections;
            this.purchase=purchase; this.exit=exit; this.rowLevels=rowLevels; this.pips=pips;
            this.title=title; this.description=description; this.stats=stats; this.cost=cost; this.balance=balance; this.feedback=feedback;
        }
        public void Bind(HubConfiguration config,IProgressionService progression,System.Action close)
        {
            if(this.progression!=null) this.progression.Changed-=Refresh;
            this.config=config; this.progression=progression; this.close=close;
            progression.Changed+=Refresh; preview.Bind(config,progression);
            purchase.onClick.RemoveAllListeners(); purchase.onClick.AddListener(Purchase);
            exit.onClick.RemoveAllListeners(); exit.onClick.AddListener(()=>{if(Interactable) this.close?.Invoke();});
            for(int i=0;i<selections.Length;i++)
            {
                var kind=(UpgradeKind)i; selections[i].onClick.RemoveAllListeners();
                selections[i].onClick.AddListener(()=>{if(Interactable) Select(kind);});
            }
            Hide();
        }
        public void ShowLocked()
        {
            surface.SetActive(true); input.interactable=false; input.blocksRaycasts=true;
            sign.ResetHidden(); Select(selected); feedback.text="Выбери улучшение и посмотри, как меняется герой.";
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
        }
        public IEnumerator Reveal()
        {
            yield return sign.Animate(true); input.interactable=true;
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(selections[(int)selected].gameObject);
        }
        public IEnumerator Conceal()
        {
            input.interactable=false;
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
            yield return sign.Animate(false);
        }
        public void Hide() { input.interactable=false; surface.SetActive(false); }
        public void Select(UpgradeKind kind) { selected=kind; preview.Select(kind); Refresh(); }
        private void Refresh()
        {
            if(progression==null) return;
            balance.text=progression.Seeds.ToString();
            for(int i=0;i<selections.Length;i++)
            {
                rowLevels[i].text="УРОВЕНЬ "+progression.Level((UpgradeKind)i)+" / 3";
                selections[i].GetComponent<Image>().color=i==(int)selected?new Color(.75f,.88f,.6f):new Color(1,.86f,.61f);
            }
            var def=config.Upgrade(selected); if(def==null) { purchase.interactable=false; return; }
            int level=progression.Level(selected); bool max=level>=def.MaxLevel;
            title.text=def.title; description.text=def.description;
            stats.text="Текущий уровень:  +"+(level*def.percentPerLevel).ToString("0.#")+"%\n"+
                (max?"Достигнут максимальный уровень":"Следующий уровень:  +"+((level+1)*def.percentPerLevel).ToString("0.#")+"%");
            cost.text=max?"МАКСИМУМ":def.Cost(level)>0?def.Cost(level)+" семечек":"Цена ещё не настроена";
            purchase.interactable=!max;
            purchase.GetComponentInChildren<TMP_Text>().text=max?"МАКСИМУМ":"УЛУЧШИТЬ ↑";
            for(int i=0;i<pips.Length;i++) pips[i].color=i<level?new Color(.1f,.48f,.18f):new Color(.73f,.61f,.39f,.45f);
            preview.Refresh();
        }
        public void Purchase()
        {
            if(!Interactable || purchasedFrame==Time.frameCount || progression==null) return;
            var def=config.Upgrade(selected); if(def==null) return;
            int level=progression.Level(selected);
            if(level>=def.MaxLevel) { feedback.text="Достигнут максимальный уровень"; return; }
            if(def.Cost(level)<=0) { feedback.text="Цена улучшения ещё не настроена"; return; }
            if(progression.Seeds<def.Cost(level)) { feedback.text="Недостаточно семечек"; return; }
            if(!progression.TryUpgrade(def)) return;
            purchasedFrame=Time.frameCount; feedback.text=def.title+" · уровень "+progression.Level(selected);
            if(reaction!=null) StopCoroutine(reaction);
            reaction=StartCoroutine(React());
        }
        private IEnumerator React()
        {
            float elapsed=0;
            while(elapsed<.4f)
            {
                elapsed+=Time.unscaledDeltaTime;
                float pulse=Mathf.Sin(Mathf.Clamp01(elapsed/.4f)*Mathf.PI);
                purchase.transform.localScale=Vector3.one*(1-.06f*pulse);
                foreach(var pip in pips) pip.transform.localScale=Vector3.one*(1+.25f*pulse);
                yield return null;
            }
            ResetReaction(); reaction=null;
        }
        private void ResetReaction()
        { purchase.transform.localScale=Vector3.one; foreach(var pip in pips) pip.transform.localScale=Vector3.one; }
        private void OnDisable() { if(reaction!=null) { StopCoroutine(reaction); reaction=null; ResetReaction(); } }
        private void OnDestroy() { if(progression!=null) progression.Changed-=Refresh; }
    }
}
