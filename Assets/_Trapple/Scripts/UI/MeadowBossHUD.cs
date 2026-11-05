using TMPro;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.UI
{
    public sealed class MeadowBossHUD : MonoBehaviour
    {
        [SerializeField] private MeadowBossEncounter encounter;
        [SerializeField] private CanvasGroup approach, introduction, fight, completion;
        [SerializeField] private TMP_Text timer, attackHint, stage, result;
        [SerializeField] private Image progressFill;
        [SerializeField] private Button continueButton, hubButton;
        private Canvas view;
        private float shownProgress;
        private bool focused;
        private LevelCourse course;
        private CanvasGroup routeGroup;
        private Image routeFill;
        private TMP_Text routeLabel;
        private RectTransform appleIcon;
        private float shownRoute, routePulse;
        private int routeMilestones;
        private void Start()
        {
            view = GetComponent<Canvas>();
            completion.gameObject.SetActive(false);
            continueButton.onClick.AddListener(encounter.Continue); hubButton.onClick.AddListener(encounter.GoToHub);
            course = FindAnyObjectByType<LevelCourse>();
            if (course != null) BuildRouteHeader();
        }
        private static void Fade(CanvasGroup group, bool show)
        { group.alpha = Mathf.MoveTowards(group.alpha, show ? 1 : 0, Time.deltaTime * 3); }
        private void Update()
        {
            if (encounter == null) return;
            var state = encounter.State;
            Fade(approach, state == MeadowEncounterState.Route && encounter.IsNearArena);
            Fade(introduction, state == MeadowEncounterState.Intro);
            bool fighting = state == MeadowEncounterState.Fighting;
            Fade(fight, fighting || state == MeadowEncounterState.Resolving);
            bool complete = state == MeadowEncounterState.Complete;
            completion.gameObject.SetActive(complete);
            Fade(completion, complete); completion.blocksRaycasts = complete; completion.interactable = complete;
            UpdateRouteHeader(state);
            shownProgress = Mathf.Lerp(shownProgress, encounter.Progress, 1 - Mathf.Exp(-Time.deltaTime * 6));
            progressFill.fillAmount = shownProgress;
            timer.text = Mathf.CeilToInt(encounter.TimeRemaining).ToString("00") + " s";
            timer.color = encounter.TimeRemaining <= 10 ? new Color(1, .67f, .24f) : new Color(1, .95f, .77f);
            stage.text = state == MeadowEncounterState.Resolving ? "THE WARDEN RESTS" :
                encounter.Progress < .333f ? "I  /  FIND YOUR RHYTHM" : encounter.Progress < .666f ? "II  /  KEEP MOVING" : "III  /  ALMOST THERE";
            var boss = encounter.Boss;
            bool warning = fighting && (boss.IsTelegraphing || boss.IsSpecialAttack && boss.State == GuardianState.Attacking);
            attackHint.text = warning ? boss.AttackAdvice :
                state == MeadowEncounterState.Resolving ? "WELL PLAYED, APPI" : "DODGE UNTIL THE TIMER ENDS";
            attackHint.color = warning ? new Color(1, .79f, .34f) : new Color(.91f, .93f, .73f);
            if (complete && !focused) { focused = true; result.text = "The old guardian lets you pass."; continueButton.Select(); }
            var manager = GameManager.Instance;
            view.enabled = manager == null || manager.State == GameState.Playing || complete;
        }

        private void BuildRouteHeader()
        {
            var root = new GameObject("Apple Meadow Route", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(.5f, 1f);
            rootRect.pivot = new Vector2(.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -38f);
            rootRect.sizeDelta = new Vector2(960f, 150f);
            routeGroup = root.GetComponent<CanvasGroup>();

            CreateRouteImage("Leafy route track", root.transform, new Vector2(0f, -77f),
                new Vector2(680f, 22f), new Color(.08f, .18f, .13f, .95f));
            var fillObject = CreateRouteImage("Meadow route fill", root.transform, new Vector2(0f, -77f),
                new Vector2(672f, 14f), new Color(.45f, .76f, .3f));
            routeFill = fillObject;
            routeFill.type = Image.Type.Filled;
            routeFill.fillMethod = Image.FillMethod.Horizontal;
            routeFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            routeFill.raycastTarget = false;

            CreateRouteIcon("Meadow start flag", root.transform, "AppleMeadow/Flag",
                new Vector2(-390f, -75f), new Vector2(54f, 54f));
            var apple = CreateRouteIcon("Meadow apple destination", root.transform, "AppleMeadow/Apple",
                new Vector2(390f, -75f), new Vector2(54f, 54f));
            appleIcon = apple.rectTransform;
            routeLabel = Instantiate(timer, root.transform);
            routeLabel.name = "Meadow route label";
            routeLabel.text = "APPLE MEADOW  /  0%";
            routeLabel.fontSize = 52f;
            routeLabel.color = new Color(1f, .94f, .75f);
            routeLabel.alignment = TextAlignmentOptions.Center;
            routeLabel.raycastTarget = false;
            var labelRect = routeLabel.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, 1f);
            labelRect.pivot = new Vector2(.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -1f);
            labelRect.sizeDelta = new Vector2(840f, 66f);
        }

        private void UpdateRouteHeader(MeadowEncounterState state)
        {
            if (routeGroup == null || course == null || course.Player == null) return;
            bool show = state == MeadowEncounterState.Route ||
                        state == MeadowEncounterState.Intro && encounter.IntroProgress < .2f;
            routeGroup.alpha = Mathf.MoveTowards(routeGroup.alpha, show ? 1f : 0f, Time.deltaTime * 2.5f);

            Vector2 bounds = course.WorldBounds;
            float span = Mathf.Max(.001f, bounds.y - bounds.x);
            float target = state == MeadowEncounterState.Route
                ? Mathf.Clamp01((course.Player.transform.position.x - bounds.x) / span)
                : 1f;
            shownRoute = Mathf.Max(shownRoute, target);
            routeFill.fillAmount = shownRoute;
            routeLabel.text = "APPLE MEADOW  /  " + Mathf.RoundToInt(shownRoute * 100f) + "%";

            int milestone = Mathf.FloorToInt(shownRoute * 4f + .001f);
            if (milestone > routeMilestones)
            {
                routeMilestones = milestone;
                routePulse = 1f;
            }
            routePulse = Mathf.MoveTowards(routePulse, 0f, Time.deltaTime * 1.6f);
            routeFill.color = Color.Lerp(new Color(.45f, .76f, .3f), new Color(1f, .9f, .45f), routePulse);
            float scale = 1f + routePulse * .15f + (shownRoute > .75f ? Mathf.Sin(Time.time * 4f) * .045f : 0f);
            appleIcon.localScale = Vector3.one * scale;
        }

        private static Image CreateRouteImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateRouteIcon(string name, Transform parent, string resourcePath, Vector2 position, Vector2 size)
        {
            var icon = CreateRouteImage(name, parent, position, size, Color.white);
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length > 0) icon.sprite = sprites[0];
            icon.preserveAspect = true;
            return icon;
        }
    }
}
