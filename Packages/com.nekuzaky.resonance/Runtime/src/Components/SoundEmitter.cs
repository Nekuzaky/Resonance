using System;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [Flags]
    public enum SoundTrigger
    {
        None = 0,
        Start = 1 << 0,
        Enable = 1 << 1,
        Disable = 1 << 2,
        Destroy = 1 << 3,
        TriggerEnter = 1 << 4,
        TriggerExit = 1 << 5,
        CollisionEnter = 1 << 6,
        CollisionExit = 1 << 7,
        MouseDown = 1 << 8
    }

    [AddComponentMenu("Resonance/Sound Emitter")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class SoundEmitter : MonoBehaviour
    {
        #region Public

        public SoundEvent sound;
        public SoundTrigger playOn = SoundTrigger.Start;
        public SoundTrigger stopOn = SoundTrigger.Disable;
        public float stopFade = -1f;
        public string triggerTag = string.Empty;
        public SoundParameter impactParameter;
        [Min(0f)] public float impactScale = 1f;

        public SoundHandle Handle => _handle;

        public bool IsPlaying => _handle.IsPlaying;

        public void Play()
        {
            if (sound == null)
                return;
            _handle = Resonance.Play(sound, gameObject);
        }

        public void Stop() => _handle.Stop(stopFade);

        public void PlayEvent(SoundEvent other)
        {
            if (other != null)
                Resonance.Play(other, gameObject);
        }

        #endregion


        #region Private and Protected

        private SoundHandle _handle;
        private static bool _quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => _quitting = true;

        private bool Matches(GameObject other) => string.IsNullOrEmpty(triggerTag) || (other != null && other.CompareTag(triggerTag));

        private bool Alive => !_quitting && gameObject.scene.isLoaded;

        private void Fire(SoundTrigger trigger, GameObject other)
        {
            if (!Matches(other))
                return;
            if ((stopOn & trigger) != 0)
                Stop();
            if ((playOn & trigger) != 0)
                Play();
        }

        private void Impact(float speed)
        {
            if (impactParameter != null)
                Resonance.SetParameter(impactParameter, speed * impactScale, gameObject, true);
        }

        #endregion


        #region Unity Callbacks

        private void Start() => Fire(SoundTrigger.Start, null);

        private void OnEnable()
        {
            if (didStart)
                Fire(SoundTrigger.Enable, null);
        }

        private void OnDisable()
        {
            if (Alive)
                Fire(SoundTrigger.Disable, null);
        }

        private void OnDestroy()
        {
            if (!Alive || sound == null)
                return;
            if ((stopOn & SoundTrigger.Destroy) != 0)
                Stop();
            if ((playOn & SoundTrigger.Destroy) != 0)
                Resonance.PlayAt(sound, transform.position);
        }

        private void OnTriggerEnter(Collider other) => Fire(SoundTrigger.TriggerEnter, other.gameObject);

        private void OnTriggerExit(Collider other) => Fire(SoundTrigger.TriggerExit, other.gameObject);

        private void OnTriggerEnter2D(Collider2D other) => Fire(SoundTrigger.TriggerEnter, other.gameObject);

        private void OnTriggerExit2D(Collider2D other) => Fire(SoundTrigger.TriggerExit, other.gameObject);

        private void OnCollisionEnter(Collision collision)
        {
            Impact(collision.relativeVelocity.magnitude);
            Fire(SoundTrigger.CollisionEnter, collision.gameObject);
        }

        private void OnCollisionExit(Collision collision) => Fire(SoundTrigger.CollisionExit, collision.gameObject);

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Impact(collision.relativeVelocity.magnitude);
            Fire(SoundTrigger.CollisionEnter, collision.gameObject);
        }

        private void OnCollisionExit2D(Collision2D collision) => Fire(SoundTrigger.CollisionExit, collision.gameObject);

        private void OnMouseDown() => Fire(SoundTrigger.MouseDown, null);

        #endregion
    }
}
