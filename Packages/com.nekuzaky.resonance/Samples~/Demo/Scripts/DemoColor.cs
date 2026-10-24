using UnityEngine;
using UnityEngine.Rendering;

namespace Nekuzaky.Resonance.Demo
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    [AddComponentMenu("Resonance Demo/Demo Color")]
    public sealed class DemoColor : MonoBehaviour
    {
        public Color color = Color.white;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int MainColor = Shader.PropertyToID("_Color");

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        private void Apply()
        {
            var target = GetComponent<Renderer>();
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultMaterial != null && target.sharedMaterial != pipeline.defaultMaterial)
                target.sharedMaterial = pipeline.defaultMaterial;

            var block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            block.SetColor(MainColor, color);
            target.SetPropertyBlock(block);
        }
    }
}
