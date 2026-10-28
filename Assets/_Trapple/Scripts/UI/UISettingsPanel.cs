using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.UI
{
    public sealed class UISettingsPanel : MonoBehaviour
    {
        public const string VolumeKey = "Trapple.MasterVolume";
        [SerializeField] private Slider master;
        [SerializeField] private TMP_Text value;
        public Slider Master => master;
        public void Configure(Slider slider, TMP_Text label) { master = slider; value = label; }
        private void OnEnable()
        {
            master.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1)));
            Refresh(master.value);
            master.onValueChanged.AddListener(Change);
        }
        private void Change(float amount)
        {
            amount = Mathf.Clamp01(amount); AudioListener.volume = amount;
            PlayerPrefs.SetFloat(VolumeKey, amount); Refresh(amount);
        }
        private void Refresh(float amount) => value.text = Mathf.RoundToInt(amount * 100) + "%";
        private void OnDisable() { master.onValueChanged.RemoveListener(Change); PlayerPrefs.Save(); }
        private void OnApplicationPause(bool paused) { if (paused) PlayerPrefs.Save(); }
        private void OnApplicationQuit() => PlayerPrefs.Save();
    }
}
