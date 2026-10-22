using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trapple.Hub.Illustrated
{
    public sealed class UpgradeCardUI : MonoBehaviour
    {
        [SerializeField] private UpgradeKind kind;
        [SerializeField] private Button purchase;
        [SerializeField] private TMP_Text level, bonus, price, buttonText;
        [SerializeField] private Image[] pips;
        private UpgradeDefinition definition;
        private IProgressionService progression;
        private Action<string> notify;
        private int purchasedFrame = -1;
        public void Configure(UpgradeKind kind, Button purchase, TMP_Text level, TMP_Text bonus,
            TMP_Text price, TMP_Text buttonText, Image[] pips)
        { this.kind = kind; this.purchase = purchase; this.level = level; this.bonus = bonus; this.price = price; this.buttonText = buttonText; this.pips = pips; }
        public void Bind(HubConfiguration config, IProgressionService progression, Action<string> notify)
        {
            definition = config.Upgrade(kind); this.progression = progression; this.notify = notify;
            purchase.onClick.RemoveListener(Purchase); purchase.onClick.AddListener(Purchase); Refresh();
        }
        public void Refresh()
        {
            if (definition == null || progression == null) { purchase.interactable = false; return; }
            int current = progression.Level(kind); bool max = current >= definition.MaxLevel;
            level.text = "УРОВЕНЬ " + current + " / 3";
            float value = current * definition.percentPerLevel;
            bonus.text = max ? "+" + value.ToString("0.#") + "% · МАКС." : "+" + value.ToString("0.#") + "% → +" + (value + definition.percentPerLevel).ToString("0.#") + "%";
            price.text = max ? "Все уровни открыты" : definition.Cost(current) > 0 ? definition.Cost(current) + " семечек" : "Цена не настроена";
            buttonText.text = max ? "МАКСИМУМ" : "УЛУЧШИТЬ ↑";
            purchase.interactable = !max;
            for (int i = 0; i < pips.Length; i++) pips[i].color = i < current ? new Color(.05f,.36f,.2f) : Color.clear;
        }
        public void Purchase()
        {
            if (purchasedFrame == Time.frameCount || definition == null || progression == null) return;
            int current = progression.Level(kind);
            if (current >= 3) { notify?.Invoke("Достигнут максимальный уровень"); return; }
            int cost = definition.Cost(current);
            if (cost <= 0) { notify?.Invoke("Цена улучшения ещё не настроена"); return; }
            if (progression.Seeds < cost) { notify?.Invoke("Недостаточно семечек"); return; }
            if (progression.TryUpgrade(definition)) { purchasedFrame = Time.frameCount; notify?.Invoke(definition.title + " · уровень " + progression.Level(kind)); }
        }
    }
}
