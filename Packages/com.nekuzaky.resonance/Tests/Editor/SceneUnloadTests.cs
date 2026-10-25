using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class SceneUnloadTests : Fixture
    {
        private const string PrefabPath = "Assets/Resonance Snapshot Trigger Test.prefab";

        [TearDown]
        public void DeletePrefab()
        {
            Resonance.Override(null);
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        [Test]
        public void SnapshotTrigger_RemovesItsSnapshotWhenItsSceneUnloads()
        {
            var snapshot = Asset<MixSnapshot>("Underwater");
            snapshot.fadeIn = 0f;
            var source = Track(new GameObject("Trigger"));
            source.AddComponent<SnapshotTrigger>().snapshot = snapshot;
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            var trigger = prefab.GetComponent<SnapshotTrigger>();
            trigger.snapshot = snapshot;
            Assert.IsFalse(trigger.gameObject.scene.isLoaded);
            Resonance.Override(Engine);

            trigger.Apply();
            typeof(SnapshotTrigger).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(trigger, null);

            Assert.IsFalse(Engine.Mixer.IsActive(snapshot));
        }
    }
}
