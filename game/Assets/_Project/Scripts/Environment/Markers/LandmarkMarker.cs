using UnityEngine;

namespace Roguelite.Environment
{
    /// <summary>
    /// Marks a named landmark position. SceneEnvironmentBuilder reads this marker
    /// and spawns the appropriate placeholder visual via WorldPlaceholderFactory.
    /// When final assets are ready, the placeholder is swapped without moving the marker.
    /// isDistantVisible = true means the factory builds extra-large geometry
    /// so the landmark acts as a navigation beacon from far away.
    /// </summary>
    public class LandmarkMarker : MonoBehaviour
    {
        [Header("Landmark Config")]
        public LandmarkType landmarkType      = LandmarkType.RuinedTower;
        public float        scale             = 1f;
        public bool         isDistantVisible  = false; // Adds extra height for silhouette
        public string       displayName       = "";    // Optional tooltip / narrative name

        [Header("Placeholder Key")]
        public PlaceholderAssetKey placeholderKey = PlaceholderAssetKey.LandmarkWaterfall;

        private void OnDrawGizmos()
        {
            Gizmos.color = isDistantVisible
                ? new Color(1.0f, 0.8f, 0.0f, 0.90f)
                : new Color(0.8f, 0.5f, 0.0f, 0.70f);
            float r = Mathf.Max(1.5f, scale);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * r, r);
        }
    }
}
