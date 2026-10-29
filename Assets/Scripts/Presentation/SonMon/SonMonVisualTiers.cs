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
        public float doorHeight;         // measured main-door height in prefab units (0 = not measured); see SonMonWorldScale
    }

    [Serializable]
    public struct BuildingVisualLevelSet
    {
        public string type;              // server building type (SpatialCatalog id)
        public string serverBuildingId;  // authoritative core building this family belongs to (e.g. tang_kinh_cac)
        public int maxLevel;             // mirrors the server cap (MAX_BUILDING_LEVEL)
        public BuildingVisualLevel[] levels;
    }

    // Marks the swappable visual under a building root so tests, tools and the upgrade FX can read it.
    public sealed class SonMonVisualTag : MonoBehaviour
    {
        public string type;
        public string serverBuildingId;  // core building id the visual was resolved for; empty for player-built
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

        // Families are resolved by authoritative building identity: a core building (spatial coreId) uses the set
        // made for its server building id; a player-built building only matches a set that names no server
        // building, so e.g. a player-built barracks never shows the Diễn Võ Trường arena.
        public static bool TryGetSet(SonMonKit kit, string type, string serverBuildingId, out BuildingVisualLevelSet set)
        {
            set = default;
            if (!Enabled || kit == null || kit.visualLevels == null) return false;
            bool core = !string.IsNullOrEmpty(serverBuildingId);
            foreach (var s in kit.visualLevels)
            {
                if (s.levels == null || s.levels.Length == 0) continue;
                bool match = core ? s.serverBuildingId == serverBuildingId : string.IsNullOrEmpty(s.serverBuildingId) && s.type == type;
                if (match) { set = s; return true; }
            }
            return false;
        }

        // Exact level model if present; otherwise the nearest LOWER level that has one (so a fallback is
        // never larger than the real model it stands in for). Saves above the cap show the max level.
        public static bool Resolve(SonMonKit kit, string type, string serverBuildingId, int level, out BuildingVisualLevel shown, out bool fellBack)
        {
            shown = default;
            fellBack = false;
            if (!TryGetSet(kit, type, serverBuildingId, out var set)) return false;
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

    }
}
