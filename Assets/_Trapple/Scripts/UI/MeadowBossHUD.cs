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
        private void Start()
        {
            view = GetComponent<Canvas>();
            completion.gameObject.SetActive(false);
            continueButton.onClick.AddListener(encounter.Continue); hubButton.onClick.AddListener(encounter.GoToHub);
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
    }
}
