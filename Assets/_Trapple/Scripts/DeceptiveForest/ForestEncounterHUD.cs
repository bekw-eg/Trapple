using TMPro;
using Trapple.Core;
using Trapple.Bosses;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.DeceptiveForest
{
    public sealed class ForestEncounterHUD : MonoBehaviour
    {
        [SerializeField] private ForestGuardianEncounter encounter;
        [SerializeField] private CanvasGroup routeGroup, combatGroup, titleGroup;
        [SerializeField] private Image routeFill, healthFill;
        [SerializeField] private RectTransform bossIcon;
        [SerializeField] private TMP_Text routeText, timerText, stickText, subtitle;
        [SerializeField] private GameObject victory;
        [SerializeField] private Button continueButton, hubButton;
        private float shownRoute, shownHealth = 1, shownSticks, pulse;
        private int milestones;
        private bool focused;
        private Canvas view;
        private Transform inventoryIcon;
        private Transform throwControl;
        private TMP_Text attackHint;
        private void Start()
        {
            view = GetComponent<Canvas>();
            inventoryIcon = transform.Find("Collected stick");
            throwControl = transform.Find("Throw control");
            attackHint = Instantiate(timerText, transform);
            attackHint.name = "Attack advice"; attackHint.fontSize = 64f; attackHint.raycastTarget = false;
            var hintRect = attackHint.rectTransform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 75f); hintRect.sizeDelta = new Vector2(1700f, 100f);
            attackHint.gameObject.SetActive(false);
            continueButton.onClick.AddListener(encounter.Continue);
            hubButton.onClick.AddListener(encounter.GoToHub);
            victory.SetActive(false);
        }
        private void Update()
        {
            if (encounter == null) return;
            var state = encounter.State;
            bool route = state == ForestEncounterState.Route || state == ForestEncounterState.Intro && encounter.IntroProgress < .2f;
            bool combat = state == ForestEncounterState.BossFight || state == ForestEncounterState.Resolving;
            bool showInventory = state != ForestEncounterState.Intro && state != ForestEncounterState.Complete;
            stickText.gameObject.SetActive(showInventory);
            if (inventoryIcon != null) inventoryIcon.gameObject.SetActive(showInventory);
            if (throwControl != null) throwControl.gameObject.SetActive(showInventory);
            float dt = Time.deltaTime;
            routeGroup.alpha = Mathf.MoveTowards(routeGroup.alpha, route ? 1 : 0, dt * 2.5f);
            combatGroup.alpha = Mathf.MoveTowards(combatGroup.alpha, combat ? 1 : 0, dt * 3);
            titleGroup.alpha = Mathf.MoveTowards(titleGroup.alpha, state == ForestEncounterState.Intro ? 1 : 0, dt * 2);
            shownRoute = Mathf.Lerp(shownRoute, encounter.Progress.Progress, 1 - Mathf.Exp(-dt * 5));
            routeFill.fillAmount = shownRoute;
            routeText.text = "DECEPTIVE FOREST  /  " + Mathf.RoundToInt(shownRoute * 100) + "%";
            int milestone = Mathf.FloorToInt(encounter.Progress.Progress * 4 + .001f);
            if (milestone > milestones) { milestones = milestone; pulse = 1; }
            pulse = Mathf.MoveTowards(pulse, 0, dt * 1.6f);
            routeFill.color = Color.Lerp(new Color(.45f, .76f, .3f), new Color(1, .9f, .45f), pulse);
            float scale = 1 + pulse * .15f + (shownRoute > .75f ? Mathf.Sin(Time.time * 4) * .045f : 0);
            bossIcon.localScale = Vector3.one * scale;
            var health = encounter.Guardian.Health;
            if (health != null) shownHealth = Mathf.Lerp(shownHealth, (float)health.CurrentHealth / health.MaximumHealth, 1 - Mathf.Exp(-dt * 8));
            healthFill.fillAmount = shownHealth;
            timerText.text = "SURVIVE  " + Mathf.CeilToInt(encounter.TimeRemaining).ToString("00") + " s";
            timerText.color = encounter.TimeRemaining < 10 ? new Color(1, .65f, .38f) : new Color(1, .94f, .75f);
            shownSticks = Mathf.MoveTowards(shownSticks, encounter.Inventory.Count, dt * 14);
            stickText.text = "x " + Mathf.RoundToInt(shownSticks);
            bool attacking = state == ForestEncounterState.BossFight &&
                (encounter.Guardian.IsTelegraphing || encounter.Guardian.State == GuardianState.Attacking);
            attackHint.gameObject.SetActive(attacking);
            if (attacking) { attackHint.text = encounter.Guardian.AttackAdvice; attackHint.color = new Color(1f, .83f, .35f); }
            bool complete = state == ForestEncounterState.Complete;
            victory.SetActive(complete);
            if (complete && !focused)
            {
                focused = true;
                subtitle.text = encounter.Retreated ? "The guardian rests. The forest is free." : "The guardian yields. The forest is free.";
                continueButton.Select();
            }
            bool visible = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing || complete;
            view.enabled = visible;
        }
    }
}
