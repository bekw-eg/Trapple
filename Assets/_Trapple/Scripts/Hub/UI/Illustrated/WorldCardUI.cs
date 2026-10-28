using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Hub.Illustrated
{
    public sealed class WorldCardUI : MonoBehaviour
    {
        [SerializeField] private string worldId;
        [SerializeField] private Button select;
        [SerializeField] private TMP_Text progress, state, badge;
        [SerializeField] private Image lockShade;
        private WorldDefinition world;
        private IProgressionService progression;
        private ISceneFlowController flow;
        private Action<string> notify;
        private bool launching;
        private Color badgeColor;
        private bool badgeColorSaved;
        public void Configure(string id, Button select, TMP_Text progress, TMP_Text state, Image lockShade, TMP_Text badge)
        { worldId = id; this.select = select; this.progress = progress; this.state = state; this.lockShade = lockShade; this.badge = badge; }
        public void Bind(HubConfiguration config, IProgressionService progression, ISceneFlowController flow, Action<string> notify)
        {
            if (!badgeColorSaved && badge != null) { badgeColor = badge.color; badgeColorSaved = true; }
            world = config.World(worldId); this.progression = progression; this.flow = flow; this.notify = notify;
            select.onClick.RemoveListener(Select); select.onClick.AddListener(Select); Refresh();
        }
        public void Refresh()
        {
            if (world == null || progression == null) { if (state != null) state.text = "Нет данных"; return; }
            bool unlocked = progression.IsWorldUnlocked(world);
            bool complete = progression.IsWorldComplete(worldId);
            if (progress != null) progress.text = world.maximumStars > 0 ?
                progression.Stars(worldId) + " / " + world.maximumStars + " звёзд" :
                progression.CompletedLevels(worldId) + " / " + world.plannedLevels + " уровней";
            if (state != null) state.text = complete ? "ПРОЙДЕНО" : unlocked ? "ВЫБРАТЬ" : "ЗАКРЫТО";
            if (lockShade != null) lockShade.gameObject.SetActive(!unlocked);
            if (badge != null)
            {
                badge.text = complete ? "COMPLETE" : unlocked ? "OK" : "ЗАКР.";
                badge.color = complete ? new Color(.24f, .56f, .23f) : badgeColor;
            }
        }
        public void Select()
        {
            if (launching) return;
            if (world == null || progression == null) { notify?.Invoke("Мир не настроен"); return; }
            if (!progression.IsWorldUnlocked(world)) { notify?.Invoke("Мир закрыт. Завершите предыдущий мир."); return; }
            // Resume the next configured level, or replay the first after completion.
            int level = progression.CompletedLevels(worldId);
            if (level >= world.plannedLevels) level = 0;
            if (flow == null || !flow.CanLaunch(world, level)) { notify?.Invoke("Игровая сцена пока недоступна"); return; }
            launching = true;
            if (!flow.TryLaunch(world, level)) { launching = false; notify?.Invoke("Не удалось открыть сцену"); }
        }
    }
}
