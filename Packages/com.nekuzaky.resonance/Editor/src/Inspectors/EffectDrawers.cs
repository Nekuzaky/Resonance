using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal static class EffectPaths
    {
        private static readonly Regex Index = new(@"^effects\.Array\.data\[(\d+)\]");

        public static int EffectIndex(SerializedProperty property)
        {
            var match = Index.Match(property.propertyPath);
            return match.Success ? int.Parse(match.Groups[1].Value) : -1;
        }

        public static BusEffect Effect(SerializedProperty property)
        {
            var index = EffectIndex(property);
            return property.serializedObject.targetObject is AudioBus bus && index >= 0 && index < bus.effects.Count ? bus.effects[index] : null;
        }

        public static List<string> EffectNames(AudioBus bus)
        {
            var names = new List<string>();
            if (bus == null)
                return names;
            for (var i = 0; i < bus.effects.Count; i++)
                names.Add(bus.effects[i] != null ? $"{i + 1}. {BusEffects.Label(bus.effects[i].type)}" : $"{i + 1}. (empty)");
            return names;
        }
    }

    [CustomPropertyDrawer(typeof(BusEffect))]
    internal sealed class BusEffectDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var index = EffectPaths.EffectIndex(property);
            if (property.serializedObject.targetObject is not AudioBus bus || index < 0 || index >= bus.effects.Count || bus.effects[index] == null)
                return new Label("Edit this effect in the Mixer tab of Resonance Studio.");
            var card = new EffectCard(bus, index, () => { });
            if (Glyphs.StyleSheet != null)
                card.Element.styleSheets.Add(Glyphs.StyleSheet);
            card.Element.schedule.Execute(() =>
            {
                if (index < bus.effects.Count && ReferenceEquals(bus.effects[index], card.Effect))
                    card.Refresh(Resonance.Engine);
            }).Every(200);
            return card.Element;
        }
    }

    [CustomPropertyDrawer(typeof(EffectModulation))]
    internal sealed class EffectModulationDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var effect = EffectPaths.Effect(property);
            var type = effect != null ? effect.type : BusEffectType.LowPass;
            var target = property.FindPropertyRelative("target");
            var curve = property.FindPropertyRelative("curve");
            root.Add(new PropertyField(property.FindPropertyRelative("parameter"), "Parameter"));
            var names = EffectEditing.ParameterNames(type);
            var choice = new DropdownField("Drives", names, Mathf.Clamp(target.intValue, 0, names.Count - 1));
            choice.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            choice.RegisterValueChangedCallback(evt =>
            {
                property.serializedObject.Update();
                target.intValue = Mathf.Max(0, names.IndexOf(evt.newValue));
                property.serializedObject.ApplyModifiedProperties();
            });
            root.Add(choice);
            root.Add(new PropertyField(curve, "Curve") { tooltip = "Across: the parameter from its minimum (0) to its maximum (1). Up: the setting's value." });
            root.Add(new Button(() =>
            {
                var info = BusEffects.Parameters(type)[Mathf.Clamp(target.intValue, 0, names.Count - 1)];
                property.serializedObject.Update();
                curve.animationCurveValue = AnimationCurve.Linear(0f, info.Min, 1f, info.Max);
                property.serializedObject.ApplyModifiedProperties();
            }) { text = "Use The Full Range", tooltip = "Reset the curve to a line from the setting's minimum to its maximum." });
            return root;
        }
    }

    [CustomPropertyDrawer(typeof(EffectOffset))]
    internal sealed class EffectOffsetDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var busProperty = property.FindPropertyRelative("bus");
            var effectProperty = property.FindPropertyRelative("effect");
            var parameterProperty = property.FindPropertyRelative("parameter");
            root.Add(new PropertyField(busProperty, "Bus"));
            var effect = new DropdownField("Effect");
            var parameter = new DropdownField("Setting");
            effect.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            parameter.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            root.Add(effect);
            root.Add(parameter);
            root.Add(new PropertyField(property.FindPropertyRelative("value"), "Value")
            {
                tooltip = "The value the setting fades to while the snapshot is applied."
            });
            void Refresh()
            {
                var bus = busProperty.objectReferenceValue as AudioBus;
                var index = bus != null ? bus.IndexOfEffect(effectProperty.intValue) : -1;
                effect.choices = EffectPaths.EffectNames(bus);
                effect.SetValueWithoutNotify(index >= 0 ? effect.choices[index] : "(choose)");
                var names = index >= 0 ? EffectEditing.ParameterNames(bus.effects[index].type) : new List<string>();
                parameter.choices = names;
                parameter.SetValueWithoutNotify(parameterProperty.intValue >= 0 && parameterProperty.intValue < names.Count ? names[parameterProperty.intValue] : "(choose)");
            }
            effect.RegisterValueChangedCallback(evt =>
            {
                var bus = busProperty.objectReferenceValue as AudioBus;
                var index = effect.choices.IndexOf(evt.newValue);
                if (bus == null || index < 0 || index >= bus.effects.Count || bus.effects[index] == null)
                    return;
                property.serializedObject.Update();
                effectProperty.intValue = bus.effects[index].id;
                parameterProperty.intValue = 0;
                property.serializedObject.ApplyModifiedProperties();
                Refresh();
            });
            parameter.RegisterValueChangedCallback(evt =>
            {
                property.serializedObject.Update();
                parameterProperty.intValue = Mathf.Max(0, parameter.choices.IndexOf(evt.newValue));
                property.serializedObject.ApplyModifiedProperties();
            });
            root.TrackPropertyValue(busProperty, _ => Refresh());
            Refresh();
            return root;
        }
    }
}
