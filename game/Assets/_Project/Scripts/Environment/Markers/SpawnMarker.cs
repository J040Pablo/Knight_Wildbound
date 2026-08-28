using UnityEngine;

namespace Roguelite.Environment
{
    public enum SpawnEntityType { Player, Horse }

    /// <summary>
    /// Marks a valid spawn position for a world entity.
    /// SceneEnvironmentBuilder reads all SpawnMarkers in the zone
    /// and delegates actual spawning to PlayerSpawnManager / HorseController.
    /// Replace the placeholder visual without touching spawn logic.
    /// </summary>
    public class SpawnMarker : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        public SpawnEntityType entityType  = SpawnEntityType.Player;
        public string          markerLabel = "Spawn";
        public bool            isPrimary   = false; // Only one primary per entity type

        [Header("Placeholder Visual")]
        public PlaceholderAssetKey placeholderKey = PlaceholderAssetKey.FriendlyHorse;

        // Runtime — filled by the bootstrap after spawning
        [HideInInspector] public GameObject spawnedEntity;

        private void OnDrawGizmos()
        {
            Gizmos.color = entityType == SpawnEntityType.Player
                ? new Color(0.2f, 0.6f, 1.0f, 0.85f)
                : new Color(0.9f, 0.7f, 0.2f, 0.85f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.7f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
        }
    }
}
