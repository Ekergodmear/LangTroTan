using UnityEngine;

namespace LangTroTan.Presentation
{
    // Scrolls a renderer's base map (cloud sea, falling water) through a property block, so several
    // renderers can share one material while drifting at different speeds.
    public sealed class SonMonDrift : MonoBehaviour
    {
        public Vector2 speed = new Vector2(0, -.3f);
        public float tiling = 1;

        static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
        Renderer target;
        MaterialPropertyBlock block;

        void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (target == null) return;
            var scale = transform.lossyScale;
            float aspect = scale.x > .01f ? scale.y / scale.x : 1;
            var offset = speed * Time.time;
            target.GetPropertyBlock(block);
            block.SetVector(BaseMapST, new Vector4(tiling, tiling * Mathf.Max(1, aspect * .25f), offset.x, offset.y));
            target.SetPropertyBlock(block);
        }
    }
}
