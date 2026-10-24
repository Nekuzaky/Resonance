using UnityEngine;

namespace Nekuzaky.Resonance.Demo
{
    [AddComponentMenu("Resonance Demo/Demo Walker")]
    public sealed class DemoWalker : MonoBehaviour
    {
        public SoundEvent footsteps;
        public SwitchGroup surface;
        public float radius = 8f;
        public float speed = 2.4f;
        public float stride = 1.1f;

        private float _angle;
        private float _travelled;

        private void Update()
        {
            var previous = transform.position;
            _angle += speed / radius * Time.deltaTime;
            var position = new Vector3(Mathf.Cos(_angle) * radius, transform.position.y, Mathf.Sin(_angle) * radius);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - previous + Vector3.forward * 0.0001f));

            _travelled += Vector3.Distance(previous, position);
            if (_travelled < stride)
                return;
            _travelled -= stride;
            if (surface != null)
                Resonance.SetSwitch(surface, position.x < 0f ? "Grass" : "Stone", gameObject);
            Resonance.Play(footsteps, gameObject);
        }
    }
}
