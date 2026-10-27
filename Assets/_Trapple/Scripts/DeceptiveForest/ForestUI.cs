using Trapple.Core;
using Trapple.Hub;
using Trapple.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Trapple.DeceptiveForest
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ForestUI : MonoBehaviour
    {
        [SerializeField] private PlayerDeath player;
        [SerializeField] private ForestStick stick;
        [SerializeField] private ForestGoal goal;
        private VisualElement root, modal, window, prompt, icon;
        private Label heading, detail, hint, currency;
        private static bool pickupShown, throwShown;
        private float promptUntil;
        private int promptKind;
        private Button resume, retry, hub, pause;
        private float started;
        private int presentation = -1;
        private bool loading;
        private IProgressionService progression;
        private string pickupHint, throwHint;
        public VisualElement Root => root;
        public bool IsModalOpen => presentation > 0;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            modal = root.Q("modal"); window = root.Q("window");
            prompt = root.Q("prompt"); icon = root.Q("stick-icon");
            pickupHint = stick != null ? stick.PickupKey + " — подобрать палку" : "";
            throwHint = stick != null ? stick.ThrowKey + " — бросить палку" : "";
            heading = root.Q<Label>("heading"); detail = root.Q<Label>("detail");
            hint = root.Q<Label>("hint"); currency = root.Q<Label>("currency");
            resume = root.Q<Button>("resume"); retry = root.Q<Button>("retry"); hub = root.Q<Button>("hub"); pause = root.Q<Button>("pause");
            root.Q<Label>("section").text = "ОБМАНЧИВЫЙ\nЛЕС";
            root.Q<Label>("stage-number").text = "02";
            root.Q<Label>("controls").text = "A / D — движение   •   ПРОБЕЛ — прыжок";
            pause.text = "ESC · ПАУЗА"; pause.focusable = false;
            resume.text = "Продолжить"; retry.text = "Повторить"; hub.text = "В лобби";
            resume.clicked += Resume; retry.clicked += Retry; hub.clicked += ReturnToHub; pause.clicked += Pause;
            root.RegisterCallback<NavigationSubmitEvent>(BlockGameplaySubmit, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(BlockGameplaySpace, TrickleDown.TrickleDown);
            if (stick != null && stick.Icon != null) icon.style.backgroundImage = new StyleBackground(stick.Icon);
            progression = HubSceneFlowController.Instance?.Progression;
            if (progression != null) { progression.Changed += RefreshCurrency; RefreshCurrency(); }
            else currency.style.display = DisplayStyle.None;
            started = Time.unscaledTime;
            Update();
        }
        private void Update()
        {
            if (root == null || GameManager.Instance == null) return;
            int next = player != null && player.IsDead ? 3 : goal != null && goal.IsComplete ? 2 :
                GameManager.Instance.State == GameState.Paused ? 1 : 0;
            if (next != presentation) Show(next);
            float age = Time.unscaledTime - started;
            root.Q("controls").style.display = age < 4f && next == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            icon.style.display = stick != null && stick.IsHeld ? DisplayStyle.Flex : DisplayStyle.None;
            if (next == 0 && stick != null)
            {
                if (stick.IsHeld && !throwShown)
                {
                    throwShown = true; promptKind = 2; promptUntil = Time.time + 3f;
                }
                else if (stick.CanPickup && !pickupShown)
                {
                    pickupShown = true; promptKind = 1; promptUntil = Time.time + 3f;
                }
            }
            bool relevant = stick != null && (promptKind == 1 ? stick.CanPickup : stick.IsHeld);
            hint.text = promptKind == 1 ? pickupHint : throwHint;
            prompt.style.display = next == 0 && relevant && Time.time < promptUntil ? DisplayStyle.Flex : DisplayStyle.None;
            if (next == 2 && Input.GetKeyDown(KeyCode.R)) Retry();
        }
        private void Show(int state)
        {
            presentation = state;
            modal.style.display = state == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            pause.style.display = state == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            window.EnableInClassList("complete", state == 2); window.EnableInClassList("death", state == 3);
            heading.text = state == 1 ? "ПАУЗА" : state == 2 ? "ТРОПА ПРОЙДЕНА!" : "ЛЕС ПЕРЕХИТРИЛ ТЕБЯ";
            detail.text = state == 1 ? "Ложная тропа · уровень 2" : state == 2 ? "Настоящее яблоко найдено." : "Ещё попытка…";
            resume.style.display = state == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            retry.style.display = hub.style.display = state == 3 ? DisplayStyle.None : DisplayStyle.Flex;
            hub.SetEnabled(Application.CanStreamedLevelBeLoaded(HubSceneFlowController.HubScenePath));
            root.panel?.focusController?.focusedElement?.Blur();
            if (state == 1) resume.Focus();
            if (state == 2) retry.Focus();
        }
        public void Pause() { if (!loading && presentation == 0) GameManager.Instance?.Pause(); }
        public void Resume() { if (!loading && presentation == 1) GameManager.Instance?.Resume(); }
        public void Retry()
        {
            if (loading || player == null || player.IsDead) return;
            loading = true; GameManager.Instance.LoadScene(ForestGoal.ScenePath);
        }
        public void ReturnToHub()
        {
            if (loading || player == null || player.IsDead || !Application.CanStreamedLevelBeLoaded(HubSceneFlowController.HubScenePath)) return;
            loading = true; GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath);
        }
        private void RefreshCurrency() => currency.text = "Семечки: " + progression.Seeds;
        private void BlockGameplaySubmit(NavigationSubmitEvent evt) { if (presentation == 0) evt.StopImmediatePropagation(); }
        private void BlockGameplaySpace(KeyDownEvent evt) { if (presentation == 0 && evt.keyCode == KeyCode.Space) evt.StopImmediatePropagation(); }
        private void OnDestroy()
        {
            if (progression != null) progression.Changed -= RefreshCurrency;
            if (resume == null) return;
            resume.clicked -= Resume; retry.clicked -= Retry; hub.clicked -= ReturnToHub; pause.clicked -= Pause;
        }
    }
}
