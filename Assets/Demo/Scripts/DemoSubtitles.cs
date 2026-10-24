using UnityEngine;

namespace Nekuzaky.Resonance.Demo
{
    [AddComponentMenu("Resonance Demo/Demo Subtitles")]
    public sealed class DemoSubtitles : MonoBehaviour
    {
        private string _text = string.Empty;
        private GUIStyle _style;

        public void Show(string text) => _text = text;

        public void Hide() => _text = string.Empty;

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_text))
                return;
            _style ??= new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            var width = Mathf.Min(Screen.width - 40f, 900f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height - 110f, width, 60f), _text, _style);
        }
    }
}
