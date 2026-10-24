using System.Reflection;
using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class EditorAssetTests
    {
        [Test]
        public void StyleSheet_Loads()
        {
            var sheet = Glyphs.StyleSheet;
            Assert.IsNotNull(sheet);
            foreach (var name in new[] { "importedWithErrors", "importedWithWarnings" })
            {
                var property = typeof(UnityEngine.UIElements.StyleSheet).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(property, name);
                Assert.IsFalse((bool)property.GetValue(sheet), name);
            }
        }

        [Test]
        public void EveryGlyph_HasAnIcon()
        {
            foreach (var field in typeof(Glyphs).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                    Assert.IsNotNull(Glyphs.Get((string)field.GetValue(null)), field.Name);
            }
        }

        [Test]
        public void EveryAssetKind_HasAColoredIcon()
        {
            foreach (var kind in ResonanceAssets.Kinds)
                Assert.IsNotNull(Glyphs.AssetIcon(kind.Icon), kind.Icon);
            Assert.IsNotNull(Glyphs.AssetIcon("Resonance"));
        }

        [Test]
        public void EveryComponent_HasAScriptIcon()
        {
            var components = new[]
            {
                typeof(SoundEmitter), typeof(SoundListener), typeof(ParameterDriver), typeof(SwitchSetter),
                typeof(StateZone), typeof(MusicSource), typeof(AnimationSounds), typeof(SnapshotTrigger),
                typeof(ReverbZone), typeof(SoundBankLoader), typeof(SubtitleRelay), typeof(VolumeSetting), typeof(MusicBeatEvents),
                typeof(SoundCallbacks), typeof(SoundIndicators), typeof(ResonanceDebugOverlay)
            };
            foreach (var type in components)
            {
                var script = FindScript(type);
                Assert.IsNotNull(script, type.Name);
                Assert.IsNotNull(EditorGUIUtility.GetIconForObject(script), type.Name);
            }
        }

        private static MonoScript FindScript(System.Type type)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}", new[] { "Packages/com.nekuzaky.resonance" }))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type)
                    return script;
            }
            return null;
        }
    }
}
