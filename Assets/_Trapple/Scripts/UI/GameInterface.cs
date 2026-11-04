using System.Collections.Generic;
using Trapple.Bosses;
using Trapple.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trapple.UI
{
    // Uses the same built-in IMGUI system as TutorialGoal; no new UI package or input system.
    [DisallowMultipleComponent]
    public sealed class GameInterface : MonoBehaviour
    {
        [SerializeField] private Color background = new Color(0.045f, 0.085f, 0.085f);
        [SerializeField] private Color accent = new Color(0.76f, 0.94f, 0.43f);
        [SerializeField, Min(1f)] private float hoverSpeed = 12f;
        private readonly Dictionary<string, float> hover = new Dictionary<string, float>();
        private GUIStyle title, subtitle, text, button, small;
        private bool settingsOpen;
        private Texture2D pauseArtwork;
        private Scene trackedScene;
        private BossController boss;
        private BossHealth health;
        private BossPhaseController phases;
        private ArenaLever lever;
        private Matrix4x4 previousMatrix;

        private void Awake() => pauseArtwork = Resources.Load<Texture2D>("Pause/PausePanel");

        private void Update()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene != trackedScene)
            {
                trackedScene = activeScene;
                settingsOpen = false;
                boss = FindAnyObjectByType<BossController>();
                health = boss != null ? boss.GetComponent<BossHealth>() : null;
                phases = boss != null ? boss.GetComponent<BossPhaseController>() : null;
                lever = FindAnyObjectByType<ArenaLever>();
            }
        }

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().path == Trapple.Hub.HubSceneFlowController.HubScenePath ||
                SceneManager.GetActiveScene().path == Trapple.Hub.Illustrated.TrappleUIManager.ScenePath) return;
            var manager = GameManager.Instance;
            if (manager == null) return;
            bool factory = SceneManager.GetActiveScene().path == GameManager.FactoryScene;
            if (factory && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
                Event.current.Use();
            if (factory && manager.State == GameState.Playing) GUIUtility.keyboardControl = 0;
            int previousDepth = GUI.depth;
            GUI.depth = -100;
            EnsureStyles();
            previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Vector2 offset = new Vector2((Screen.width - 1280f * scale) / 2f, (Screen.height - 720f * scale) / 2f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);

            switch (manager.State)
            {
                case GameState.MainMenu: DrawMainMenu(manager); break;
                case GameState.Paused: DrawPause(manager); break;
                case GameState.Victory: DrawVictory(manager); break;
                case GameState.LevelComplete: DrawFactoryComplete(manager); break;
                default:
                    if (boss != null) DrawBossHud();
                    if (factory) DrawFactoryHud();
                    if (DrawButton("pause", new Rect(factory ? 1105f : 1110f, 650f, 150f, 44f),
                        factory ? "ESC • ПАУЗА" : "PAUSE", 18)) manager.Pause();
                    break;
            }
            GUI.matrix = previousMatrix;
            GUI.depth = previousDepth;
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
        }

        private void DrawMainMenu(GameManager manager)
        {
            Backdrop();
            Fill(new Rect(85f, 87f, 42f, 5f), accent);
            GUI.Label(new Rect(85f, 112f, 660f, 42f), "A SMALL STEP. AN UNEXPECTED TRAP.", subtitle);
            GUI.Label(new Rect(78f, 157f, 760f, 115f), "TRAPPLE", title);
            GUI.Label(new Rect(85f, 283f, 590f, 65f), "Reach the apple. Watch your step.\nChallenge the Ancient Tree.", text);
            Fill(new Rect(925f, 215f, 115f, 115f), new Color(0.94f, 0.29f, 0.25f));
            Fill(new Rect(975f, 190f, 12f, 34f), accent);
            Fill(new Rect(990f, 187f, 35f, 15f), accent);
            if (settingsOpen) DrawSettings(new Rect(85f, 370f, 550f, 235f));
            else
            {
                if (DrawButton("play", new Rect(85f, 370f, 340f, 58f), "PLAY")) manager.StartGame();
                if (DrawButton("settings", new Rect(85f, 440f, 340f, 58f), "SETTINGS")) settingsOpen = true;
                if (DrawButton("quit", new Rect(85f, 510f, 340f, 58f), "QUIT"))
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
                if (DrawButton("boss-test", new Rect(850f, 465f, 340f, 58f), "BOSS PROTOTYPE", 22)) manager.StartBossTest();
                GUI.Label(new Rect(850f, 535f, 340f, 70f), "Ancient Tree / test arena\nTwo phases. No weapons.", small);
            }
            GUI.Label(new Rect(85f, 660f, 900f, 30f), "PROTOTYPE 01      /      A, D OR ARROWS: MOVE      SPACE: JUMP      ESC: PAUSE", small);
        }

        private void DrawPause(GameManager manager)
        {
            Backdrop(0.48f);
            const float panelX = 361f;
            if (pauseArtwork != null)
                GUI.DrawTexture(new Rect(panelX, 0f, 558f, 720f), pauseArtwork, ScaleMode.StretchToFill, true);
            else
                Fill(new Rect(390f, 90f, 500f, 520f), new Color(.78f, .62f, .37f, .98f));

            if (settingsOpen)
            {
                Fill(new Rect(454f, 220f, 372f, 352f), new Color(.25f, .13f, .065f, .98f));
                Fill(new Rect(462f, 228f, 356f, 336f), new Color(.91f, .78f, .57f, .99f));
                DrawSettings(new Rect(484f, 260f, 312f, 235f));
                return;
            }
            if (PauseHit(new Rect(panelX + 135f, 222f, 295f, 80f))) manager.Resume();
            if (PauseHit(new Rect(panelX + 135f, 303f, 295f, 78f))) settingsOpen = true;
            if (PauseHit(new Rect(panelX + 135f, 378f, 295f, 74f))) manager.RestartLevel();
            if (PauseHit(new Rect(panelX + 135f, 453f, 295f, 82f))) manager.ReturnToMenu();
        }

        private static bool PauseHit(Rect rect)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0f);
            bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.color = previous;
            return clicked;
        }

        private void DrawVictory(GameManager manager)
        {
            Backdrop(0.95f);
            GUI.Label(new Rect(210f, 150f, 950f, 100f), "FOREST RESTORED", title);
            GUI.Label(new Rect(215f, 275f, 900f, 70f), "ANCIENT TREE DEFEATED\nYou turned the arena against its guardian.", text);
            if (DrawButton("victory-restart", new Rect(410f, 400f, 460f, 58f), "TRY AGAIN")) manager.RestartLevel();
            if (DrawButton("victory-menu", new Rect(410f, 475f, 460f, 58f), "MAIN MENU")) manager.ReturnToMenu();
        }

        private void DrawFactoryHud()
        {
            Fill(new Rect(22f, 20f, 570f, 74f), new Color(.055f, .045f, .065f, .9f));
            GUI.Label(new Rect(40f, 27f, 525f, 30f), "АБАНДОНИРОВАННАЯ ФАБРИКА  /  ЦЕХ № 03", subtitle);
            GUI.Label(new Rect(40f, 59f, 525f, 27f), "E — ПОДОБРАТЬ ШЕСТЕРЁНКУ     F — БРОСИТЬ В ЗАСЛОНКУ", small);
        }

        private void DrawFactoryComplete(GameManager manager)
        {
            Backdrop(.93f);
            Fill(new Rect(260f, 112f, 760f, 496f), new Color(.09f, .055f, .055f, .98f));
            Fill(new Rect(260f, 112f, 760f, 8f), new Color(1f, .39f, .11f));
            GUI.Label(new Rect(320f, 165f, 640f, 50f), "ЦЕХ 03  /  ВЫХОД НАЙДЕН", subtitle);
            GUI.Label(new Rect(320f, 225f, 640f, 100f), "ТЫ ПРОШЁЛ ФАБРИКУ", title);
            var run = FindAnyObjectByType<Trapple.Level.FactoryRun>();
            var time = LabelStyle(52, FontStyle.Bold, new Color(1f, .67f, .24f));
            GUI.Label(new Rect(320f, 350f, 640f, 72f), run != null ? run.TimeLabel : "00:00", time);
            GUI.Label(new Rect(320f, 418f, 640f, 45f), "ВРЕМЯ ПРОХОЖДЕНИЯ", small);
            if (DrawButton("factory-next", new Rect(320f, 505f, 290f, 60f), "К КАРТЕ МИРОВ", 21))
            {
                manager.ReturnToLobby(true);
                GUIUtility.ExitGUI();
            }
            if (DrawButton("factory-again", new Rect(655f, 505f, 300f, 60f), "ПРОЙТИ ЕЩЁ РАЗ", 21))
            {
                manager.RestartLevel();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSettings(Rect area)
        {
            GUI.Label(new Rect(area.x, area.y, area.width, 38f), "ГРОМКОСТЬ", subtitle);
            float volume = GUI.HorizontalSlider(new Rect(area.x, area.y + 55f, area.width - 100f, 32f), AudioListener.volume, 0f, 1f);
            GUI.Label(new Rect(area.xMax - 80f, area.y + 42f, 80f, 40f), Mathf.RoundToInt(volume * 100f) + "%", text);
            if (!Mathf.Approximately(volume, AudioListener.volume))
            {
                AudioListener.volume = volume;
                PlayerPrefs.SetFloat("Trapple.MasterVolume", volume);
            }
            GUI.Label(new Rect(area.x, area.y + 88f, area.width, 58f), "Громкость сохраняется между запусками игры.", small);
            if (DrawButton("back", new Rect(area.x, area.y + 160f, 220f, 54f), "НАЗАД", 22))
            {
                PlayerPrefs.Save();
                settingsOpen = false;
            }
        }

        private void DrawBossHud()
        {
            Fill(new Rect(300f, 20f, 680f, 108f), new Color(0.04f, 0.08f, 0.08f, 0.96f));
            GUI.Label(new Rect(320f, 29f, 650f, 32f), "ANCIENT TREE     /     PHASE " + phases.CurrentPhase, subtitle);
            Fill(new Rect(320f, 74f, 510f, 18f), new Color(0.2f, 0.28f, 0.24f));
            Fill(new Rect(320f, 74f, 510f * health.CurrentHealth / health.MaximumHealth, 18f), accent);
            GUI.Label(new Rect(845f, 62f, 125f, 42f), health.CurrentHealth + " / " + health.MaximumHealth, small);
            string status = boss.State == BossState.Vulnerable ? "EXPOSED!  Return to the lever and press E." :
                boss.State == BossState.Telegraphing ? "WARNING  /  " + AttackName(boss.GetComponent<BossAttack>().CurrentAttack) + "  /  Move away from yellow!" :
                boss.State == BossState.Attacking ? "DANGER  /  " + AttackName(boss.GetComponent<BossAttack>().CurrentAttack) : "Stay alert. The guardian is preparing an attack.";
            Fill(new Rect(220f, 135f, 840f, 43f), new Color(0.04f, 0.08f, 0.08f, 0.92f));
            GUI.Label(new Rect(238f, 140f, 810f, 35f), status, small);
            Fill(new Rect(24f, 590f, 1010f, 100f), new Color(0.04f, 0.08f, 0.08f, 0.9f));
            GUI.Label(new Rect(42f, 602f, 980f, 72f), "A / D OR ARROWS: MOVE     SPACE: JUMP     E: LEVER     ESC: PAUSE\n" +
                (lever != null && lever.CanActivate ? "[ E ] PULL LEVER — drop a stone on the exposed guardian!" :
                "Dodge yellow warnings. After an attack, use the cyan lever on the left. One stone per opening."), small);
        }

        private static string AttackName(BossAttackKind kind) => kind == BossAttackKind.RootAttack ? "ROOTS" :
            kind == BossAttackKind.FallingBranch ? "FALLING BRANCH" : "PLATFORM TRAP";

        private bool DrawButton(string key, Rect rect, string label, int size = 26)
        {
            float value = hover.TryGetValue(key, out float current) ? current : 0f;
            if (Event.current.type == EventType.Repaint)
            {
                value = Mathf.MoveTowards(value, rect.Contains(Event.current.mousePosition) ? 1f : 0f,
                    Time.unscaledDeltaTime * hoverSpeed);
                hover[key] = value;
            }
            rect.x += value * 6f;
            button.fontSize = size;
            GUI.backgroundColor = Color.Lerp(new Color(0.12f, 0.2f, 0.18f), new Color(0.27f, 0.39f, 0.23f), value);
            bool clicked = GUI.Button(rect, label, button);
            GUI.backgroundColor = Color.white;
            Fill(new Rect(rect.x, rect.y, 4f, rect.height), accent);
            return clicked;
        }

        private void Backdrop(float opacity = 1f)
        {
            // Include letterbox regions on ultrawide and portrait windows.
            GUI.matrix = Matrix4x4.identity;
            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(background.r, background.g, background.b, opacity));
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector2((Screen.width - 1280f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f), Quaternion.identity, Vector3.one * scale);
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (title != null) return;
            title = LabelStyle(76, FontStyle.Bold, Color.white);
            subtitle = LabelStyle(22, FontStyle.Bold, accent);
            text = LabelStyle(23, FontStyle.Normal, new Color(0.8f, 0.86f, 0.8f));
            small = LabelStyle(18, FontStyle.Normal, new Color(0.8f, 0.86f, 0.8f));
            button = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                padding = new RectOffset(24, 12, 8, 8), border = new RectOffset() };
            foreach (var state in new[] { button.normal, button.hover, button.active, button.focused })
            {
                state.background = Texture2D.whiteTexture;
                state.textColor = Color.white;
            }
        }

        private static GUIStyle LabelStyle(int size, FontStyle weight, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight,
                wordWrap = true, alignment = TextAnchor.UpperLeft };
            style.normal.textColor = color;
            return style;
        }

        private void OnApplicationQuit() => PlayerPrefs.Save();
    }
}
