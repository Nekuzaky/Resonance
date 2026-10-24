using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed partial class EventEditor
    {
        #region Private and Protected

        private VisualElement _props;

        private VisualElement BuildProperties()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("rs-props");
            _props = Ui.Box("rs-props-content");
            scroll.TrackSerializedObjectValue(_serialized, _ => _tree.RefreshItems());
            scroll.Add(_props);
            return scroll;
        }

        private void ShowProperties(Row row)
        {
            _props.Unbind();
            _props.Clear();
            if (row == null)
                return;
            if (row.IsEvent)
                EventProperties();
            else
                NodeProperties(row);
            _props.Bind(_serialized);
        }

        private void Field(string path, string label, string tooltip = null)
        {
            var field = new PropertyField { bindingPath = path, label = label, tooltip = tooltip };
            _props.Add(field);
        }

        private void EventProperties()
        {
            OutputProperties();
            SpatialProperties();
            SpaceProperties();
            VoiceProperties();
            _props.Add(Ui.GroupTitle(Glyphs.Parameter, "Parameter Modulation"));
            _props.Add(Ui.Help("Each curve reads a parameter (X goes from its minimum to its maximum) and outputs dB for Volume, " +
                               "semitones for Pitch or Hz for Low Pass and High Pass."));
            Field("modulations", "Modulations");
        }

        private void OutputProperties()
        {
            _props.Add(Ui.GroupTitle(Glyphs.Bus, "Output"));
            Field("bus", "Bus", "Mixer bus this sound plays on.");
            Field("volume", "Volume (dB)");
            Field("pitch", "Pitch (semitones)");
            Field("fadeIn", "Fade In (s)");
            Field("fadeOut", "Fade Out (s)", "Used when the sound is stopped.");
            Field("delay", "Delay (s)", "Wait before the sound starts.");
            Field("delayRandom", "Delay Random (s)", "Extra random wait, from 0 to this value.");
        }

        private void SpatialProperties()
        {
            _props.Add(Ui.GroupTitle(Glyphs.Emitter, "3D Sound"));
            Field("spatialize", "Spatialize", "Off: the sound is the same everywhere (UI, music). On: it comes from the emitter.");
            Field("attenuation", "Attenuation Preset", "Shared distance settings. When set, it replaces the settings below.");
            if (_sound.attenuation != null)
            {
                _props.Add(Ui.Help($"Distance settings come from '{_sound.attenuation.name}'. Edit the preset to change them for every sound that uses it."));
            }
            else
            {
                Field("spatial.blend", "3D Amount");
                Field("spatial.minDistance", "Full Volume Within (m)");
                Field("spatial.maxDistance", "Silent Beyond (m)");
                Field("spatial.rolloff", "Rolloff", "Custom uses the curve below: X is the distance divided by Silent Beyond.");
                Field("spatial.customCurve", "Custom Curve");
                Field("spatial.lowPassAtMax", "Low Pass At Max (Hz)", "Far sounds lose their highs. 22000 turns it off.");
                Field("spatial.spread", "Spread (degrees)");
                Field("spatial.doppler", "Doppler");
            }
            Field("followEmitter", "Follow Emitter");
            Field("virtualizeWhenFar", "Virtualize When Far", "Out of range voices stop using CPU and resume where they would be.");
            Field("stopWhenEmitterDestroyed", "Stop With Emitter");
        }

        private void SpaceProperties()
        {
            _props.Add(Ui.GroupTitle(Glyphs.Reverb, "Occlusion and Reverb"));
            Field("occlusion.enabled", "Occlusion", "Walls between the sound and the listener muffle it.");
            if (_sound.occlusion.enabled)
            {
                Field("occlusion.layers", "Blocking Layers");
                Field("occlusion.volume", "Occluded Volume (dB)");
                Field("occlusion.lowPass", "Occluded Low Pass (Hz)");
                Field("occlusion.width", "Ray Spread (m)", "Three rays this far apart: a sound half behind a corner is half occluded.");
                Field("occlusion.smoothing", "Smoothing (s)");
                Field("occlusion.interval", "Check Every (s)");
            }
            Field("reverbSend", "Reverb Send", "How much of this sound goes into the Reverb Zone around the listener.");
        }

        private void VoiceProperties()
        {
            _props.Add(Ui.GroupTitle(Glyphs.Voices, "Voices"));
            Field("maxInstances", "Max Instances", "0 means no limit.");
            Field("limitScope", "Limit Per");
            Field("limitBehavior", "When Full");
            Field("priority", "Priority", "Higher priority sounds steal voices from lower ones when the voice budget is full.");
            Field("cooldown", "Cooldown (s)", "Minimum time between two plays.");
        }

        private void NodeProperties(Row row)
        {
            var path = row.Path + ".";
            var title = Ui.GroupTitle(Glyphs.Of(row.Node.Kind), row.Node.Kind.ToString());
            _props.Add(title);
            _props.Add(Ui.Help(HelpOf(row.Node.Kind)));
            Field(path + "name", "Name");

            switch (row.Node)
            {
                case ClipNode clip: ClipProperties(path, clip); break;
                case RandomNode: RandomProperties(path); break;
                case SequenceNode: SequenceProperties(path); break;
                case SwitchNode switchNode: SwitchProperties(path, switchNode); break;
                case BlendNode: Field(path + "parameter", "Parameter"); break;
                case DialogueNode: DialogueProperties(path); break;
            }

            _props.Add(Ui.GroupTitle(Glyphs.Bus, "Mix"));
            Field(path + "volume", "Volume (dB)");
            Field(path + "volumeRandom", "Volume Random (+/- dB)");
            Field(path + "pitch", "Pitch (semitones)");
            Field(path + "pitchRandom", "Pitch Random (+/- st)");
            ParentProperties(row, path);
        }

        private static string HelpOf(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Random: return "Plays one of its children, picked at random. Weight changes how often each child is picked.";
                case NodeKind.Sequence: return "Step: each play takes the next child. Continuous: plays every child, one after the other.";
                case NodeKind.Switch: return "Plays the child whose value matches the switch on the emitter: surface, weapon, character...";
                case NodeKind.Blend: return "Plays every child at once and fades each one in or out with a parameter: speed, intensity, rain...";
                case NodeKind.Layer: return "Plays every child at the same time.";
                case NodeKind.Dialogue: return "Plays the line recorded in the player's language, or the fallback language, and sends its subtitle.";
                default: return "Plays one audio file.";
            }
        }

        private void ClipProperties(string path, ClipNode clip)
        {
            Field(path + "clip", "Audio Clip");
            Field(path + "loop", "Loop");
            Field(path + "randomStart", "Random Start", "Start at a random position, so several copies of a loop never sound in sync.");
            if (clip.clip == null)
                return;
            var waveform = new WaveformView();
            waveform.SetClip(clip.clip);
            _props.Add(waveform);
            var info = clip.clip;
            _props.Add(Ui.Help($"{info.length:0.00} s   ·   {info.channels} ch   ·   {info.frequency} Hz   ·   {info.loadType}"));
        }

        private void DialogueProperties(string path)
        {
            Field(path + "speaker", "Speaker", "Shown before the subtitle and used by Subtitle Relay filters.");
            Field(path + "fallbackLanguage", "Fallback Language", "Played when no line exists in the player's language.");
            Field(path + "lines", "Lines");
        }

        private void RandomProperties(string path)
        {
            Field(path + "shuffle", "Shuffle", "Play every child once, in a random order, before playing any of them again.");
            Field(path + "avoidRepeat", "Avoid Last", "How many of the last picks are skipped, so the same sound does not play twice in a row.");
        }

        private void SequenceProperties(string path)
        {
            Field(path + "mode", "Mode");
            Field(path + "loop", "Loop", "Continuous mode only: start again after the last child.");
        }

        private void SwitchProperties(string path, SwitchNode node)
        {
            Field(path + "group", "Switch Group");
            Field(path + "fallback", "Fallback Child", "Index of the child played when no value matches.");
            if (node.group == null)
                return;
            _props.Add(Ui.TextButton(Glyphs.Magic, "Add a child for each missing value", () => FillSwitch(node)));
        }

        private void FillSwitch(SwitchNode node)
        {
            var missing = new List<string>(node.group.values);
            foreach (var child in node.children)
            {
                if (child != null)
                    missing.Remove(child.switchValue);
            }
            if (missing.Count == 0)
                return;
            Commit("Fill switch", () =>
            {
                foreach (var value in missing)
                    node.children.Add(new RandomNode { name = value, switchValue = value });
            });
            SelectNode(node);
        }

        private void ParentProperties(Row row, string path)
        {
            switch (row.Parent)
            {
                case RandomNode:
                    _props.Add(Ui.GroupTitle(Glyphs.Random, "In Random"));
                    Field(path + "weight", "Weight", "2 means picked twice as often as a child with weight 1.");
                    break;
                case SwitchNode switchNode:
                    _props.Add(Ui.GroupTitle(Glyphs.Switch, "In Switch"));
                    _props.Add(SwitchValueField(row.Node, switchNode));
                    break;
                case BlendNode blend:
                    _props.Add(Ui.GroupTitle(Glyphs.Blend, "In Blend"));
                    _props.Add(BlendRangeField(path, blend));
                    Field(path + "blendFade", "Fade Width", "How far outside the range the child fades out.");
                    break;
            }
        }

        private VisualElement SwitchValueField(SoundNode node, SwitchNode parent)
        {
            var choices = parent.group != null ? new List<string>(parent.group.values) : new List<string>();
            if (!string.IsNullOrEmpty(node.switchValue) && !choices.Contains(node.switchValue))
                choices.Add(node.switchValue);
            var field = new DropdownField("Plays When", choices, node.switchValue ?? string.Empty);
            field.RegisterValueChangedCallback(evt =>
            {
                Commit("Change switch value", () => node.switchValue = evt.newValue);
                SelectNode(node);
            });
            return field;
        }

        private static VisualElement BlendRangeField(string path, BlendNode blend)
        {
            var parameter = blend.parameter;
            var low = parameter != null ? Mathf.Min(parameter.min, parameter.max) : 0f;
            var high = parameter != null ? Mathf.Max(parameter.min, parameter.max) : 1f;
            var slider = new MinMaxSlider("Active Range", low, high, low, high) { bindingPath = path + "blendRange" };
            var box = new VisualElement();
            box.Add(slider);
            box.Add(new PropertyField { bindingPath = path + "blendRange", label = "Range Values" });
            return box;
        }

        #endregion
    }
}
