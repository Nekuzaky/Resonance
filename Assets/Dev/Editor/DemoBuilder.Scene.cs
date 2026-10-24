using Nekuzaky.Resonance.Demo;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static partial class DemoBuilder
    {
        #region Private and Protected

        private static void BuildScene(DemoAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 22f, -16f), Quaternion.Euler(52f, 0f, 0f));
            Object.DestroyImmediate(camera.GetComponent<AudioListener>());
            camera.gameObject.AddComponent<SoundIndicators>();
            camera.gameObject.AddComponent<ResonanceDebugOverlay>();

            Ground("Grass", new Vector3(-7.5f, 0f, 0f), new Color(0.32f, 0.52f, 0.3f));
            Ground("Stone", new Vector3(7.5f, 0f, 0f), new Color(0.5f, 0.5f, 0.52f));
            BuildPlayer(assets);
            BuildCar(assets);
            BuildZone(assets);
            BuildCoins(assets);
            BuildAmbience(assets);
            BuildHall();
            BuildWall();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void Paint(GameObject target, Color color) => target.AddComponent<DemoColor>().color = color;

        private static void Ground(string name, Vector3 position, Color color)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = $"{name} Ground";
            ground.transform.position = position + Vector3.down * 0.5f;
            ground.transform.localScale = new Vector3(15f, 1f, 24f);
            Paint(ground, color);
        }

        private static void BuildPlayer(DemoAssets assets)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(8f, 1f, 0f);
            Paint(player, new Color(0.18f, 0.66f, 0.72f));
            var body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            player.AddComponent<AudioListener>();
            player.AddComponent<SoundListener>();
            var intensity = player.AddComponent<DemoIntensity>();
            intensity.parameter = assets.Intensity;
            intensity.range = 10f;
            var walker = player.AddComponent<DemoWalker>();
            walker.footsteps = assets.Footsteps;
            walker.surface = assets.Surface;
        }

        private static void BuildCar(DemoAssets assets)
        {
            var car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "Car";
            car.transform.position = new Vector3(0f, 0.6f, -9f);
            car.transform.localScale = new Vector3(2.4f, 1.2f, 1.2f);
            Paint(car, new Color(0.86f, 0.55f, 0.24f));
            car.AddComponent<DemoMotion>().period = 9f;
            var emitter = car.AddComponent<SoundEmitter>();
            emitter.sound = assets.Engine;
            var driver = car.AddComponent<ParameterDriver>();
            driver.parameter = assets.CarSpeed;
            driver.source = DriverSource.Speed;
        }

        private static void BuildZone(DemoAssets assets)
        {
            var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = "Combat Zone";
            zone.transform.position = new Vector3(0f, 1f, 8f);
            zone.transform.localScale = new Vector3(7f, 2f, 4f);
            Paint(zone, new Color(0.85f, 0.3f, 0.3f));
            zone.GetComponent<Collider>().isTrigger = true;
            var player = GameObject.Find("Player");
            if (player != null)
                player.GetComponent<DemoIntensity>().target = zone.transform;
            var state = zone.AddComponent<StateZone>();
            state.group = assets.Game;
            state.enterState = "Combat";
        }

        private static void BuildHall()
        {
            var hall = new GameObject("Stone Hall");
            hall.transform.position = new Vector3(8f, 2f, 0f);
            var trigger = hall.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(5f, 4f, 7f);
            var zone = hall.AddComponent<ReverbZone>();
            zone.preset = AudioReverbPreset.StoneCorridor;
            zone.fadeDistance = 2f;
            for (var side = -1; side <= 1; side += 2)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = side < 0 ? "Hall Wall West" : "Hall Wall East";
                wall.transform.SetParent(hall.transform, false);
                wall.transform.localPosition = new Vector3(side * 2.7f, 0f, 0f);
                wall.transform.localScale = new Vector3(0.4f, 4f, 7f);
                Paint(wall, new Color(0.42f, 0.47f, 0.58f));
            }
        }

        private static void BuildWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Occluding Wall";
            wall.transform.position = new Vector3(5f, 1.5f, -6.5f);
            wall.transform.localScale = new Vector3(8f, 3f, 0.4f);
            Paint(wall, new Color(0.62f, 0.42f, 0.36f));
        }

        private static void BuildCoins(DemoAssets assets)
        {
            for (var i = 0; i < 6; i++)
            {
                var angle = i / 6f * Mathf.PI * 2f + 0.4f;
                var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coin.name = $"Coin {i + 1}";
                coin.transform.position = new Vector3(Mathf.Cos(angle) * 8f, 1f, Mathf.Sin(angle) * 8f);
                coin.transform.localScale = Vector3.one * 0.6f;
                Paint(coin, new Color(0.95f, 0.78f, 0.25f));
                coin.GetComponent<Collider>().isTrigger = true;
                var emitter = coin.AddComponent<SoundEmitter>();
                emitter.sound = assets.Coin;
                emitter.playOn = SoundTrigger.TriggerEnter;
                emitter.stopOn = SoundTrigger.None;
                emitter.triggerTag = "Player";
            }
        }

        private static void BuildAmbience(DemoAssets assets)
        {
            var audio = new GameObject("Audio");
            var wind = new GameObject("Wind");
            wind.transform.SetParent(audio.transform);
            wind.AddComponent<SoundEmitter>().sound = assets.Wind;
            audio.AddComponent<SoundBankLoader>().bank = assets.Bank;
            var music = new GameObject("Music");
            music.transform.SetParent(audio.transform);
            music.AddComponent<MusicSource>().track = assets.Score;
            var narrator = new GameObject("Narrator");
            narrator.transform.SetParent(audio.transform);
            var subtitles = narrator.AddComponent<DemoSubtitles>();
            var relay = narrator.AddComponent<SubtitleRelay>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(relay.onSubtitle, subtitles.Show);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(relay.onSubtitleEnd, subtitles.Hide);
            var repeater = narrator.AddComponent<DemoRepeater>();
            repeater.sound = assets.Narrator;
            repeater.firstDelay = 5f;
            repeater.interval = 14f;
        }

        #endregion
    }
}
