using System;
using System.IO;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class AudioRecorder : MonoBehaviour
    {
        #region Private and Protected

        private const string Flag = "-resonanceRecord";
        private const int MaxSeconds = 40;

        private string _path;
        private ListenerTap _tap;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, Flag);
            if (index < 0 || index + 1 >= args.Length)
                return;
            var listener = FindAnyObjectByType<AudioListener>();
            if (listener == null)
            {
                Debug.Log("[Record] no Audio Listener in the scene");
                return;
            }
            var recorder = listener.gameObject.AddComponent<AudioRecorder>();
            recorder._path = args[index + 1];
            recorder._tap = listener.gameObject.AddComponent<ListenerTap>();
            recorder._tap.Buffer = new float[AudioSettings.outputSampleRate * 8 * MaxSeconds];
            Debug.Log($"[Record] tapping the listener at {AudioSettings.outputSampleRate} Hz");
        }

        private void Write()
        {
            var rate = AudioSettings.outputSampleRate;
            var channels = Mathf.Max(1, _tap.Channels);
            var count = _tap.Count / channels * channels;
            using var stream = new BinaryWriter(File.Create(_path));
            stream.Write("RIFF".ToCharArray());
            stream.Write(36 + count * 4);
            stream.Write("WAVEfmt ".ToCharArray());
            stream.Write(16);
            stream.Write((short)3);
            stream.Write((short)channels);
            stream.Write(rate);
            stream.Write(rate * channels * 4);
            stream.Write((short)(channels * 4));
            stream.Write((short)32);
            stream.Write("data".ToCharArray());
            stream.Write(count * 4);
            for (var i = 0; i < count; i++)
                stream.Write(_tap.Buffer[i]);
            Debug.Log($"[Record] wrote {count / channels / (float)rate:0.0} s of {channels} channels to {_path}");
        }

        #endregion


        #region Unity Callbacks

        private void OnApplicationQuit()
        {
            if (_tap != null)
                Write();
        }

        #endregion
    }
}
