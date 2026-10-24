using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    [CustomEditor(typeof(VolumeSetting))]
    internal sealed class VolumeSettingInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var setting = (VolumeSetting)target;
            var root = InspectorKit.Root(nameof(VolumeSetting), "Lets the player set a bus volume in your options menu. The value is saved.");
            root.Add(new HelpBox("On your UI Slider (range 0 to 1), click + under On Value Changed, drag this object in and pick " +
                                 "VolumeSetting > SetVolume (dynamic float). Then click + under On Loaded below, drag the Slider in and " +
                                 "pick Slider > value, so the slider shows the saved volume.", HelpBoxMessageType.Info));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "bus", "Bus", "Usually Music, SFX or Voice."));
            root.Add(InspectorKit.Field(serializedObject, "defaultVolume", "Default Volume", "Used the first time the game runs."));
            root.Add(InspectorKit.Field(serializedObject, "saveBetweenSessions", "Remember Between Sessions"));
            root.Add(InspectorKit.Field(serializedObject, "onLoaded", "On Loaded"));
            root.Add(InspectorKit.Live(() => $"{setting.Volume * 100f:0}%  ({Ui.Decibels(VolumeSetting.ToDecibels(setting.Volume))})"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                if (setting.bus == null)
                    warnings.Add(HelpBoxMessageType.Error, "Pick the bus this setting controls.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(MusicBeatEvents))]
    internal sealed class MusicBeatEventsInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var events = (MusicBeatEvents)target;
            var root = InspectorKit.Root(nameof(MusicBeatEvents), "Calls your Unity Events on the music's beats and bars: lights that pulse, enemies that move in time.");
            root.Add(InspectorKit.Field(serializedObject, "everyBeats", "Every N Beats", "1 fires on every beat, 2 on every other beat..."));
            root.Add(InspectorKit.Field(serializedObject, "onBeat", "On Beat"));
            root.Add(InspectorKit.Field(serializedObject, "onBar", "On Bar"));
            root.Add(InspectorKit.Field(serializedObject, "onSegment", "On Segment"));
            root.Add(InspectorKit.Live(() => events.Bar > 0 ? $"Bar {events.Bar}  ·  Beat {events.Beat}" : "Waiting for music"));
            return root;
        }
    }

    [CustomEditor(typeof(SoundCallbacks))]
    internal sealed class SoundCallbacksInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = InspectorKit.Root(nameof(SoundCallbacks), "Calls your Unity Events when a sound on this object reaches a marker or ends.");
            root.Add(new HelpBox("Markers are placed on clips in Resonance Studio (select a clip node, Markers list). Sounds must be played " +
                                 "from this object, by a Sound Emitter or Resonance.Play(sound, gameObject).", HelpBoxMessageType.Info));
            root.Add(InspectorKit.Choice(serializedObject, "markerFilter", "Only Marker", MarkerNames, "(every marker)"));
            root.Add(InspectorKit.Field(serializedObject, "onMarker", "On Marker"));
            root.Add(InspectorKit.Field(serializedObject, "onSoundEnded", "On Sound Ended"));
            return root;
        }

        private static IEnumerable<string> MarkerNames()
        {
            var names = new SortedSet<string>();
            foreach (var sound in ResonanceAssets.FindAll<SoundEvent>())
            {
                var walker = new NodeWalker(sound.root);
                for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
                {
                    if (node is not ClipNode clip)
                        continue;
                    foreach (var marker in clip.markers)
                    {
                        if (!string.IsNullOrEmpty(marker?.name))
                            names.Add(marker.name);
                    }
                }
            }
            return names;
        }
    }

    [CustomEditor(typeof(SoundIndicators))]
    internal sealed class SoundIndicatorsInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var indicators = (SoundIndicators)target;
            var root = InspectorKit.Root(nameof(SoundIndicators), "Shows where sounds come from, for players who are deaf or hard of hearing.");
            root.Add(new HelpBox("Only Sound Events with Visual Indicator ticked are shown (Studio > event > Accessibility). Give them a short " +
                                 "Caption such as \"Footsteps\" or \"Gunfire\". 3D sounds get an arrow around the screen center, 2D sounds a caption " +
                                 "at the bottom.", HelpBoxMessageType.Info));
            var warnings = new Warnings();
            root.Add(warnings);
            root.Add(InspectorKit.Field(serializedObject, "viewCamera", "Camera", "Empty uses the main camera."));
            root.Add(InspectorKit.Field(serializedObject, "radius", "Radius (px)"));
            root.Add(InspectorKit.Field(serializedObject, "minimumLoudness", "Hide Below", "Quiet or far sounds below this loudness are not shown."));
            root.Add(InspectorKit.Field(serializedObject, "showCaptions", "Show Captions"));
            root.Add(InspectorKit.Field(serializedObject, "showNonDirectional", "Show 2D Sounds"));
            root.Add(InspectorKit.Field(serializedObject, "color", "Color"));
            root.Add(InspectorKit.Field(serializedObject, "fontSize", "Text Size"));
            root.Add(InspectorKit.Live(() => $"{indicators.Visible} indicators on screen"));
            InspectorKit.Watch(root, serializedObject, () =>
            {
                warnings.Clear(false);
                var flagged = 0;
                foreach (var sound in ResonanceAssets.FindAll<SoundEvent>())
                {
                    if (sound.indicator)
                        flagged++;
                }
                if (flagged == 0)
                    warnings.Add(HelpBoxMessageType.Warning, "No Sound Event has Visual Indicator ticked yet, so nothing will show.");
                warnings.Render();
            });
            return root;
        }
    }

    [CustomEditor(typeof(ResonanceDebugOverlay))]
    internal sealed class DebugOverlayInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = InspectorKit.Root(nameof(ResonanceDebugOverlay), "Draws every playing sound in the Game view, with its distance, occlusion and level.");
            root.Add(InspectorKit.Field(serializedObject, "viewCamera", "Camera", "Empty uses the main camera."));
            root.Add(InspectorKit.Field(serializedObject, "onlyInDevelopmentBuilds", "Only In Development", "Hidden in release builds, shown in the editor and development builds."));
            root.Add(InspectorKit.Field(serializedObject, "showLabels", "Show Labels"));
            root.Add(InspectorKit.Field(serializedObject, "showSummary", "Show Summary"));
            return root;
        }
    }
}
