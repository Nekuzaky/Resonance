using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    [CustomEditor(typeof(StateZone))]
    [CanEditMultipleObjects]
    internal sealed class StateZoneInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var zone = (StateZone)target;
            var root = InspectorKit.Root(nameof(StateZone), "Changes a game state, and the mix, while the player is inside this collider.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Tag(serializedObject, "triggerTag", "Player Tag", "Only objects with this tag enter the zone."));
            root.Add(InspectorKit.Field(serializedObject, "group", "State Group"));
            var state = InspectorKit.Choice(serializedObject, "enterState", "State Inside", () => InspectorKit.States(zone.group), "(choose)");
            root.Add(state);
            root.Add(InspectorKit.Field(serializedObject, "restoreOnExit", "Restore On Exit", "Put the previous state back when the player leaves."));
            root.Add(InspectorKit.Field(serializedObject, "snapshot", "Mix Snapshot", "Optional: a mix applied while the player is inside."));
            root.Add(InspectorKit.Live(() => zone.Occupied ? "Player inside" : "Empty"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                state.Refresh();
                Validate(zone, warnings);
            });
            return root;
        }

        private static void Validate(StateZone zone, Warnings warnings)
        {
            warnings.Clear(false);
            var go = zone.gameObject;
            if (!InspectorKit.HasCollider(go))
                warnings.Add(HelpBoxMessageType.Error, "A zone needs a trigger collider to know when the player enters.", "Add a trigger collider",
                    () => InspectorKit.AddBoxCollider(go, true));
            else if (!InspectorKit.IsTrigger(go))
                warnings.Add(HelpBoxMessageType.Warning, "The collider is solid: the player bumps into it instead of entering.", "Make it a trigger",
                    () => InspectorKit.MakeTrigger(go));
            if (zone.group == null && zone.snapshot == null)
                warnings.Add(HelpBoxMessageType.Warning, "Pick a State Group, a Mix Snapshot, or both.");
            else if (zone.group != null && string.IsNullOrEmpty(zone.enterState))
                warnings.Add(HelpBoxMessageType.Warning, "Choose the state to set inside the zone.");
            else if (zone.group != null && !zone.group.states.Contains(zone.enterState))
                warnings.Add(HelpBoxMessageType.Error, $"'{zone.enterState}' is not a state of {zone.group.name}.");
            if (!string.IsNullOrEmpty(zone.triggerTag) && GameObject.FindGameObjectWithTag(zone.triggerTag) == null)
                warnings.Add(HelpBoxMessageType.Info, $"No object in the open scenes has the tag '{zone.triggerTag}' yet.");
            warnings.Render();
        }
    }

    [CustomEditor(typeof(ReverbZone))]
    [CanEditMultipleObjects]
    internal sealed class ReverbZoneInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var zone = (ReverbZone)target;
            var root = InspectorKit.Root(nameof(ReverbZone), "Gives this space its reverb. It fades in as the player approaches.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "preset", "Space", "The kind of room: small room, stone corridor, cave, hangar..."));
            root.Add(InspectorKit.Field(serializedObject, "intensity", "Intensity"));
            root.Add(InspectorKit.Field(serializedObject, "fadeDistance", "Fade Distance (m)", "How far outside the collider the reverb starts fading in."));
            root.Add(InspectorKit.Live(() =>
            {
                var engine = Resonance.Engine;
                return engine != null && engine.Reverb.Dominant == zone ? $"Active, {engine.Reverb.Wet * 100f:0}%" : "Listener is not here";
            }));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                var go = zone.gameObject;
                if (go.TryGetComponent<MeshCollider>(out var mesh) && !mesh.convex)
                    warnings.Add(HelpBoxMessageType.Error, "Concave mesh colliders cannot measure distances. Use a box, sphere or capsule, or tick Convex.");
                else if (go.GetComponent<Collider>() != null && !go.GetComponent<Collider>().isTrigger)
                    warnings.Add(HelpBoxMessageType.Info, "The collider is solid and blocks the player. A trigger is usually what you want.", "Make it a trigger",
                        () => InspectorKit.MakeTrigger(go));
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(MusicSource))]
    internal sealed class MusicSourceInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var source = (MusicSource)target;
            var root = InspectorKit.Root(nameof(MusicSource), "Plays a Music Track. Game states move it between segments.",
                InspectorHeader.Button("Play", () => Resonance.PlayMusic(source.track)),
                InspectorHeader.Button("Stop", () => Resonance.StopMusic(source.stopFade)));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "track", "Music Track"));
            root.Add(InspectorKit.Field(serializedObject, "playOnStart", "Play On Start"));
            root.Add(InspectorKit.Field(serializedObject, "stopOnDisable", "Stop On Disable"));
            root.Add(InspectorKit.Field(serializedObject, "stopFade", "Stop Fade (s)"));
            root.Add(InspectorKit.Live(() => Resonance.Engine?.Music.Current != null ? $"Playing {Resonance.Engine.Music.Current.name}" : "Stopped"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                if (source.track == null)
                    warnings.Add(HelpBoxMessageType.Error, "Pick the Music Track to play.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(SoundBankLoader))]
    internal sealed class SoundBankLoaderInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var loader = (SoundBankLoader)target;
            var root = InspectorKit.Root(nameof(SoundBankLoader), "Keeps a Sound Bank's audio in memory while this object is enabled.");
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "bank", "Sound Bank"));
            root.Add(InspectorKit.Field(serializedObject, "unloadOnDisable", "Unload On Disable"));
            root.Add(InspectorKit.Live(() => loader.bank == null ? "No bank" : loader.IsReady ? "Loaded and ready" : "Loading..."));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                if (loader.bank == null)
                    warnings.Add(HelpBoxMessageType.Error, "Pick the Sound Bank to load.");
                else if (loader.bank.events.Count == 0 && loader.bank.music.Count == 0)
                    warnings.Add(HelpBoxMessageType.Warning, $"{loader.bank.name} is empty. Add Sound Events and Music Tracks to it.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(SnapshotTrigger))]
    internal sealed class SnapshotTriggerInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var trigger = (SnapshotTrigger)target;
            var root = InspectorKit.Root(nameof(SnapshotTrigger), "Applies a mix snapshot while this object is enabled: pause menu, underwater, low health...",
                InspectorKit.PlayButton("Apply", trigger.Apply),
                InspectorKit.PlayButton("Remove", trigger.Remove));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "snapshot", "Mix Snapshot"));
            root.Add(InspectorKit.Field(serializedObject, "applyOnEnable", "Apply While Enabled"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                if (trigger.snapshot == null)
                    warnings.Add(HelpBoxMessageType.Error, "Pick the Mix Snapshot to apply.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(SubtitleRelay))]
    internal sealed class SubtitleRelayInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var relay = (SubtitleRelay)target;
            var root = InspectorKit.Root(nameof(SubtitleRelay), "Sends dialogue subtitles to your UI.");
            root.Add(new HelpBox("Click + under On Subtitle, drag your text object in, and pick its text property " +
                                 "(TextMeshPro: TextMeshProUGUI > text). Do the same under On Subtitle End and leave the text empty to clear it.",
                HelpBoxMessageType.Info));
            root.Add(InspectorKit.Choice(serializedObject, "speakerFilter", "Only Speaker", Speakers, "(everyone)",
                "Show only the lines of this character."));
            root.Add(InspectorKit.Field(serializedObject, "includeSpeakerName", "Show Speaker Name"));
            root.Add(InspectorKit.Field(serializedObject, "onSubtitle", "On Subtitle"));
            root.Add(InspectorKit.Field(serializedObject, "onSubtitleEnd", "On Subtitle End"));
            root.Add(InspectorKit.Live(() => string.IsNullOrEmpty(relay.Current) ? "No subtitle" : relay.Current));
            return root;
        }

        private static IEnumerable<string> Speakers()
        {
            var speakers = new SortedSet<string>();
            foreach (var sound in ResonanceAssets.FindAll<SoundEvent>())
            {
                var walker = new NodeWalker(sound.root);
                for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
                {
                    if (node is DialogueNode { speaker: { Length: > 0 } } dialogue)
                        speakers.Add(dialogue.speaker);
                }
            }
            return speakers;
        }
    }

    [CustomEditor(typeof(AnimationSounds))]
    internal sealed class AnimationSoundsInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var sounds = (AnimationSounds)target;
            var root = InspectorKit.Root(nameof(AnimationSounds), "Plays sounds from Animation Events.");
            root.Add(new HelpBox("In the Animation window, add an event at the frame you want. Pick the function PlaySound and drop " +
                                 "a Sound Event in its Object field. Or pick PlaySoundNamed and type one of the names below.", HelpBoxMessageType.Info));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "sounds", "Named Sounds"));
            root.Add(InspectorKit.Field(serializedObject, "emitter", "Play From", "Empty plays from this object. Pick a bone to play footsteps from the feet."));
            InspectorKit.Watch(root, serializedObject, () => Validate(sounds, warnings));
            return root;
        }

        private static void Validate(AnimationSounds sounds, Warnings warnings)
        {
            warnings.Clear(false);
            var seen = new HashSet<string>();
            foreach (var entry in sounds.sounds)
            {
                if (entry == null)
                    continue;
                if (string.IsNullOrEmpty(entry.name))
                    warnings.Add(HelpBoxMessageType.Warning, "A named sound has no name: PlaySoundNamed cannot reach it.");
                else if (!seen.Add(entry.name))
                    warnings.Add(HelpBoxMessageType.Error, $"The name '{entry.name}' is used twice: only the first one plays.");
                if (entry.sound == null)
                    warnings.Add(HelpBoxMessageType.Warning, $"'{entry.name}' has no Sound Event.");
            }
            warnings.Render();
        }
    }
}
