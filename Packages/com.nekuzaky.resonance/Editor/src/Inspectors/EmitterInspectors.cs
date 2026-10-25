using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    [CustomEditor(typeof(SoundEmitter))]
    [CanEditMultipleObjects]
    internal sealed class SoundEmitterInspector : UnityEditor.Editor
    {
        private const SoundTrigger Physics = SoundTrigger.TriggerEnter | SoundTrigger.TriggerExit | SoundTrigger.CollisionEnter | SoundTrigger.CollisionExit;
        private const SoundTrigger Triggers = SoundTrigger.TriggerEnter | SoundTrigger.TriggerExit;
        private const SoundTrigger Collisions = SoundTrigger.CollisionEnter | SoundTrigger.CollisionExit;

        public override VisualElement CreateInspectorGUI()
        {
            var emitter = (SoundEmitter)target;
            var root = InspectorKit.Root(nameof(SoundEmitter), "Plays a Sound Event from this object.",
                InspectorHeader.Button("Play", () => Preview(emitter)),
                InspectorHeader.Button("Stop", () => Stop(emitter)),
                InspectorHeader.Button("Edit Sound", () =>
                {
                    if (emitter.sound != null)
                        StudioWindow.Open(emitter.sound);
                }));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "sound", "Sound", "The Sound Event to play."));
            root.Add(InspectorKit.Field(serializedObject, "playOn", "Play When", "Pick one or more moments. Triggers and collisions need a collider."));
            root.Add(InspectorKit.Field(serializedObject, "stopOn", "Stop When", "Disable is the usual choice, so loops stop with their object."));
            root.Add(InspectorKit.Field(serializedObject, "stopFade", "Stop Fade (s)", "-1 uses the event's own Fade Out."));
            root.Add(InspectorKit.Tag(serializedObject, "triggerTag", "Only For Tag", "Only react to triggers and collisions with objects that have this tag."));
            root.Add(InspectorKit.Field(serializedObject, "impactParameter", "Impact Parameter", "Receives the collision speed, so harder hits can sound louder."));
            root.Add(InspectorKit.Field(serializedObject, "impactScale", "Impact Scale"));
            root.Add(InspectorKit.Live(() => emitter.IsPlaying ? "Playing" : "Not playing"));
            InspectorKit.Watch(root, serializedObject, () => Validate(emitter, warnings));
            return root;
        }

        private static void Validate(SoundEmitter emitter, Warnings warnings)
        {
            if (emitter == null)
                return;
            warnings.Clear(false);
            var go = emitter.gameObject;
            var flags = emitter.playOn | emitter.stopOn;
            if (emitter.sound == null)
                warnings.Add(HelpBoxMessageType.Error, "Pick a Sound Event to play.");
            if (emitter.playOn == SoundTrigger.None)
                warnings.Add(HelpBoxMessageType.Info, "Play When is empty: the sound only plays when a script or a Unity Event calls Play().");
            if (((flags & Physics) != 0 || (flags & SoundTrigger.MouseDown) != 0) && !InspectorKit.HasCollider(go))
            {
                var trigger = (flags & Triggers) != 0;
                warnings.Add(HelpBoxMessageType.Warning, "Triggers, collisions and clicks need a collider on this object.",
                    trigger ? "Add a trigger collider" : "Add a collider", () => InspectorKit.AddBoxCollider(go, trigger));
            }
            else if ((flags & Triggers) != 0 && !InspectorKit.IsTrigger(go))
            {
                warnings.Add(HelpBoxMessageType.Warning, "Trigger events only fire when the collider is a trigger.", "Make it a trigger", () => InspectorKit.MakeTrigger(go));
            }
            if ((flags & Collisions) != 0 && go.GetComponent<Rigidbody>() == null && go.GetComponent<Rigidbody2D>() == null)
                warnings.Add(HelpBoxMessageType.Info, "Collisions need a Rigidbody on this object or on the object that hits it.");
            warnings.Render();
        }

        private static void Preview(SoundEmitter emitter)
        {
            if (Application.isPlaying)
                emitter.Play();
            else if (emitter.sound != null)
                PreviewEngine.Engine.Play(emitter.sound, null, Vector3.zero);
        }

        private static void Stop(SoundEmitter emitter)
        {
            if (Application.isPlaying)
                emitter.Stop();
            else if (emitter.sound != null)
                PreviewEngine.Engine.StopEvent(emitter.sound, -1f);
        }
    }

    [CustomEditor(typeof(SoundListener))]
    internal sealed class SoundListenerInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var listener = (SoundListener)target;
            var root = InspectorKit.Root(nameof(SoundListener), "The player hears from here. Distance, occlusion and reverb are measured from this object.");
            var warnings = new Warnings();
            root.Add(warnings);
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                var count = ActiveListeners();
                if (count > 1)
                    warnings.Add(HelpBoxMessageType.Warning, $"There are {count} Sound Listeners in the scene. Keep one; the last enabled wins.");
                if (listener != null && listener.GetComponent<AudioListener>() == null)
                    warnings.Add(HelpBoxMessageType.Info, "Unity renders 3D sound from the Audio Listener. Put both on the same object so what you hear matches.",
                        "Add an Audio Listener here", () => Undo.AddComponent<AudioListener>(listener.gameObject));
                warnings.Render();
            });
            return root;
        }

        private static int ActiveListeners()
        {
            var count = 0;
            foreach (var listener in Resources.FindObjectsOfTypeAll<SoundListener>())
            {
                if (!EditorUtility.IsPersistent(listener) && listener.gameObject.scene.IsValid() && listener.isActiveAndEnabled)
                    count++;
            }
            return count;
        }
    }

    [CustomEditor(typeof(ParameterDriver))]
    [CanEditMultipleObjects]
    internal sealed class ParameterDriverInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var driver = (ParameterDriver)target;
            var root = InspectorKit.Root(nameof(ParameterDriver), "Feeds a parameter from this object every frame.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "parameter", "Parameter"));
            root.Add(InspectorKit.Field(serializedObject, "source", "Read From"));
            var animator = InspectorKit.Field(serializedObject, "animator", "Animator");
            var animatorValue = InspectorKit.Choice(serializedObject, "animatorParameter", "Animator Float", () => AnimatorFloats(driver.animator), "(choose)");
            var constant = InspectorKit.Field(serializedObject, "constant", "Value");
            root.Add(animator);
            root.Add(animatorValue);
            root.Add(constant);
            root.Add(InspectorKit.Field(serializedObject, "multiplier", "Multiplier"));
            root.Add(InspectorKit.Field(serializedObject, "offset", "Offset"));
            root.Add(InspectorKit.Live(() => driver.parameter != null ? $"{driver.parameter.name} = {driver.Value:0.###}" : "No parameter"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                var fromAnimator = driver.source == DriverSource.AnimatorFloat;
                animator.style.display = fromAnimator ? DisplayStyle.Flex : DisplayStyle.None;
                animatorValue.style.display = fromAnimator ? DisplayStyle.Flex : DisplayStyle.None;
                constant.style.display = driver.source == DriverSource.Constant ? DisplayStyle.Flex : DisplayStyle.None;
                animatorValue.Refresh();
                Validate(driver, warnings);
            });
            return root;
        }

        private static void Validate(ParameterDriver driver, Warnings warnings)
        {
            warnings.Clear(false);
            if (driver.parameter == null)
                warnings.Add(HelpBoxMessageType.Error, "Pick the parameter this object drives.");
            if (driver.source == DriverSource.AnimatorFloat && driver.animator == null)
                warnings.Add(HelpBoxMessageType.Warning, "Pick the Animator to read from.", driver.GetComponent<Animator>() != null ? "Use the Animator on this object" : null,
                    driver.GetComponent<Animator>() != null ? () => { Undo.RecordObject(driver, "Use Animator"); driver.animator = driver.GetComponent<Animator>(); } : null);
            if (driver.parameter != null && driver.parameter.scope == ParameterScope.Global)
                warnings.Add(HelpBoxMessageType.Info, "This parameter is global: every driver writes the same value. Set its scope to Per Emitter to give each object its own.");
            warnings.Render();
        }

        private static IEnumerable<string> AnimatorFloats(Animator animator)
        {
            var names = new List<string>();
            if (animator == null || animator.runtimeAnimatorController is not UnityEditor.Animations.AnimatorController controller)
                return names;
            foreach (var parameter in controller.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float)
                    names.Add(parameter.name);
            }
            return names;
        }
    }

    [CustomEditor(typeof(SwitchSetter))]
    [CanEditMultipleObjects]
    internal sealed class SwitchSetterInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var setter = (SwitchSetter)target;
            var root = InspectorKit.Root(nameof(SwitchSetter), "Tells the sounds on this object which variation to play: surface, material, character...",
                InspectorKit.PlayButton("Apply Now", setter.Apply));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "group", "Switch Group"));
            var value = InspectorKit.Choice(serializedObject, "value", "Value", () => InspectorKit.Values(setter.group), "(choose)");
            root.Add(value);
            root.Add(InspectorKit.Field(serializedObject, "global", "For Every Object", "Set the value for every object that has none of its own."));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                value.Refresh();
                warnings.Clear(false);
                if (setter.group == null)
                    warnings.Add(HelpBoxMessageType.Error, "Pick a Switch Group, then choose its value.");
                else if (string.IsNullOrEmpty(setter.value))
                    warnings.Add(HelpBoxMessageType.Warning, "Choose a value.");
                else if (!setter.group.values.Contains(setter.value))
                    warnings.Add(HelpBoxMessageType.Error, $"'{setter.value}' is not a value of {setter.group.name}.");
                warnings.Render();
            });
            return root;
        }
    }
}
