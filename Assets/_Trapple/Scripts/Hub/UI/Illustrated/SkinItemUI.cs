using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Hub.Illustrated
{
    public sealed class SkinItemUI : MonoBehaviour
    {
        [SerializeField] private string cosmeticId;
        [SerializeField] private Button select;
        [SerializeField] private TMP_Text title, description, status;
        [SerializeField] private Image highlight;
        public string Id => cosmeticId;
        public void Configure(string id, Button select, TMP_Text title, TMP_Text description, TMP_Text status, Image highlight)
        { cosmeticId = id; this.select = select; this.title = title; this.description = description; this.status = status; this.highlight = highlight; }
        public void Bind(WardrobePanelUI owner)
        { select.onClick.RemoveAllListeners(); select.onClick.AddListener(() => owner.Select(cosmeticId)); }
        public void Refresh(CosmeticDefinition cosmetic, IProgressionService progression)
        {
            if (cosmetic == null) { status.text = "Нет данных"; return; }
            title.text = cosmetic.title; description.text = cosmetic.description;
            status.text = progression.EquippedCosmetic == cosmeticId ? "НАДЕТО" : progression.IsCosmeticUnlocked(cosmetic) ? "НАДЕТЬ" : "ЗАКРЫТО";
            highlight.color = new Color(.03f,.5f,.26f,progression.SelectedCosmetic == cosmeticId ? .13f : 0);
        }
    }
}
