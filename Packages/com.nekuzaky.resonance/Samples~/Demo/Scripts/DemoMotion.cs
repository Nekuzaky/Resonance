using UnityEngine;

namespace Nekuzaky.Resonance.Demo
{
    [AddComponentMenu("Resonance Demo/Demo Motion")]
    public sealed class DemoMotion : MonoBehaviour
    {
        public Vector3 axis = Vector3.right;
        public float distance = 12f;
        public float period = 10f;

        private Vector3 _origin;

        private void Awake() => _origin = transform.position;

        private void Update()
        {
            var phase = Mathf.Sin(Time.time * Mathf.PI * 2f / period);
            transform.position = _origin + axis.normalized * phase * distance;
        }
    }
}
