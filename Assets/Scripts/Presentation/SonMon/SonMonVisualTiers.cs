using System;
using UnityEngine;

namespace LangTroTan.Presentation
{
    // Data-driven mapping from the server's authoritative building level to a visual tier.
    // Presentation only: the tier never feeds back into gameplay, ids, placement or server state.
    [Serializable]
    public struct BuildingVisualTier
    {
        public string label;            // e.g. "II · Tàng Kinh Các trung phẩm"
        public int minLevel;            // first server level that shows this tier
        public GameObject prefab;       // main visual; null = inherit from the previous tier
        public GameObject[] decorations; // additive extras (lanterns, formations, crystals, VFX)
    }

    [Serializable]
    public struct BuildingVisualTierSet
    {
        public string type;             // server building type (SpatialCatalog id)
        public BuildingVisualTier[] tiers; // ascending minLevel
    }

    // Marks the swappable visual under a building root so tests and tools can read the active tier.
    public sealed class SonMonVisualTag : MonoBehaviour
    {
        public string type;
        public int level;
        public int tierIndex = -1;      // -1 = no tier set (single kit prefab or legacy)
        public string tierLabel;
        public string source;           // "tier", "tier-fallback", "kit" (diagnostics)
    }

    public static class SonMonTiers
    {
        // Tiers stay off in the production/preview scene until visually approved; QA enables them with
        // the -visual-tiers command-line flag.
        public static bool Enabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-visual-tiers") >= 0;

        // Resolves the tier for a level. Walks down from the highest qualifying tier to the first one
        // with a prefab, so a missing tier asset falls back to the nearest lower tier.
        public static bool Resolve(SonMonKit kit, string type, int level, out int index, out GameObject prefab, out bool fellBack)
        {
            index = -1;
            prefab = null;
            fellBack = false;
            if (!Enabled || kit == null || kit.visualTiers == null) return false;
            foreach (var set in kit.visualTiers)
            {
                if (set.type != type || set.tiers == null || set.tiers.Length == 0) continue;
                int wanted = 0;
                for (int i = 0; i < set.tiers.Length; i++)
                    if (level >= set.tiers[i].minLevel) wanted = i;
                for (int i = wanted; i >= 0; i--)
                {
                    if (set.tiers[i].prefab == null) continue;
                    index = wanted;
                    prefab = set.tiers[i].prefab;
                    fellBack = i != wanted;
                    return true;
                }
                return false;
            }
            return false;
        }

        public static BuildingVisualTier Tier(SonMonKit kit, string type, int index)
        {
            foreach (var set in kit.visualTiers)
                if (set.type == type) return set.tiers[index];
            return default;
        }
    }
}
