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
        [SerializeField] private Image portrait;
        [SerializeField] private Sprite normalFrame, lockedFrame, selectedFrame;
        public string Id => cosmeticId;
        public void Configure(string id, Button select, TMP_Text title, TMP_Text description, TMP_Text status, Image highlight)
        { cosmeticId = id; this.select = select; this.title = title; this.description = description; this.status = status; this.highlight = highlight; }
        public void ConfigureAppearance(Image portrait,Sprite normal,Sprite locked,Sprite selected)
        { this.portrait=portrait; normalFrame=normal; lockedFrame=locked; selectedFrame=selected; }
        public void Bind(WardrobePanelUI owner)
        { select.onClick.RemoveAllListeners(); select.onClick.AddListener(() => owner.Select(cosmeticId)); }
        public void Refresh(CosmeticDefinition cosmetic, IProgressionService progression)
        {
            if (cosmetic == null) { status.text = "NO DATA"; select.interactable=false; return; }
            title.text = Trapple.UI.UIWords.Skin(cosmetic.id); description.text = "";
            status.text = progression.EquippedCosmetic == cosmeticId ? "EQUIPPED" : progression.IsCosmeticUnlocked(cosmetic) ? "AVAILABLE" : "LOCKED";
            bool unlocked=progression.IsCosmeticUnlocked(cosmetic);
            if(portrait!=null) portrait.enabled=unlocked;
            if(normalFrame!=null) select.GetComponent<Image>().sprite=unlocked?normalFrame:lockedFrame;
            if(selectedFrame!=null) highlight.sprite=unlocked?selectedFrame:lockedFrame;
            highlight.color = Color.white;
            highlight.gameObject.SetActive(progression.SelectedCosmetic == cosmeticId);
            GetComponent<Trapple.UI.UIButtonAnimator>()?.SetChosen(progression.SelectedCosmetic == cosmeticId);
        }
    }
}
