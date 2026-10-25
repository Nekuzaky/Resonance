using System;
using System.Collections.Generic;
using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class EffectEditorTests : Fixture
    {
        [Test]
        public void EffectsPanel_ShowsOneCardPerEffectWithEveryParameter()
        {
            var bus = BusWithEveryEffect();
            var panel = new EffectsPanel();

            panel.Show(bus);
            panel.Refresh(Engine);

            var cards = panel.Element.Query(className: "rs-effect").ToList();
            Assert.AreEqual(bus.effects.Count, cards.Count);
            var rows = 0;
            foreach (BusEffectType type in Enum.GetValues(typeof(BusEffectType)))
                rows += BusEffects.Parameters(type).Count;
            Assert.AreEqual(rows + 1, panel.Element.Query(className: "rs-effect-row").ToList().Count, "One row per parameter, plus the reverb routing");
        }

        [Test]
        public void EffectsPanel_RebuildsWhenTheChainChanges()
        {
            var bus = Asset<AudioBus>("SFX");
            bus.AddEffect(BusEffectType.LowPass);
            var panel = new EffectsPanel();
            panel.Show(bus);

            bus.AddEffect(BusEffectType.Delay);
            panel.Refresh(Engine);

            Assert.AreEqual(2, panel.Element.Query(className: "rs-effect").ToList().Count);
        }

        [Test]
        public void ParameterRow_ShowsTheLiveValueAfterParameters()
        {
            var bus = Asset<AudioBus>("SFX");
            var effect = bus.AddEffect(BusEffectType.LowPass);
            var row = new ParameterRow(bus, effect, BusEffects.FilterCutoff);

            row.Refresh(new[] { 800f, 1f });

            Assert.AreEqual("now 800", row.Element.Q<Label>(className: "rs-effect-live").text);
        }

        [Test]
        public void ParameterRow_WritesTheValueWithUndo()
        {
            var bus = Asset<AudioBus>("SFX");
            var effect = bus.AddEffect(BusEffectType.Delay);
            var row = new ParameterRow(bus, effect, BusEffects.DelayTime);

            row.Set(120.04f);
            Assert.AreEqual(120f, effect.Value(BusEffects.DelayTime));
            Undo.IncrementCurrentGroup();
            row.Set(99999f);
            Assert.AreEqual(5000f, effect.Value(BusEffects.DelayTime));

            Undo.PerformUndo();
            Assert.AreEqual(120f, effect.Value(BusEffects.DelayTime));
        }

        [Test]
        public void Drawers_BuildForBusEffectsAndSnapshotOffsets()
        {
            var bus = BusWithEveryEffect();
            bus.effects[0].modulations.Add(new EffectModulation { parameter = Asset<SoundParameter>("Depth") });
            var snapshot = Asset<MixSnapshot>("Underwater");
            snapshot.effects.Add(new EffectOffset { bus = bus, effect = bus.effects[2].id, parameter = BusEffects.ReverbDecay, value = 4f });
            var busProperty = new SerializedObject(bus).FindProperty("effects.Array.data[0]");
            var modulation = busProperty.FindPropertyRelative("modulations.Array.data[0]");
            var offset = new SerializedObject(snapshot).FindProperty("effects.Array.data[0]");

            var card = new BusEffectDrawer().CreatePropertyGUI(busProperty);
            var drive = new EffectModulationDrawer().CreatePropertyGUI(modulation);
            var target = new EffectOffsetDrawer().CreatePropertyGUI(offset);

            Assert.IsTrue(card.ClassListContains("rs-effect"));
            Assert.AreEqual("Cutoff (Hz)", drive.Q<DropdownField>().value);
            var choices = target.Query<DropdownField>().ToList();
            Assert.AreEqual("3. Reverb", choices[0].value);
            Assert.AreEqual("Decay (s)", choices[1].value);
        }

        [Test]
        public void BusInspector_HidesTheRawListAndShowsTheEffectsPanel()
        {
            var bus = BusWithEveryEffect();
            var editor = (AudioBusInspector)UnityEditor.Editor.CreateEditor(bus);
            try
            {
                var gui = editor.CreateInspectorGUI();

                foreach (var field in gui.Query<PropertyField>().ToList())
                    Assert.AreNotEqual("effects", field.bindingPath);
                Assert.IsNotNull(gui.Q<PropertyField>());
                Assert.AreEqual(bus.effects.Count, gui.Query(className: "rs-effect").ToList().Count);
                Assert.AreSame(bus, editor.Effects.Bus);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void EffectCard_MoveShowsTheNewOrder()
        {
            var bus = Chain(BusEffectType.LowPass, BusEffectType.Delay, BusEffectType.Reverb);
            var editor = (AudioBusInspector)UnityEditor.Editor.CreateEditor(bus);
            try
            {
                var gui = editor.CreateInspectorGUI();

                Card(editor.Effects, 1).Move(-1);

                Assert.AreEqual(BusEffectType.Delay, bus.effects[0].type);
                CollectionAssert.AreEqual(new[] { "Delay", "Low Pass", "Reverb" }, Names(editor.Effects));
                Assert.AreEqual(3, gui.Query(className: "rs-effect").ToList().Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
            }
        }

        [Test]
        public void EffectCard_RemovesItsOwnEffectAfterTheChainMoved()
        {
            var bus = Chain(BusEffectType.LowPass, BusEffectType.Delay, BusEffectType.Reverb);
            var reverb = bus.effects[2];
            var card = new EffectCard(bus, 2, () => { });
            (bus.effects[0], bus.effects[2]) = (bus.effects[2], bus.effects[0]);

            card.Remove();

            Assert.AreEqual(2, bus.effects.Count);
            Assert.IsFalse(bus.effects.Contains(reverb));
            Assert.AreEqual(BusEffectType.Delay, bus.effects[0].type);
            Assert.AreEqual(BusEffectType.LowPass, bus.effects[1].type);
        }

        [Test]
        public void EffectCard_RemoveCanBeUndone()
        {
            var bus = Chain(BusEffectType.LowPass, BusEffectType.Delay);
            var panel = new EffectsPanel();
            panel.Show(bus);
            Undo.IncrementCurrentGroup();

            Card(panel, 0).Remove();
            Assert.AreEqual(1, bus.effects.Count);
            Undo.PerformUndo();
            panel.Refresh(Engine);

            Assert.AreEqual(2, bus.effects.Count);
            CollectionAssert.AreEqual(new[] { "Low Pass", "Delay" }, Names(panel));
        }

        [Test]
        public void EffectCard_DisablesUpOnTheFirstAndDownOnTheLast()
        {
            var bus = Chain(BusEffectType.LowPass, BusEffectType.Delay, BusEffectType.Reverb);
            var panel = new EffectsPanel();
            panel.Show(bus);
            var cards = panel.Element.Query(className: "rs-effect").ToList();

            Assert.IsFalse(cards[0].Q(className: "rs-effect-up").enabledSelf);
            Assert.IsTrue(cards[0].Q(className: "rs-effect-down").enabledSelf);
            Assert.IsTrue(cards[1].Q(className: "rs-effect-up").enabledSelf);
            Assert.IsTrue(cards[1].Q(className: "rs-effect-down").enabledSelf);
            Assert.IsTrue(cards[2].Q(className: "rs-effect-up").enabledSelf);
            Assert.IsFalse(cards[2].Q(className: "rs-effect-down").enabledSelf);
        }

        [Test]
        public void ReverbCard_LaysOutRoutingLikeTheSliderRows()
        {
            var bus = Chain(BusEffectType.Reverb);
            var card = new EffectCard(bus, 0, () => { });

            var routing = card.Element.Q(className: "rs-effect-routing");

            Assert.IsTrue(routing.ClassListContains("rs-effect-row"));
            Assert.AreEqual("Routing", routing.Q<Label>(className: "rs-effect-label").text);
            Assert.IsNotNull(routing.Q<EnumField>(className: "rs-effect-slider"));
        }

        [Test]
        public void EffectsPanel_DoesNotRebuildForAnEmptyEntry()
        {
            var bus = Chain(BusEffectType.LowPass, BusEffectType.Delay);
            bus.effects.Insert(1, null);
            var panel = new EffectsPanel();
            panel.Show(bus);
            panel.Refresh(Engine);
            var first = panel.Element.Q(className: "rs-effect");

            panel.Refresh(Engine);
            panel.Refresh(Engine);

            Assert.AreSame(first, panel.Element.Q(className: "rs-effect"));
        }

        private AudioBus Chain(params BusEffectType[] types)
        {
            var bus = Asset<AudioBus>("Chain");
            foreach (var type in types)
                bus.AddEffect(type);
            return bus;
        }

        private static EffectCard Card(EffectsPanel panel, int index) => panel.Cards[index];

        private static List<string> Names(EffectsPanel panel)
        {
            var names = new List<string>();
            foreach (var label in panel.Element.Query<Label>(className: "rs-effect-name").ToList())
                names.Add(label.text);
            return names;
        }

        private AudioBus BusWithEveryEffect()
        {
            var bus = Asset<AudioBus>("Everything");
            foreach (BusEffectType type in Enum.GetValues(typeof(BusEffectType)))
                bus.AddEffect(type);
            return bus;
        }
    }
}
