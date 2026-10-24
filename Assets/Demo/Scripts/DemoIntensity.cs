using UnityEngine;

namespace Nekuzaky.Resonance.Demo
{
    [AddComponentMenu("Resonance Demo/Demo Intensity")]
    public sealed class DemoIntensity : MonoBehaviour
    {
        public SoundParameter parameter;
        public Transform target;
        public float range = 12f;

        private void Update()
        {
            if (parameter == null || target == null)
                return;
            var distance = Vector3.Distance(transform.position, target.position);
            Resonance.SetParameter(parameter, 1f - Mathf.Clamp01(distance / range));
        }
    }
}
