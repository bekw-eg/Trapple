using TMPro;
using Trapple.Core;
using Trapple.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Trapple.UI
{
    [DisallowMultipleComponent]
    public sealed class DeathScreenUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text score;
        [SerializeField] private Button respawn, hub;
        private string levelScene;
        private bool loading;
        public bool IsOpen { get; private set; }
        public Button RespawnButton => respawn;
        public Button HubButton => hub;

        public void Configure(TMP_Text scoreLabel, Button respawnButton, Button hubButton)
        {
            score = scoreLabel;
            respawn = respawnButton;
            hub = hubButton;
        }

        private void Awake()
        {
            respawn.onClick.AddListener(Respawn);
            hub.onClick.AddListener(ReturnToHub);
        }

        // No score service exists yet. A future service can supply its value here.
        public void Show(string scenePath, int currentScore = 0)
        {
            if (IsOpen) return;
            IsOpen = true;
            loading = false;
            levelScene = scenePath;
            score.text = "Score: " + currentScore;
            respawn.interactable = hub.interactable = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(respawn.gameObject);
        }

        public void Hide()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }

        private bool BeginTransition()
        {
            if (!IsOpen || loading || GameManager.Instance == null) return false;
            loading = true;
            respawn.interactable = hub.interactable = false;
            Hide();
            return true;
        }

        public void Respawn()
        {
            if (string.IsNullOrEmpty(levelScene) || !BeginTransition()) return;
            GameManager.Instance.LoadScene(levelScene);
        }

        public void ReturnToHub()
        {
            if (!BeginTransition()) return;
            if (HubSceneFlowController.Instance != null) HubSceneFlowController.Instance.ReturnToHub();
            else GameManager.Instance.LoadScene(HubSceneFlowController.HubScenePath);
        }

        private void OnDestroy()
        {
            respawn.onClick.RemoveListener(Respawn);
            hub.onClick.RemoveListener(ReturnToHub);
        }
    }
}
