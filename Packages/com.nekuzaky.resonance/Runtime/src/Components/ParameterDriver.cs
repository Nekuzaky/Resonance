using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Parameter Driver")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class ParameterDriver : MonoBehaviour
    {
        #region Public

        public SoundParameter parameter;
        public DriverSource source = DriverSource.Speed;
        public Animator animator;
        public string animatorParameter = string.Empty;
        public float constant;
        public float multiplier = 1f;
        public float offset;

        public float Value { get; private set; }

        #endregion


        #region Private and Protected

        private Vector3 _lastPosition;
        private int _animatorHash;

        private float Measure(float deltaTime)
        {
            var position = transform.position;
            var velocity = deltaTime > 0f ? (position - _lastPosition) / deltaTime : Vector3.zero;
            _lastPosition = position;
            switch (source)
            {
                case DriverSource.Speed: return velocity.magnitude;
                case DriverSource.VerticalSpeed: return velocity.y;
                case DriverSource.DistanceToListener: return Vector3.Distance(position, ListenerPosition());
                case DriverSource.Height: return position.y;
                case DriverSource.AnimatorFloat: return animator != null ? animator.GetFloat(_animatorHash) : 0f;
                default: return constant;
            }
        }

        private static Vector3 ListenerPosition()
        {
            var engine = Resonance.Engine;
            if (engine != null && engine.Listener != null)
                return engine.Listener.position;
            if (SoundListener.Current != null)
                return SoundListener.Current.transform.position;
            var camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _animatorHash = Animator.StringToHash(animatorParameter);
        }

        private void Update()
        {
            if (parameter == null)
                return;
            Value = Measure(Time.deltaTime) * multiplier + offset;
            Resonance.SetParameter(parameter, Value, gameObject);
        }

        #endregion
    }
}
