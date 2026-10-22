using Trapple.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Trapple.Hub.Illustrated
{
    public sealed class IllustratedUIBootstrap : MonoBehaviour
    {
        [SerializeField] private HubConfiguration configuration;
        [SerializeField] private TrappleUIManager ui;
        [SerializeField] private PanelSettings returnPanel;
        [SerializeField] private VisualTreeAsset returnTree;
        public void Configure(HubConfiguration config, TrappleUIManager ui, PanelSettings panel, VisualTreeAsset tree)
        { configuration = config; this.ui = ui; returnPanel = panel; returnTree = tree; }
        private void Start()
        {
            if (configuration == null || ui == null) { Debug.LogError("Build the UI using Tools > Trapple > Build UI."); return; }
            var flow = HubSceneFlowController.Ensure(configuration, returnPanel, returnTree);
            ui.Initialize(configuration, flow.Progression, flow, ui.Show, Close, Close);
            ui.Show(HubScreen.Worlds);
        }
        private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) Close(); }
        private void Close()
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
            else ui.Hide();
        }
    }
}
