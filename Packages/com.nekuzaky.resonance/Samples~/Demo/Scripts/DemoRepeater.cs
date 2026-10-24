using UnityEngine;

namespace Nekuzaky.Resonance.Demo
{
    [AddComponentMenu("Resonance Demo/Demo Repeater")]
    public sealed class DemoRepeater : MonoBehaviour
    {
        public SoundEvent sound;
        public float firstDelay = 3f;
        public float interval = 12f;

        private float _next;

        private void Start() => _next = Time.time + firstDelay;

        private void Update()
        {
            if (Time.time < _next)
                return;
            _next = Time.time + interval;
            Resonance.Play(sound, gameObject);
        }
    }
}
