using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Trapple.Hub.Illustrated
{
    [Serializable]
    public sealed class IllustratedPage
    {
        public HubScreen screen;
        public GameObject root;
        public Button firstButton;
    }

    // The hub facade owns player control and return navigation. This view owns only uGUI.
    public sealed class TrappleUIManager : MonoBehaviour
    {
        public const string ScenePath = "Assets/_Trapple/Scenes/Menu/TrappleUI.unity";
        [SerializeField] private GameObject surface;
        [SerializeField] private IllustratedPage[] pages;
        [SerializeField] private WorldCardUI[] worlds;
        [SerializeField] private UpgradeCardUI[] upgrades;
        [SerializeField] private WardrobePanelUI wardrobe;
        [SerializeField] private TMP_Text currency, message;
        private IProgressionService progression;
        private Action<HubScreen> navigate;
        private Action close, menu;
        private float messageUntil;
        public HubScreen CurrentScreen { get; private set; }
        public string Message => message != null ? message.text : "";
        public bool IsOpen => surface != null && surface.activeSelf;

        public void Configure(GameObject surface, IllustratedPage[] pages, WorldCardUI[] worlds,
            UpgradeCardUI[] upgrades, WardrobePanelUI wardrobe, TMP_Text currency, TMP_Text message)
        {
            this.surface = surface; this.pages = pages; this.worlds = worlds; this.upgrades = upgrades;
            this.wardrobe = wardrobe; this.currency = currency; this.message = message;
        }

        public void Initialize(HubConfiguration config, IProgressionService progress, ISceneFlowController flow,
            Action<HubScreen> navigate, Action close, Action menu)
        {
            if (progression != null) progression.Changed -= Refresh;
            // Also update already-authored scene instances from the earlier opaque prefab.
            var inputBlocker = surface != null ? surface.transform.Find("Backdrop")?.GetComponent<Image>() : null;
            if (inputBlocker != null) inputBlocker.color = Color.clear;
            progression = progress; this.navigate = navigate; this.close = close; this.menu = menu;
            foreach (var card in worlds) if (card != null) card.Bind(config, progress, flow, Notify);
            foreach (var card in upgrades) if (card != null) card.Bind(config, progress, Notify);
            if (wardrobe != null) wardrobe.Bind(config, progress, Notify);
            if (progression != null) progression.Changed += Refresh;
            Hide(); Refresh();
        }

        public void Show(HubScreen screen)
        {
            if (surface == null || progression == null) return;
            surface.SetActive(true); CurrentScreen = screen;
            foreach (var page in pages)
            {
                page.root.SetActive(page.screen == screen);
                if (page.screen == screen && EventSystem.current != null && page.firstButton != null)
                    EventSystem.current.SetSelectedGameObject(page.firstButton.gameObject);
            }
            Refresh();
        }
        public void Hide()
        {
            CurrentScreen = HubScreen.None;
            if (surface != null) surface.SetActive(false);
            if (message != null) message.transform.parent.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        public void OpenWorkshop() => navigate?.Invoke(HubScreen.Workshop);
        public void OpenWorldMap() => navigate?.Invoke(HubScreen.Worlds);
        public void OpenWardrobe() => navigate?.Invoke(HubScreen.Wardrobe);
        public void Close() => close?.Invoke();
        public void ReturnToMenu() => menu?.Invoke();
        public void SecretWorlds() => Notify("Скоро");
        public void Notify(string text)
        {
            if (message == null) return;
            message.text = text; message.transform.parent.gameObject.SetActive(true); messageUntil = Time.unscaledTime + 3.5f;
        }
        public void Refresh()
        {
            if (progression == null) return;
            if (currency != null) currency.text = progression.Seeds.ToString();
            foreach (var card in worlds) if (card != null) card.Refresh();
            foreach (var card in upgrades) if (card != null) card.Refresh();
            if (wardrobe != null) wardrobe.Refresh();
        }
        private void Update()
        {
            if (message != null && message.transform.parent.gameObject.activeSelf && Time.unscaledTime >= messageUntil) message.transform.parent.gameObject.SetActive(false);
        }
        private void OnDestroy() { if (progression != null) progression.Changed -= Refresh; }
    }
}
