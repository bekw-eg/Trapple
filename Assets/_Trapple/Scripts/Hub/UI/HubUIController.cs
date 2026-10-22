using System;
using Trapple.Core;
using Trapple.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Trapple.Hub
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class HubUIController : MonoBehaviour
    {
        [SerializeField] private Illustrated.TrappleUIManager illustrated;
        [SerializeField] private Trapple.Workshop.WorkshopTransitionController workshop;
        public Trapple.Workshop.WorkshopTransitionController Workshop => workshop;
        public void SetWorkshop(Trapple.Workshop.WorkshopTransitionController value) => workshop=value;
        public bool ControlBeforeModal => controlCaptured ? previousControl : player!=null && player.ControlEnabled;
        public Illustrated.TrappleUIManager Illustrated => illustrated;
        public void SetIllustratedUI(Illustrated.TrappleUIManager value) => illustrated = value;
        private VisualElement root, modal, window, content, prompt;
        private Label title, subtitle, seeds, toast;
        private Button close, workshopTab, worldsTab, wardrobeTab, settings;
        private IProgressionService progression;
        private HubConfiguration configuration;
        private IUpgradeService upgradeService;
        private ISceneFlowController flow;
        private PlayerController player;
        private UpgradePanelController upgrades;
        private WorldSelectionController worlds;
        private WardrobeController wardrobe;
        private bool previousControl;
        private bool controlCaptured;
        private bool initialized;
        private IVisualElementScheduledItem hideWindow, hideToast;
        private Slider volume;
        private Label volumeValue;
        private Button menu;
        public HubScreen CurrentScreen { get; private set; }
        public bool IsOpen => CurrentScreen != HubScreen.None;
        public VisualElement Root => root;

        public void Initialize(HubConfiguration configuration, IProgressionService progression,
            IUpgradeService upgradeService, ISceneFlowController flow, PlayerController player)
        {
            if (initialized) return;
            this.configuration = configuration;
            this.upgradeService = upgradeService;
            this.flow = flow;
            this.progression = progression;
            this.player = player;
            root = GetComponent<UIDocument>().rootVisualElement;
            modal = root.Q("modal"); window = root.Q("window"); content = root.Q("content");
            prompt = root.Q("interaction-prompt");
            title = root.Q<Label>("screen-title"); subtitle = root.Q<Label>("screen-subtitle");
            seeds = root.Q<Label>("seeds"); toast = root.Q<Label>("toast");
            close = root.Q<Button>("close"); settings = root.Q<Button>("settings");
            modal.style.display = DisplayStyle.None;
            modal.RemoveFromClassList("visible");
            prompt.RemoveFromClassList("visible");
            // Esc is the keyboard shortcut. A gameplay jump must never submit this HUD button.
            settings.focusable = false;
            root.panel?.focusController?.focusedElement?.Blur();
            root.RegisterCallback<NavigationSubmitEvent>(BlockGameplaySubmit, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(BlockGameplaySpace, TrickleDown.TrickleDown);
            workshopTab = root.Q<Button>("tab-workshop"); worldsTab = root.Q<Button>("tab-worlds"); wardrobeTab = root.Q<Button>("tab-wardrobe");
            if (illustrated == null)
            {
                upgrades = new UpgradePanelController(content.Q("workshop"), configuration, progression, upgradeService, Notify);
                worlds = new WorldSelectionController(content.Q("worlds"), configuration, progression, flow, Notify);
                wardrobe = new WardrobeController(content.Q("wardrobe"), configuration, progression, Notify);
            }
            close.clicked += Close;
            settings.clicked += OpenSettings;
            workshopTab.clicked += OpenWorkshop; worldsTab.clicked += OpenWorlds; wardrobeTab.clicked += OpenWardrobe;
            volume = content.Q<Slider>("volume");
            volumeValue = content.Q<Label>("volume-value");
            volume.SetValueWithoutNotify(AudioListener.volume);
            volumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100f) + "%";
            volume.RegisterValueChangedCallback(ChangeVolume);
            menu = content.Q<Button>("main-menu"); menu.clicked += ReturnToMenu;
            progression.Changed += RefreshCurrency;
            // Keep Tab within the modal. The HUD is removed from keyboard traversal while open.
            window.RegisterCallback<NavigationMoveEvent>(Navigate);
            initialized = true;
            illustrated?.Initialize(configuration, progression, flow, Open, Close, ReturnToMenu);
            if(workshop!=null) workshop.View.Bind(configuration,progression,Close);
            RefreshCurrency();
        }

        private void OpenSettings() => Open(HubScreen.Settings);
        public void HandleEscape()
        {
            if (IsOpen) Close();
            else Open(HubScreen.Settings);
        }

        private void BlockGameplaySubmit(NavigationSubmitEvent evt)
        {
            if (!IsOpen) evt.StopImmediatePropagation();
        }

        private void BlockGameplaySpace(KeyDownEvent evt)
        {
            if (!IsOpen && evt.keyCode == KeyCode.Space) evt.StopImmediatePropagation();
        }
        private void OnEnable()
        {
            if (configuration != null && !initialized) Initialize(configuration, progression, upgradeService, flow, player);
        }
        private void OpenWorkshop() => Open(HubScreen.Workshop);
        private void OpenWorlds() => Open(HubScreen.Worlds);
        private void OpenWardrobe() => Open(HubScreen.Wardrobe);
        private void RefreshCurrency() => seeds.text = progression.Seeds.ToString("N0");
        private void Navigate(NavigationMoveEvent evt)
        {
            // Runtime UI Toolkit already supports Tab/Shift-Tab and Submit for focused buttons.
            if (evt.direction == NavigationMoveEvent.Direction.None) evt.StopPropagation();
        }

        public void Open(HubScreen screen)
        {
            if (!initialized || screen == HubScreen.None) return;
            if(workshop!=null && (screen==HubScreen.Workshop || workshop.Active)) { workshop.Navigate(screen); return; }
            hideWindow?.Pause();
            if (!controlCaptured)
            {
                previousControl = player != null && player.ControlEnabled;
                controlCaptured = true;
                player?.SetControlEnabled(false);
            }
            CurrentScreen = screen;
            bool useIllustrated = illustrated != null && screen != HubScreen.Settings;
            illustrated?.Hide();
            foreach (string cssClass in new[] { "hud", "hub-caption", "controls" })
                root.Q(className: cssClass).style.display = useIllustrated ? DisplayStyle.None : DisplayStyle.Flex;
            if (useIllustrated)
            {
                prompt.RemoveFromClassList("visible");
                modal.style.display = DisplayStyle.None;
                illustrated.Show(screen);
                return;
            }
            settings.focusable = false;
            prompt.RemoveFromClassList("visible");
            modal.style.display = DisplayStyle.Flex;
            modal.schedule.Execute(() => modal.AddToClassList("visible"));
            string[] titles = { "", "МАСТЕРСКАЯ", "ВЫБОР МИРА", "ГАРДЕРОБ", "НАСТРОЙКИ" };
            string[] subtitles = { "", "Сделай своего героя лучше", "У каждого пути — свои сюрпризы", "Маленькие детали. Твой характер.", "Устройся поудобнее" };
            title.text = titles[(int)screen]; subtitle.text = subtitles[(int)screen];
            foreach (var child in content.Children()) child.style.display = DisplayStyle.None;
            string target = screen == HubScreen.Workshop ? "workshop" : screen == HubScreen.Worlds ? "worlds" : screen == HubScreen.Wardrobe ? "wardrobe" : "settings-panel";
            var page = content.Q(target);
            page.style.display = DisplayStyle.Flex;
            page.RemoveFromClassList("page-enter");
            page.schedule.Execute(() => page.AddToClassList("page-enter"));
            workshopTab.EnableInClassList("selected", screen == HubScreen.Workshop);
            worldsTab.EnableInClassList("selected", screen == HubScreen.Worlds);
            wardrobeTab.EnableInClassList("selected", screen == HubScreen.Wardrobe);
            close.Focus();
        }

        public void Close()
        {
            if(workshop!=null && workshop.Active) { workshop.Navigate(HubScreen.None); return; }
            if (!IsOpen) return;
            CurrentScreen = HubScreen.None;
            illustrated?.Hide();
            foreach (string cssClass in new[] { "hud", "hub-caption", "controls" })
                root.Q(className: cssClass).style.display = DisplayStyle.Flex;
            modal.RemoveFromClassList("visible");
            // Keep movement locked through the closing transition, including rapid reopen.
            hideWindow = modal.schedule.Execute(() =>
            {
                if (IsOpen) return;
                modal.style.display = DisplayStyle.None;
                RestoreControl();
                root.panel?.focusController?.focusedElement?.Blur();
            }).StartingIn(180);
        }

        private void RestoreControl()
        {
            if (!controlCaptured) return;
            controlCaptured = false;
            if (player == null) return;
            var death = player.GetComponent<PlayerDeath>();
            if (previousControl && (death == null || !death.IsDead)) player.SetControlEnabled(true);
        }

        public void BeginWorkshopSession()
        {
            hideWindow?.Pause();
            // Workshop captures the prior control state before starting the session.
            controlCaptured=false;
            CurrentScreen=HubScreen.Workshop; illustrated?.Hide(); modal.style.display=DisplayStyle.None;
            prompt.RemoveFromClassList("visible");
            foreach(string cssClass in new[]{"hud","hub-caption","controls"}) root.Q(className:cssClass).style.display=DisplayStyle.None;
            root.panel?.focusController?.focusedElement?.Blur();
        }
        public void EndWorkshopSession()
        {
            hideWindow?.Pause(); controlCaptured=false; CurrentScreen=HubScreen.None;
            if(root==null) return;
            modal.style.display=DisplayStyle.None;
            foreach(string cssClass in new[]{"hud","hub-caption","controls"}) root.Q(className:cssClass).style.display=DisplayStyle.Flex;
        }

        public void SetPrompt(string value)
        {
            if (!initialized) return;
            prompt.EnableInClassList("visible", !string.IsNullOrEmpty(value) && !IsOpen);
            root.Q<Label>("prompt-text").text = value ?? "";
        }

        public void Notify(string message)
        {
            hideToast?.Pause();
            toast.text = message;
            toast.AddToClassList("visible");
            hideToast = toast.schedule.Execute(() => toast.RemoveFromClassList("visible")).StartingIn(2600);
        }

        private void ChangeVolume(ChangeEvent<float> evt)
        {
            AudioListener.volume = evt.newValue;
            volumeValue.text = Mathf.RoundToInt(evt.newValue * 100f) + "%";
            PlayerPrefs.SetFloat("Trapple.MasterVolume", evt.newValue);
            PlayerPrefs.Save();
        }
        private void ReturnToMenu() => GameManager.Instance?.ReturnToMenu();

        private void OnDisable()
        {
            if (!initialized) return;
            if(workshop!=null) workshop.Cancel();
            hideWindow?.Pause(); hideToast?.Pause();
            if (IsOpen || (player != null && !player.ControlEnabled)) RestoreControl();
            CurrentScreen = HubScreen.None;
            illustrated?.Hide();
            close.clicked -= Close; settings.clicked -= OpenSettings;
            foreach (string cssClass in new[] { "hud", "hub-caption", "controls" })
                root.Q(className: cssClass).style.display = DisplayStyle.Flex;
            root.UnregisterCallback<NavigationSubmitEvent>(BlockGameplaySubmit, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(BlockGameplaySpace, TrickleDown.TrickleDown);
            workshopTab.clicked -= OpenWorkshop; worldsTab.clicked -= OpenWorlds; wardrobeTab.clicked -= OpenWardrobe;
            menu.clicked -= ReturnToMenu; volume.UnregisterValueChangedCallback(ChangeVolume);
            window.UnregisterCallback<NavigationMoveEvent>(Navigate);
            progression.Changed -= RefreshCurrency;
            upgrades?.Dispose(); worlds?.Dispose(); wardrobe?.Dispose();
            initialized = false;
        }
    }
}
