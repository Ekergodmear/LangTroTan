using UnityEngine;

namespace LangTroTan.Presentation
{
    // Applies the standard character display height (SonMonWorldScale) to this character's visual child on spawn.
    // The root transform (position, rotation, scale) stays exactly as gameplay and movement set it.
    [DisallowMultipleComponent]
    public sealed class SonMonCharacterVisual : MonoBehaviour
    {
        public Transform visual;  // the single child holding the rendered model
        public float AppliedFactor { get; private set; } = 1;

        void Awake()
        {
            if (visual == null && transform.childCount == 1) visual = transform.GetChild(0);
            AppliedFactor = SonMonWorldScale.ApplyCharacter(visual);
        }
    }
}
