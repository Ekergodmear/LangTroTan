using UnityEngine;

namespace LangTroTan.Presentation
{
    // Keeps a world-space label facing the active camera (development plaques).
    public sealed class SonMonBillboard : MonoBehaviour
    {
        static Camera cached;

        void LateUpdate()
        {
            if (cached == null || !cached.isActiveAndEnabled) cached = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (cached != null) transform.rotation = cached.transform.rotation;
        }
    }
}
