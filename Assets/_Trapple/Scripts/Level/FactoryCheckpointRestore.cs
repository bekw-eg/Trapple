using UnityEngine;

namespace Trapple.Level
{
    [DefaultExecutionOrder(20000)]
    [DisallowMultipleComponent]
    public sealed class FactoryCheckpointRestore : MonoBehaviour
    {
        private void Awake() => FactoryCheckpoint.RestoreCurrentScene();
    }
}
