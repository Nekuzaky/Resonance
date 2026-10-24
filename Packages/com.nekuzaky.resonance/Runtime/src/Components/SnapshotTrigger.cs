using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Snapshot Trigger")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class SnapshotTrigger : MonoBehaviour
    {
        public MixSnapshot snapshot;
        public bool applyOnEnable = true;

        public void Apply() => Resonance.ApplySnapshot(snapshot);

        public void Remove() => Resonance.RemoveSnapshot(snapshot);

        private void OnEnable()
        {
            if (applyOnEnable)
                Apply();
        }

        private void OnDisable()
        {
            if (applyOnEnable && gameObject.scene.isLoaded)
                Remove();
        }
    }
}
