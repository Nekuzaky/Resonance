using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/State Zone")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class StateZone : MonoBehaviour
    {
        #region Public

        public string triggerTag = "Player";
        public StateGroup group;
        public string enterState = string.Empty;
        public bool restoreOnExit = true;
        public MixSnapshot snapshot;

        public bool Occupied => _inside > 0;

        #endregion


        #region Private and Protected

        private string _previous;
        private int _inside;

        private bool Matches(GameObject other) => string.IsNullOrEmpty(triggerTag) || other.CompareTag(triggerTag);

        private void Enter(GameObject other)
        {
            if (!Matches(other) || _inside++ > 0)
                return;
            if (group != null)
            {
                _previous = Resonance.GetState(group);
                Resonance.SetState(group, enterState);
            }
            if (snapshot != null)
                Resonance.ApplySnapshot(snapshot);
        }

        private void Exit(GameObject other)
        {
            if (!Matches(other) || _inside == 0 || --_inside > 0)
                return;
            if (group != null && restoreOnExit && _previous != null)
                Resonance.SetState(group, _previous);
            if (snapshot != null)
                Resonance.RemoveSnapshot(snapshot);
        }

        #endregion


        #region Unity Callbacks

        private void OnTriggerEnter(Collider other) => Enter(other.gameObject);

        private void OnTriggerExit(Collider other) => Exit(other.gameObject);

        private void OnTriggerEnter2D(Collider2D other) => Enter(other.gameObject);

        private void OnTriggerExit2D(Collider2D other) => Exit(other.gameObject);

        private void OnDisable()
        {
            if (_inside > 0 && snapshot != null)
                Resonance.RemoveSnapshot(snapshot);
            _inside = 0;
        }

        #endregion
    }
}
