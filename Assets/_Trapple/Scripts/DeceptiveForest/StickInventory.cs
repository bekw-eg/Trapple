using System;
using UnityEngine;

namespace Trapple.DeceptiveForest
{
    /// <summary>Scene-owned ammunition. Arena checkpoints restore a snapshot, never add a second grant.</summary>
    [DisallowMultipleComponent]
    public sealed class StickInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumCapacity = 10;
        public int Count { get; private set; }
        public int MaximumCapacity => maximumCapacity;
        public event Action<int> Changed;

        public bool TryAdd(int amount = 1)
        {
            if (amount <= 0 || Count >= maximumCapacity) return false;
            Count += Mathf.Min(maximumCapacity - Count, amount);
            Changed?.Invoke(Count);
            return true;
        }

        public bool TrySpend()
        {
            if (Count <= 0) return false;
            Count--;
            Changed?.Invoke(Count);
            return true;
        }

        public void Restore(int count)
        {
            Count = Mathf.Clamp(count, 0, maximumCapacity);
            Changed?.Invoke(Count);
        }

        private void OnValidate() => maximumCapacity = Mathf.Max(1, maximumCapacity);
    }
}
