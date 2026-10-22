using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Hub.Illustrated
{
    public sealed class WardrobePanelUI : MonoBehaviour
    {
        [SerializeField] private SkinItemUI[] rows;
        [SerializeField] private Image preview;
        [SerializeField] private TMP_Text title, description, count, equipText;
        [SerializeField] private Button equip;
        private HubConfiguration configuration;
        private IProgressionService progression;
        private Action<string> notify;
        public void Configure(SkinItemUI[] rows, Image preview, TMP_Text title, TMP_Text description, TMP_Text count, Button equip, TMP_Text equipText)
        { this.rows = rows; this.preview = preview; this.title = title; this.description = description; this.count = count; this.equip = equip; this.equipText = equipText; }
        public void Bind(HubConfiguration config, IProgressionService progression, Action<string> notify)
        {
            configuration = config; this.progression = progression; this.notify = notify;
            foreach (var row in rows) row.Bind(this);
            equip.onClick.RemoveListener(Equip); equip.onClick.AddListener(Equip); Refresh();
        }
        public void Select(string id)
        {
            if (progression == null || !progression.SelectCosmetic(id)) return;
            if (!progression.IsCosmeticUnlocked(configuration.Cosmetic(id))) notify?.Invoke("Скин закрыт");
        }
        public void Equip()
        {
            var cosmetic = configuration?.Cosmetic(progression?.SelectedCosmetic);
            if (cosmetic == null) { notify?.Invoke("Скин не настроен"); return; }
            if (!progression.TryEquip(cosmetic)) notify?.Invoke("Скин закрыт");
            else notify?.Invoke(cosmetic.title + " · надето");
        }
        public void Refresh()
        {
            if (configuration == null || progression == null) return;
            var cosmetic = configuration.Cosmetic(progression.SelectedCosmetic) ?? configuration.Cosmetic(progression.EquippedCosmetic);
            if (cosmetic == null) return;
            preview.sprite = cosmetic.previewSprite != null ? cosmetic.previewSprite : cosmetic.sprite != null ? cosmetic.sprite : configuration.heroSprite;
            preview.enabled = preview.sprite != null; preview.color = cosmetic.tint;
            title.text = cosmetic.title;
            description.text = preview.sprite == null ? "Превью пока отсутствует" : cosmetic.description;
            equipText.text = progression.EquippedCosmetic == cosmetic.id ? "НАДЕТО" : progression.IsCosmeticUnlocked(cosmetic) ? "НАДЕТЬ" : "ЗАКРЫТО";
            int unlocked = 0;
            foreach (var value in configuration.cosmetics) if (progression.IsCosmeticUnlocked(value)) unlocked++;
            count.text = unlocked + " / " + configuration.cosmetics.Length;
            foreach (var row in rows) row.Refresh(configuration.Cosmetic(row.Id), progression);
        }
    }
}
