using TMPro;
using Trapple.Bosses;
using Trapple.Core;
using Trapple.DeceptiveForest;
using Trapple.Hub;
using Trapple.Level;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Trapple.UI
{
    // GameManager still owns pause, control and scene loading; this replaces its IMGUI view.
    public sealed class GameInterface : MonoBehaviour
    {
        [SerializeField] private GameObject menu, pause, complete, settings, hud, hubShortcuts;
        [SerializeField] private TMP_Text seeds, bossText, completionDetail;
        [SerializeField] private ControlHintView controlHint;
        [SerializeField] private TimedNotice notice;
        [SerializeField] private Image heldItem;
        [SerializeField] private Button[] buttons;
        [SerializeField] private DeathScreenUI deathScreen;
        private HubUIController hub;
        private HubInteractionController interaction;
        private TutorialGoal tutorial;
        private ForestGoal forest;
        private ForestUI forestPresentation;
        private BossHealth health;
        private BossPhaseController phases;
        private ArenaLever lever;
        private Trapple.Player.PlayerDeath player;
        private GameState shownState = (GameState)(-1);
        private bool settingsOpen, shownHubModal;
        private TutorialControlHints tutorialHints;
        private IProgressionService progress;
        private GameManager Manager => GameManager.Instance;
        public bool SettingsOpen => settingsOpen;
        public void SetHeldItem(Image value) => heldItem=value;
        public void SetDeathScreen(DeathScreenUI value) => deathScreen=value;
        public void Configure(GameObject[] panels, TMP_Text[] labels, ControlHintView hint, TimedNotice warning, Button[] controls)
        {
            menu=panels[0]; pause=panels[1]; complete=panels[2]; settings=panels[3]; hud=panels[4];
            hubShortcuts=panels[5];
            seeds=labels[0]; bossText=labels[1]; completionDetail=labels[2];
            controlHint=hint; notice=warning; buttons=controls;
        }
        private void Awake()
        {
            buttons[0].onClick.AddListener(()=>Manager.StartGame()); buttons[1].onClick.AddListener(OpenSettings);
            buttons[2].onClick.AddListener(Quit); buttons[3].onClick.AddListener(()=>Manager.Resume());
            buttons[4].onClick.AddListener(OpenSettings); buttons[5].onClick.AddListener(()=>Manager.RestartLevel());
            buttons[6].onClick.AddListener(ReturnToHub); buttons[7].onClick.AddListener(()=>Manager.ReturnToMenu());
            buttons[8].onClick.AddListener(()=>Manager.RestartLevel()); buttons[9].onClick.AddListener(NextLevel);
            buttons[10].onClick.AddListener(ReturnToHub); buttons[11].onClick.AddListener(CloseSettings);
            buttons[12].onClick.AddListener(()=>Manager.Pause()); buttons[13].onClick.AddListener(()=>hub?.Open(HubScreen.Worlds));
            buttons[14].onClick.AddListener(()=>hub?.Open(HubScreen.Workshop)); buttons[15].onClick.AddListener(()=>hub?.Open(HubScreen.Wardrobe));
            buttons[16].onClick.AddListener(()=>hub?.Open(HubScreen.Settings));
            SceneManager.sceneLoaded+=SceneLoaded; SceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(mode==LoadSceneMode.Additive) return;
            settingsOpen=false; shownState=(GameState)(-1);
            deathScreen.Hide();
            hub=FindAnyObjectByType<HubUIController>(); interaction=FindAnyObjectByType<HubInteractionController>();
            tutorial=FindAnyObjectByType<TutorialGoal>(); forest=FindAnyObjectByType<ForestGoal>();
            forestPresentation=FindAnyObjectByType<ForestUI>(); health=FindAnyObjectByType<BossHealth>();
            phases=FindAnyObjectByType<BossPhaseController>(); lever=FindAnyObjectByType<ArenaLever>();
            player=FindAnyObjectByType<Trapple.Player.PlayerDeath>();
            tutorialHints=FindAnyObjectByType<TutorialControlHints>();
            controlHint.SetTopOffset(hub!=null?144:32);
            controlHint.Show(ControlHint.None); notice.Clear();
            if(tutorial!=null || forest!=null) notice.Show("WATCH YOUR STEP",5f);
            seeds.transform.parent.gameObject.SetActive(progress!=null);
            menu.SetActive(false); pause.SetActive(false); complete.SetActive(false); settings.SetActive(false);
            if(EventSystem.current==null) new GameObject("UI EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        }
        private void LateUpdate()
        {
            if(Manager==null) return;
            bool finished=(tutorial!=null && tutorial.IsComplete) || (forest!=null && forest.IsComplete) || Manager.State==GameState.Victory;
            bool hubModal=hub!=null && hub.IsOpen;
            bool dying=Manager.State==GameState.Dying || Manager.State==GameState.Dead;
            var state=!dying && finished && Manager.State!=GameState.Paused?GameState.Victory:Manager.State;
            if(state!=shownState || hubModal!=shownHubModal)
            {
                if(state!=shownState) settingsOpen=false;
                shownState=state; shownHubModal=hubModal; RefreshPanels();
            }
            var current=HubSceneFlowController.Instance?.Progression;
            if(progress!=current)
            {
                if(progress!=null) progress.Changed-=RefreshSeeds;
                progress=current; if(progress!=null) progress.Changed+=RefreshSeeds; RefreshSeeds();
            }
            bool playing=shownState==GameState.Playing && !hubModal && !settingsOpen && (player==null || !player.IsDead);
            hud.SetActive(playing); hubShortcuts.SetActive(hub!=null && !dying);
            buttons[12].gameObject.SetActive(hub==null);
            notice.Tick(playing);
            RefreshPrompt(playing);
            if(heldItem!=null)
            {
                heldItem.sprite=forestPresentation!=null?forestPresentation.HeldItemIcon:null;
                heldItem.transform.parent.gameObject.SetActive(playing && heldItem.sprite!=null);
            }
            bossText.text=health!=null?"ANCIENT TREE  "+health.CurrentHealth+" / "+health.MaximumHealth+
                "   PHASE "+(phases!=null?phases.CurrentPhase:1):"";
            if(EventSystem.current!=null) EventSystem.current.sendNavigationEvents=!playing;
        }
        private void RefreshPanels()
        {
            if(shownState==GameState.Dying || shownState==GameState.Dead)
            {
                if(interaction!=null) interaction.enabled=false;
                if(hub!=null && hub.IsOpen) hub.Close();
            }
            menu.SetActive(shownState==GameState.MainMenu && !settingsOpen);
            pause.SetActive(shownState==GameState.Paused && !settingsOpen);
            complete.SetActive(shownState==GameState.Victory && !settingsOpen);
            settings.SetActive(settingsOpen);
            if(shownState==GameState.Victory)
            {
                completionDetail.text=health!=null?"ANCIENT TREE DEFEATED":"THE APPLE IS SAFE";
                buttons[9].interactable=TryNext(out _,out _);
            }
            Button focus=settingsOpen?buttons[11]:shownState==GameState.MainMenu?buttons[0]:
                shownState==GameState.Paused?buttons[3]:shownState==GameState.Victory?buttons[8]:null;
            if(EventSystem.current!=null && !shownHubModal)
                EventSystem.current.SetSelectedGameObject(focus!=null?focus.gameObject:null);
            if(shownState==GameState.Dead) deathScreen.Show(Manager.DeathScenePath);
            else deathScreen.Hide();
        }
        private void RefreshSeeds()
        {
            seeds.text=progress!=null?progress.Seeds.ToString():"";
            seeds.transform.parent.gameObject.SetActive(progress!=null);
        }
        private void RefreshPrompt(bool playing)
        {
            if (playing && interaction != null && interaction.ActiveMap != null && interaction.ActiveMap.IsInteracting)
            {
                controlHint.Show(ControlHint.Map, "CLICK A WORLD / ESC TO CLOSE");
                return;
            }
            string label=""; bool throwing=false;
            if(playing && interaction?.Nearest!=null)
                label=interaction.Nearest.Screen==HubScreen.Workshop?"ENTER WORKSHOP":
                    interaction.Nearest.Screen==HubScreen.Wardrobe?"OPEN WARDROBE":"OPEN WORLD MAP";
            if(playing && forestPresentation!=null && forestPresentation.IsHintVisible)
            {
                throwing=forestPresentation.HintIsThrow; label=throwing?"THROW STICK":"PICK UP STICK";
            }
            if(playing && lever!=null && lever.CanActivate) label="PULL LEVER";
            if(!string.IsNullOrEmpty(label)) controlHint.Show(throwing?ControlHint.Throw:ControlHint.Interact,label);
            else controlHint.Show(playing && tutorialHints!=null?tutorialHints.Current:ControlHint.None);
        }
        public void OpenSettings()
        {
            if(Manager.State==GameState.Dying || Manager.State==GameState.Dead) return;
            settingsOpen=true; RefreshPanels();
        }
        public void CloseSettings() { settingsOpen=false; RefreshPanels(); }
        public bool HandleEscape()
        {
            if(player!=null && player.IsDead || shownState==GameState.Victory) return true;
            if(!settingsOpen) return false; CloseSettings(); return true;
        }
        private static void ReturnToHub()
        {
            if(HubSceneFlowController.Instance!=null) HubSceneFlowController.Instance.ReturnToHub();
            else GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath);
        }
        private bool TryNext(out WorldDefinition world,out int index)
        {
            world=null; index=-1; var flow=HubSceneFlowController.Instance;
            if(flow==null) return false;
            string path=SceneManager.GetActiveScene().path;
            foreach(var value in flow.Configuration.worlds)
            {
                int current=System.Array.IndexOf(value.levelScenes??System.Array.Empty<string>(),path);
                if(current<0 || !flow.CanLaunch(value,current+1)) continue;
                world=value; index=current+1; return true;
            }
            return false;
        }
        private void NextLevel() { if(TryNext(out var world,out int index)) HubSceneFlowController.Instance.TryLaunch(world,index); }
        private static void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded-=SceneLoaded; if(progress!=null) progress.Changed-=RefreshSeeds;
        }
    }
}
