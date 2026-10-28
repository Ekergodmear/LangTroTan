using System;
using UnityEngine;

namespace LangTroTan.Presentation
{
    // Data-driven mapping from the server's authoritative building level (1..maxLevel, 10 by design)
    // to one visual per level. Presentation only: never feeds back into gameplay, ids, placement or
    // server state.
    [Serializable]
    public struct BuildingVisualLevel
    {
        public int level;                // exact server level this entry represents (1..maxLevel)
        public GameObject prefab;        // null = no model supplied yet (development fallback applies)
        public GameObject[] decorations; // optional additive extras for this level
        public string status;            // "approved" | "candidate" | "placeholder" (never shown as final art unless approved)
        public float targetHeight;       // height the builder fitted the model to (monotonic ladder)
    }

    [Serializable]
    public struct BuildingVisualLevelSet
    {
        public string type;              // server building type (SpatialCatalog id)
        public int maxLevel;             // mirrors the server cap (MAX_BUILDING_LEVEL)
        public BuildingVisualLevel[] levels;
    }

    // Marks the swappable visual under a building root so tests, tools and the upgrade FX can read it.
    public sealed class SonMonVisualTag : MonoBehaviour
    {
        public string type;
        public int level;                // authoritative server level this visual represents
        public int modelLevel = -1;      // level whose model is shown (differs from level on fallback)
        public string status;            // status of the shown model
        public string source;            // "level" | "fallback" | "kit" (diagnostics)
        public bool Fallback => source == "fallback";
    }

    public static class SonMonTiers
    {
        // Level visuals stay off in the production/preview scene until visually approved; QA enables
        // them with the -visual-tiers command-line flag.
        public static bool Enabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-visual-tiers") >= 0;

        public static bool TryGetSet(SonMonKit kit, string type, out BuildingVisualLevelSet set)
        {
            set = default;
            if (!Enabled || kit == null || kit.visualLevels == null) return false;
            foreach (var s in kit.visualLevels)
                if (s.type == type && s.levels != null && s.levels.Length > 0) { set = s; return true; }
            return false;
        }

        // Exact level model if present; otherwise the nearest LOWER level that has one (so a fallback is
        // never larger than the real model it stands in for). Saves above the cap show the max level.
        public static bool Resolve(SonMonKit kit, string type, int level, out BuildingVisualLevel shown, out bool fellBack)
        {
            shown = default;
            fellBack = false;
            if (!TryGetSet(kit, type, out var set)) return false;
            int wanted = Mathf.Clamp(level, 1, Mathf.Max(1, set.maxLevel));
            for (int l = wanted; l >= 1; l--)
                foreach (var entry in set.levels)
                    if (entry.level == l && entry.prefab != null)
                    {
                        shown = entry;
                        fellBack = l != wanted;
                        return true;
                    }
            return false;
        }

        public static int MaxLevel(SonMonKit kit, string type) => TryGetSet(kit, type, out var set) ? set.maxLevel : 10;
    }
}
