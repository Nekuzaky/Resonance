using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static partial class DemoBuilder
    {
        #region Private and Protected

        private static DemoAssets BuildAssets()
        {
            var assets = new DemoAssets();
            BuildMix(assets);
            BuildSyncs(assets);
            BuildEvents(assets);
            BuildEngine(assets);
            BuildMusic(assets);
            BuildSpace(assets);
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { AssetFolder }))
                EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)));
            return assets;
        }

        private static void BuildMix(DemoAssets assets)
        {
            assets.Master = Fresh<AudioBus>("Master");
            assets.Music = Fresh<AudioBus>("Music");
            assets.Sfx = Fresh<AudioBus>("SFX");
            assets.Voice = Fresh<AudioBus>("Voice");
            assets.Ambience = Fresh<AudioBus>("Ambience");
            assets.Master.volume = -6f;
            assets.Music.parent = assets.Master;
            assets.Music.volume = -4f;
            assets.Sfx.parent = assets.Master;
            assets.Voice.parent = assets.Master;
            assets.Ambience.parent = assets.Master;
            assets.Ambience.volume = -8f;
            assets.Voice.ducking.Add(new DuckRule { target = assets.Music, amount = -10f, attack = 0.15f, release = 0.8f });
            assets.Voice.ducking.Add(new DuckRule { target = assets.Ambience, amount = -6f, attack = 0.15f, release = 0.8f });

            assets.Pause = Fresh<MixSnapshot>("Pause Menu");
            assets.Pause.offsets.Add(new BusOffset { bus = assets.Music, volume = -12f });
            assets.Pause.offsets.Add(new BusOffset { bus = assets.Sfx, volume = -30f });
            assets.Pause.offsets.Add(new BusOffset { bus = assets.Ambience, volume = -30f });
        }

        private static void BuildSyncs(DemoAssets assets)
        {
            assets.CarSpeed = Fresh<SoundParameter>("Car Speed");
            assets.CarSpeed.max = 30f;
            assets.CarSpeed.scope = ParameterScope.PerEmitter;
            assets.CarSpeed.seekSpeed = 40f;

            assets.Surface = Fresh<SwitchGroup>("Surface");
            assets.Surface.values = new List<string> { "Grass", "Stone" };

            assets.Game = Fresh<StateGroup>("Game");
            assets.Game.states = new List<string> { "Explore", "Combat" };
        }

        private static void BuildEvents(DemoAssets assets)
        {
            var grass = RandomOf("Grass", new[] { "footstep_grass_1", "footstep_grass_2", "footstep_grass_3", "footstep_grass_4" });
            grass.switchValue = "Grass";
            var stone = RandomOf("Stone", new[] { "footstep_stone_1", "footstep_stone_2", "footstep_stone_3", "footstep_stone_4" });
            stone.switchValue = "Stone";
            var surface = new SwitchNode { name = "By Surface", group = assets.Surface, children = { grass, stone } };
            surface.volumeRandom = 2f;
            surface.pitchRandom = 1f;
            assets.Footsteps = Fresh<SoundEvent>("Footsteps");
            assets.Footsteps.root = surface;
            assets.Footsteps.bus = assets.Sfx;
            assets.Footsteps.spatial.maxDistance = 25f;

            assets.Wind = Fresh<SoundEvent>("Wind");
            assets.Wind.root = new ClipNode { name = "Wind Loop", clip = ClipAt("wind_loop"), loop = true };
            assets.Wind.bus = assets.Ambience;
            assets.Wind.spatialize = false;
            assets.Wind.fadeIn = 2f;
            assets.Wind.fadeOut = 2f;

            assets.Coin = Fresh<SoundEvent>("Coin");
            assets.Coin.root = new RandomNode { name = "Coin", children = { Clip("coin") }, pitchRandom = 1.5f };
            assets.Coin.bus = assets.Sfx;
            assets.Coin.priority = 160;

            assets.Click = Fresh<SoundEvent>("UI Click");
            assets.Click.root = Clip("ui_click");
            assets.Click.bus = assets.Sfx;
            assets.Click.spatialize = false;
            assets.Click.cooldown = 0.05f;

            assets.Narrator = Fresh<SoundEvent>("Narrator");
            var line = new DialogueNode { name = "Welcome", speaker = "Narrator", fallbackLanguage = "en" };
            line.lines.Add(new DialogueLine { language = "en", clip = ClipAt("voice_line"), subtitle = "Stone floors ring, grass hushes. Listen to the hall." });
            line.lines.Add(new DialogueLine { language = "fr", clip = ClipAt("voice_line_fr"), subtitle = "La pierre resonne, l'herbe etouffe. Ecoute la salle." });
            assets.Narrator.root = line;
            assets.Narrator.bus = assets.Voice;
            assets.Narrator.spatialize = false;
            assets.Narrator.priority = 220;
            assets.Narrator.maxInstances = 1;
            assets.Narrator.limitBehavior = LimitBehavior.RejectNew;
        }

        private static void BuildEngine(DemoAssets assets)
        {
            var low = new ClipNode { name = "Idle", clip = ClipAt("engine_low"), loop = true, blendRange = new Vector2(0f, 12f), blendFade = 8f };
            var high = new ClipNode { name = "Revving", clip = ClipAt("engine_high"), loop = true, blendRange = new Vector2(18f, 30f), blendFade = 8f };
            assets.Engine = Fresh<SoundEvent>("Car Engine");
            assets.Engine.root = new BlendNode { name = "By Speed", parameter = assets.CarSpeed, children = { low, high } };
            assets.Engine.bus = assets.Sfx;
            assets.Engine.volume = -4f;
            assets.Engine.fadeIn = 0.5f;
            assets.Engine.spatial.maxDistance = 40f;
            assets.Engine.modulations.Add(new Modulation
            {
                parameter = assets.CarSpeed,
                target = ModulationTarget.Pitch,
                curve = AnimationCurve.Linear(0f, -2f, 1f, 4f)
            });
        }

        private static void BuildSpace(DemoAssets assets)
        {
            assets.Foley = Fresh<AttenuationPreset>("Close Foley");
            assets.Foley.spatial.maxDistance = 25f;
            assets.Foley.spatial.lowPassAtMax = 3000f;
            assets.Footsteps.attenuation = assets.Foley;
            assets.Coin.attenuation = assets.Foley;

            assets.Engine.occlusion.enabled = true;
            assets.Engine.occlusion.volume = -10f;
            assets.Engine.occlusion.lowPass = 900f;
            assets.Engine.spatial.lowPassAtMax = 6000f;

            assets.Footsteps.caption = "Footsteps";
            assets.Footsteps.indicator = true;
            assets.Engine.caption = "Engine";
            assets.Engine.indicator = true;
            assets.Coin.caption = "Coin";
            assets.Coin.indicator = true;
            assets.Narrator.caption = "Narrator speaking";
            assets.Narrator.indicator = true;
            ((ClipNode)((RandomNode)assets.Coin.root).children[0]).markers.Add(new ClipMarker { name = "sparkle", time = 0.08f });

            assets.Bank = Fresh<SoundBank>("Demo Bank");
            assets.Bank.events.AddRange(new[] { assets.Footsteps, assets.Wind, assets.Engine, assets.Coin, assets.Click, assets.Narrator });
            assets.Bank.music.Add(assets.Score);
        }

        private static void BuildMusic(DemoAssets assets)
        {
            var score = Fresh<MusicTrack>("Adventure Score");
            score.bpm = MusicSynth.Bpm;
            score.beatsPerBar = 4;
            score.bus = assets.Music;
            assets.Intensity = Fresh<SoundParameter>("Intensity");
            assets.Intensity.seekSpeed = 0.6f;
            var explore = new MusicSegment { name = "Explore", clip = ClipAt("music_explore"), bars = 4 };
            explore.layers.Add(new MusicLayer { name = "Lead", clip = ClipAt("music_explore_lead"), parameter = assets.Intensity, range = new Vector2(0.45f, 1f), fade = 0.3f });
            score.segments.Add(explore);
            score.segments.Add(new MusicSegment { name = "Combat", clip = ClipAt("music_combat"), bars = 4 });
            score.segments.Add(new MusicSegment { name = "Rise", clip = ClipAt("stinger_alert"), bars = 1 });
            score.firstSegment = "Explore";
            score.stateGroup = assets.Game;
            score.links.Add(new MusicStateLink { state = "Explore", segment = "Explore" });
            score.links.Add(new MusicStateLink { state = "Combat", segment = "Combat", bridge = "Rise" });
            score.transitionSync = MusicSync.NextBar;
            score.crossfade = 0.15f;
            score.stingers.Add(new Stinger { name = "Alert", clip = ClipAt("stinger_alert"), sync = MusicSync.NextBeat });
            assets.Score = score;
        }

        #endregion
    }
}
