using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [ExecuteAlways]
    [AddComponentMenu("Resonance/Reverb Zone")]
    [RequireComponent(typeof(Collider))]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/space.md")]
    public sealed class ReverbZone : MonoBehaviour
    {
        #region Public

        public AudioReverbPreset preset = AudioReverbPreset.Room;
        [Range(0f, 1f)] public float intensity = 1f;
        [Min(0f)] public float fadeDistance = 3f;

        public static IReadOnlyList<ReverbZone> Active => Zones;

        public float WeightAt(Vector3 position)
        {
            if (_collider == null || !_collider.enabled)
                return 0f;
            var closest = _collider.ClosestPoint(position);
            var distance = Vector3.Distance(closest, position);
            if (distance <= 0.001f)
                return intensity;
            return fadeDistance <= 0f ? 0f : Mathf.Clamp01(1f - distance / fadeDistance) * intensity;
        }

        #endregion


        #region Private and Protected

        private static readonly List<ReverbZone> Zones = new();
        private Collider _collider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Zones.Clear();

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _collider = GetComponent<Collider>();
            if (!Zones.Contains(this))
                Zones.Add(this);
        }

        private void OnDisable() => Zones.Remove(this);

        private void OnDrawGizmosSelected()
        {
            var bounds = GetComponent<Collider>() != null ? GetComponent<Collider>().bounds : new Bounds(transform.position, Vector3.one);
            Gizmos.color = new Color(0.36f, 0.6f, 0.84f, 0.8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = new Color(0.36f, 0.6f, 0.84f, 0.25f);
            Gizmos.DrawWireCube(bounds.center, bounds.size + Vector3.one * fadeDistance * 2f);
        }

        #endregion
    }
}
