using System;
using UnityEngine;

namespace LangTroTan.Presentation
{
    // Single source of truth for the visual world scale (approved 2026-10-28): architectural main door 0.52 Unity units,
    // character display height 0.85. Presentation only: it scales visual children, never a character or building root,
    // and never touches positions, footprints, movement, colliders used by gameplay, animation timing or server data.
    // Rollout is reversible: the asset's `enabled` flag (default off) or the -world-scale / -no-world-scale launch flags.
    public sealed class SonMonWorldScale : ScriptableObject
    {
        public bool enabled;
        [Tooltip("Architectural main-door height every building level is normalised to (Unity units).")]
        public float doorHeight = .52f;
        [Tooltip("Rendered height of disciples and worker characters (Unity units).")]
        public float characterHeight = .85f;

        const string ResourcePath = "SonMon/SonMonWorldScale";
        static SonMonWorldScale cached;
        static bool loaded;

        public static SonMonWorldScale Settings
        {
            get
            {
                if (!loaded)
                {
                    cached = Resources.Load<SonMonWorldScale>(ResourcePath);
                    if (cached == null) cached = CreateInstance<SonMonWorldScale>();  // code defaults, disabled
                    loaded = true;
                }
                return cached;
            }
        }

        public static bool Enabled
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                if (Array.IndexOf(args, "-no-world-scale") >= 0) return false;
                return Array.IndexOf(args, "-world-scale") >= 0 || Settings.enabled;
            }
        }

        // Scale a building level model so its measured main door becomes the standard door height.
        // doorHeightInModel is the door height in the model's current (prefab) units; 0 means "not measured".
        public static float BuildingFactor(float doorHeightInModel) =>
            Enabled && doorHeightInModel > 0 ? Settings.doorHeight / doorHeightInModel : 1f;

        // Fit a character's visual child to the standard display height. The root is never touched.
        // Returns the factor applied (1 when disabled or nothing measurable).
        public static float ApplyCharacter(Transform visual)
        {
            if (visual == null || !Enabled) return 1f;
            visual.localScale = Vector3.one;
            float height = RenderedHeight(visual);
            if (height <= .01f) return 1f;
            float factor = Settings.characterHeight / height;
            visual.localScale = Vector3.one * factor;
            return factor;
        }

        public static float RenderedHeight(Transform visual)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any ? bounds.size.y : 0;
        }

        // The dedicated child that holds a character's rendered geometry (created on demand).
        public static Transform VisualChild(Transform root)
        {
            var visual = root.Find("Visual");
            if (visual != null) return visual;
            visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);
            return visual;
        }
    }
}
