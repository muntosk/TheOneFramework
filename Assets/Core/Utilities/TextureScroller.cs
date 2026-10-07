using UnityEngine;

namespace TheOneFramework.Utilities
{
    // Slides a renderer's texture over time (e.g. the fizzler's energy ripples). Uses a property
    // block so the shared material asset itself is never modified.
    [RequireComponent(typeof(Renderer))]
    public class TextureScroller : MonoBehaviour
    {
        [Tooltip("UV units per second.")]
        [SerializeField] private Vector2 speed = new Vector2(0.05f, 0.15f);

        [Tooltip("Texture repeats across the surface.")]
        [SerializeField] private Vector2 tiling = Vector2.one;

        [SerializeField] private string textureProperty = "_BaseMap";

        private Renderer target;
        private MaterialPropertyBlock block;
        private int stProperty;

        private void Awake()
        {
            target = GetComponent<Renderer>();
            block = new MaterialPropertyBlock();
            stProperty = Shader.PropertyToID(textureProperty + "_ST");
        }

        private void Update()
        {
            Vector2 offset = speed * Time.time;
            target.GetPropertyBlock(block);
            block.SetVector(stProperty, new Vector4(tiling.x, tiling.y, offset.x % 1.0f, offset.y % 1.0f));
            target.SetPropertyBlock(block);
        }
    }
}
