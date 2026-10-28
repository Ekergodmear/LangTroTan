using System.Collections.Generic;
using UnityEngine;

namespace LangTroTan.Presentation
{
    // Procedural motion on the real skeletons exported by Tools/Blender/sonmon_creatures.py.
    // Each frame every driven bone is reset to its rest pose and then rotated about the character's own
    // axes, so the motion is independent of Blender bone roll. Presentation only.
    public sealed class SonMonPuppet : MonoBehaviour
    {
        public enum Kind { SpiritDeer, Disciple }

        public Kind kind;
        [Tooltip("World units per second; 0 = idle. Set by SonMonWalker or by the caller.")]
        public float speed;
        public float phaseOffset;

        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Vector3> restScale = new Dictionary<Transform, Vector3>();
        float stride;
        Transform basis;  // the imported model (parent of the "*_rig" armature): its axes define pitch/yaw

        void Awake()
        {
            basis = transform;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("_rig") && t.parent != null) basis = t.parent;
                if (!bones.ContainsKey(t.name)) bones[t.name] = t;
                rest[t] = t.localRotation;
                restScale[t] = t.localScale;
            }
            phaseOffset += Random.value * 10;
        }

        Transform Bone(string name) => bones.TryGetValue(name, out var t) ? t : null;

        void Pose(string bone, float pitch, float yaw = 0, float roll = 0)
        {
            var t = Bone(bone);
            if (t == null) return;
            t.localRotation = rest[t];
            var root = basis;
            t.rotation = Quaternion.AngleAxis(yaw, root.up) * Quaternion.AngleAxis(pitch, root.right) *
                         Quaternion.AngleAxis(roll, root.forward) * t.rotation;
        }

        void Breathe(string bone, float amount, float time)
        {
            var t = Bone(bone);
            if (t == null) return;
            float s = 1 + Mathf.Sin(time * 1.6f) * amount;
            t.localScale = Vector3.Scale(restScale[t], new Vector3(s, 1, s));
        }

        void LateUpdate()
        {
            float time = Time.time + phaseOffset;
            if (kind == Kind.SpiritDeer) Deer(time); else Disciple(time);
        }

        void Deer(float t)
        {
            Breathe("chest", .022f, t);
            // Slow, curious head movement: a gentle look-around with occasional dips.
            float look = Mathf.Sin(t * .31f) * 16 + Mathf.Sin(t * .83f) * 4;
            float dip = Mathf.Max(0, Mathf.Sin(t * .19f)) * 10 + Mathf.Sin(t * 1.1f) * 1.5f;
            Pose("neck", dip * .6f, look * .5f);
            Pose("head", dip * .4f, look * .5f, Mathf.Sin(t * .5f) * 3);
            float twitch = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 2.3f + 1)), 24) * 18;
            Pose("ear_l", 0, 0, twitch);
            Pose("ear_r", 0, 0, -Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * 1.7f)), 24) * 18);
            Pose("tail", Mathf.Sin(t * 3.1f) * 6 + 8, Mathf.Sin(t * 2.2f) * 10);
        }

        void Disciple(float t)
        {
            float walk = Mathf.Clamp01(speed / .8f);
            stride += Time.deltaTime * (speed > .01f ? 1.9f + speed * 1.6f : 0);
            float s = Mathf.Sin(stride * Mathf.PI);
            Breathe("chest", .012f, t);
            Pose("spine", 2 * walk + Mathf.Sin(t * 1.6f) * .8f, s * 4 * walk);
            Pose("chest", 0, -s * 5 * walk);
            Pose("neck", 0, Mathf.Sin(t * .37f) * 6 * (1 - walk));
            Pose("head", Mathf.Sin(t * .6f) * 2, Mathf.Sin(t * .29f) * 8 * (1 - walk), Mathf.Sin(t * .45f) * 3 * (1 - walk));
            Pose("thigh_l", -s * 24 * walk);
            Pose("thigh_r", s * 24 * walk);
            Pose("knee_l", Mathf.Max(0, s) * 30 * walk);
            Pose("knee_r", Mathf.Max(0, -s) * 30 * walk);
            // Arms rest slightly forward with hands near the sash at idle; they swing gently when walking.
            Pose("shoulder_l", 8 + s * 14 * walk, 0, -6);
            Pose("shoulder_r", 8 - s * 14 * walk, 0, 6);
            Pose("elbow_l", -18 * (1 - walk) - 8);
            Pose("elbow_r", -18 * (1 - walk) - 8);
        }
    }
}
