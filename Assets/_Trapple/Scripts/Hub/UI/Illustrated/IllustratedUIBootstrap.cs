using UnityEngine;

namespace Trapple.Hub.Illustrated
{
    [DefaultExecutionOrder(30)]
    public sealed class IllustratedUIBootstrap : MonoBehaviour
    {
        [SerializeField] private HubUIController hubUI;
        public void Configure(HubUIController ui) => hubUI = ui;
        private void Start()
        {
            if (hubUI == null) { Debug.LogError("Build the UI using Tools > Trapple > Build UI.",this); return; }
            // HubBootstrap initializes the existing navigation and player lock at execution order 20.
            hubUI.Open(HubScreen.Worlds);
        }
    }
}
