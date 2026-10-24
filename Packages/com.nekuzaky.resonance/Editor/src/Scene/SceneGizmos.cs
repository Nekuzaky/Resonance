using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    internal static class SceneGizmos
    {
        #region Private and Protected

        private static readonly Color Near = new(0.88f, 0.57f, 0.25f, 0.9f);
        private static readonly Color Far = new(0.88f, 0.57f, 0.25f, 0.3f);
        private static readonly Color Zone = new(0.36f, 0.6f, 0.84f, 0.12f);
        private static readonly Color ZoneEdge = new(0.36f, 0.6f, 0.84f, 0.8f);
        private static readonly Color Open = new(0.38f, 0.75f, 0.52f, 0.9f);
        private static readonly Color Blocked = new(0.89f, 0.38f, 0.38f, 0.9f);

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawEmitter(SoundEmitter emitter, GizmoType type)
        {
            var sound = emitter.sound;
            if (sound == null || !sound.spatialize)
                return;
            var spatial = sound.Spatial;
            var position = emitter.transform.position;
            Gizmos.color = Near;
            Gizmos.DrawWireSphere(position, spatial.minDistance);
            Gizmos.color = Far;
            Gizmos.DrawWireSphere(position, spatial.maxDistance);
            Handles.Label(position + Vector3.up * 0.6f, $"{sound.name}\nfull within {spatial.minDistance:0.#} m, silent beyond {spatial.maxDistance:0.#} m");
            if (Application.isPlaying)
                DrawOcclusion(emitter.gameObject);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawStateZone(StateZone zone, GizmoType type)
        {
            if (!zone.TryGetComponent<Collider>(out var collider))
                return;
            var bounds = collider.bounds;
            Gizmos.color = Zone;
            Gizmos.DrawCube(bounds.center, bounds.size);
            Gizmos.color = ZoneEdge;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            if ((type & GizmoType.Selected) != 0 || zone.Occupied)
                Handles.Label(bounds.center + Vector3.up * bounds.extents.y, ZoneLabel(zone));
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawListener(SoundListener listener, GizmoType type)
        {
            Gizmos.color = new Color(0.82f, 0.42f, 0.54f, 0.9f);
            Gizmos.DrawWireSphere(listener.transform.position, 0.35f);
        }

        private static string ZoneLabel(StateZone zone)
        {
            var state = zone.group != null ? $"{zone.group.name} = {zone.enterState}" : "no state";
            var snapshot = zone.snapshot != null ? $"\n+ {zone.snapshot.name}" : string.Empty;
            return state + snapshot;
        }

        private static void DrawOcclusion(GameObject emitter)
        {
            var engine = Resonance.Engine;
            var listener = engine?.ListenerTransform();
            if (listener == null)
                return;
            foreach (var instance in engine.Instances)
            {
                if (instance.Emitter != emitter)
                    continue;
                Gizmos.color = Color.Lerp(Open, Blocked, instance.Occlusion);
                Gizmos.DrawLine(instance.Position, listener.position);
            }
        }

        #endregion
    }
}
