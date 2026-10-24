using System;
using System.Collections.Generic;
using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class InspectorTests : Fixture
    {
        private static readonly Type[] Components =
        {
            typeof(SoundEmitter), typeof(SoundListener), typeof(ParameterDriver), typeof(SwitchSetter), typeof(StateZone),
            typeof(ReverbZone), typeof(MusicSource), typeof(SoundBankLoader), typeof(SubtitleRelay), typeof(AnimationSounds),
            typeof(SnapshotTrigger)
        };

        [Test]
        public void EveryComponent_HasAResonanceInspectorThatBuilds()
        {
            var host = Track(new GameObject("Inspector Host", typeof(BoxCollider)));
            foreach (var type in Components)
            {
                var component = host.GetComponent(type) ?? host.AddComponent(type);
                var editor = UnityEditor.Editor.CreateEditor(component);
                try
                {
                    Assert.AreEqual("Nekuzaky.Resonance.Editor", editor.GetType().Namespace, type.Name);
                    var gui = editor.CreateInspectorGUI();
                    Assert.IsNotNull(gui, type.Name);
                    Assert.Greater(gui.childCount, 0, type.Name);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }
            }
        }

        [Test]
        public void ChoiceField_OffersTheGroupValuesAndWritesThePick()
        {
            var host = Track(new GameObject("Setter"));
            var setter = host.AddComponent<SwitchSetter>();
            setter.group = Asset<SwitchGroup>("Surface");
            setter.group.values = new List<string> { "Grass", "Stone" };
            var serialized = new SerializedObject(setter);
            var field = new ChoiceField(serialized.FindProperty("value"), "Value", () => setter.group.values, "(choose)", null);

            CollectionAssert.AreEqual(new[] { "(choose)", "Grass", "Stone" }, field.choices);
            field.Select("Stone");

            Assert.AreEqual("Stone", setter.value);
        }

        [Test]
        public void ChoiceField_FlagsValuesThatNoLongerExist()
        {
            var host = Track(new GameObject("Zone", typeof(BoxCollider)));
            var zone = host.AddComponent<StateZone>();
            zone.group = Asset<StateGroup>("Game");
            zone.group.states = new List<string> { "Explore" };
            zone.enterState = "Combat";
            var field = new ChoiceField(new SerializedObject(zone).FindProperty("enterState"), "State", () => zone.group.states, "(choose)", null);

            Assert.AreEqual("Combat" + ChoiceField.MissingSuffix, field.value);
            Assert.IsTrue(field.ClassListContains("rs-choice--missing"));
        }

        [Test]
        public void EmptyValue_ShowsTheEmptyChoice()
        {
            var host = Track(new GameObject("Emitter"));
            var emitter = host.AddComponent<SoundEmitter>();
            emitter.triggerTag = string.Empty;
            var field = InspectorKit.Tag(new SerializedObject(emitter), "triggerTag", "Tag", null);

            Assert.AreEqual("(any object)", field.value);
            Assert.Contains("Untagged", field.choices);
        }
    }
}
