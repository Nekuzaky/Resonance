using System;
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
            Assert.AreEqual(rows, panel.Element.Query(className: "rs-effect-row").ToList().Count);
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

        private AudioBus BusWithEveryEffect()
        {
            var bus = Asset<AudioBus>("Everything");
            foreach (BusEffectType type in Enum.GetValues(typeof(BusEffectType)))
                bus.AddEffect(type);
            return bus;
        }
    }
}
