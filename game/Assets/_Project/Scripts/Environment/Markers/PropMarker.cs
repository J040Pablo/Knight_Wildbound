using UnityEngine;

namespace Roguelite.Environment
{
    /// <summary>
    /// Marks the position of a single environmental prop.
    /// SceneEnvironmentBuilder reads PropMarkers and builds placeholder visuals.
    /// To replace a prop with a real asset: remove the placeholder child renderer
    /// and attach the real prefab — the marker stays, the data stays, streaming works.
    /// </summary>
    public class PropMarker : MonoBehaviour
    {
        [Header("Prop Config")]
        public PlaceholderAssetKey assetKey         = PlaceholderAssetKey.RockMedium;
        public float               scale            = 1f;
        public float               rotationVariance = 15f;  // Random Y rotation ±N degrees
        public bool                blocksNavigation = false; // Hint for nav mesh baking
        public int                 variationSeed    = 0;     // Deterministic random seed

        [Header("Replacement")]
        [Tooltip("When true, WorldPlaceholderFactory skips building a primitive — real asset expected here.")]
        public bool assetOverrideReady = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = assetOverrideReady
                ? new Color(0.0f, 1.0f, 0.5f, 0.60f)
                : new Color(0.6f, 0.6f, 0.6f, 0.40f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * Mathf.Max(0.3f, scale * 0.5f));
        }
    }
}
