using TMPro;
using Trapple.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Hub.Illustrated
{
    public sealed class WorldMapPanelUI : MonoBehaviour
    {
        [SerializeField] private Button[] worlds;
        [SerializeField] private Transform levels;
        [SerializeField] private Button levelTemplate, play;
        [SerializeField] private TMP_Text details;
        [SerializeField] private Sprite[] numbers;
        [SerializeField] private Sprite locked, completed;
        private HubConfiguration config;
        private IProgressionService progress;
        private ISceneFlowController flow;
        private WorldDefinition selected;
        private int selectedLevel;
        private bool launching;
        private Button[] levelButtons;
        private string builtFor;
        public int SelectedLevel => selectedLevel;
        public WorldDefinition SelectedWorld => selected;
        public void Configure(Button[] worlds, Transform levels, Button template, Button play, TMP_Text details,
            Sprite[] numbers, Sprite locked, Sprite completed)
        {
            this.worlds = worlds; this.levels = levels; levelTemplate = template; this.play = play;
            this.details = details; this.numbers = numbers; this.locked = locked; this.completed = completed;
        }
        public void Bind(HubConfiguration config, IProgressionService progress, ISceneFlowController flow)
        {
            this.config = config; this.progress = progress; this.flow = flow;
            for (int i = 0; i < worlds.Length; i++)
            {
                int index = i; worlds[i].onClick.RemoveAllListeners();
                worlds[i].onClick.AddListener(() => SelectWorld(index));
            }
            play.onClick.RemoveAllListeners(); play.onClick.AddListener(Play);
            SelectWorld(0);
        }
        public void SelectWorld(int index)
        {
            if (config == null || index < 0 || index >= config.worlds.Length) return;
            selected = config.worlds[index]; selectedLevel = -1; launching = false; Refresh();
        }
        public void SelectWorld(string id)
        {
            if (config == null) return;
            int index = System.Array.FindIndex(config.worlds, world => world != null && world.id == id);
            if (index >= 0) SelectWorld(index);
        }
        public void Refresh()
        {
            if (config == null || progress == null || selected == null) return;
            for (int i = 0; i < worlds.Length; i++)
            {
                worlds[i].interactable = i < config.worlds.Length && config.worlds[i] != null;
                worlds[i].GetComponent<UIButtonAnimator>().SetChosen(i < config.worlds.Length && config.worlds[i] == selected);
                var marker=worlds[i].transform.Find("Locked");
                if(marker!=null) marker.gameObject.SetActive(i < config.worlds.Length && !progress.IsWorldUnlocked(config.worlds[i]));
            }
            // Remove retired rows immediately from layout and navigation before Destroy's end-of-frame pass.
            if(builtFor!=selected.id || levelButtons==null || levelButtons.Length!=selected.plannedLevels)
            {
                foreach (Transform child in levels) if (child.gameObject != levelTemplate.gameObject) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                builtFor=selected.id; levelButtons=new Button[selected.plannedLevels];
                for(int i=0;i<levelButtons.Length;i++)
                {
                    int level=i; var button=Instantiate(levelTemplate,levels); button.name="Level "+(i+1); levelButtons[i]=button;
                    button.gameObject.SetActive(true); button.onClick.AddListener(()=>{ selectedLevel=level; Refresh(); });
                }
            }
            bool unlocked = progress.IsWorldUnlocked(selected);
            if (selectedLevel < 0)
                for (int i = 0; i < selected.plannedLevels; i++)
                    if (unlocked && flow.CanLaunch(selected, i)) { selectedLevel = i; break; }
            for (int i = 0; i < selected.plannedLevels; i++)
            {
                var button = levelButtons[i];
                bool available = unlocked && flow.CanLaunch(selected, i);
                var image = button.GetComponent<Image>();
                image.sprite = !available ? locked : progress.IsLevelComplete(selected.id, i) ? completed : numbers[Mathf.Min(i, numbers.Length-1)];
                button.interactable = available; button.gameObject.SetActive(true);
                button.GetComponent<UIButtonAnimator>().SetChosen(selectedLevel == i);
            }
            details.text = UIWords.World(selected.id) + "\n" + progress.CompletedLevels(selected.id) + " / " + selected.plannedLevels + " COMPLETE\n" +
                (!unlocked ? "LOCKED - COMPLETE THE PREVIOUS WORLD" : selectedLevel < 0 ? "THIS WORLD IS NOT READY YET" : "LEVEL " + (selectedLevel+1));
            play.interactable = !launching && selectedLevel >= 0 && unlocked && flow.CanLaunch(selected, selectedLevel);
        }
        public void Play()
        {
            if (launching || selected == null || selectedLevel < 0 || !flow.CanLaunch(selected, selectedLevel)) return;
            launching = true;
            if (!flow.TryLaunch(selected, selectedLevel)) { launching = false; Refresh(); }
        }
    }
}
