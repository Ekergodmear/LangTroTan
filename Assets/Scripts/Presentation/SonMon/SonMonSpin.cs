using UnityEngine;

namespace LangTroTan.Presentation
{
    // Slow rotation (formation circles) with a gentle breathing glow scale.
    public sealed class SonMonSpin : MonoBehaviour
    {
        public float degreesPerSecond = 6;
        public float pulse = .03f;
        Vector3 baseScale;

        void Awake() => baseScale = transform.localScale;

        void Update()
        {
            transform.Rotate(0, degreesPerSecond * Time.deltaTime, 0, Space.World);
            transform.localScale = baseScale * (1 + Mathf.Sin(Time.time * 1.3f) * pulse);
        }
    }
}
