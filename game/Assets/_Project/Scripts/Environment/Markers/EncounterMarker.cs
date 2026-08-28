using UnityEngine;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    /// <summary>
    /// Marks a gameplay encounter zone. SceneEnvironmentBuilder reads this marker
    /// and attaches the correct EncounterZone + BoxCollider at runtime.
    /// Replacing visuals around the marker never changes encounter data.
    /// </summary>
    public class EncounterMarker : MonoBehaviour
    {
        [Header("Encounter Config")]
        public EncounterDifficulty difficulty  = EncounterDifficulty.Easy;
        public Vector3             zoneSize    = new Vector3(60f, 8f, 50f);
        public bool                hardGate    = false; // If true, blocks progress until cleared
        public string              encounterID = "";     // Unique ID for save-state tracking

        // Runtime state
        [HideInInspector] public bool isCleared = false;

        private void OnDrawGizmos()
        {
            Color c = difficulty switch
            {
                EncounterDifficulty.Easy   => new Color(0.2f, 1.0f, 0.3f, 0.30f),
                EncounterDifficulty.Medium => new Color(1.0f, 0.8f, 0.1f, 0.30f),
                EncounterDifficulty.Hard   => new Color(1.0f, 0.2f, 0.2f, 0.30f),
                _                          => new Color(1.0f, 1.0f, 1.0f, 0.20f)
            };
            Gizmos.color = c;
            Gizmos.DrawCube(transform.position, zoneSize);
            Gizmos.color = new Color(c.r, c.g, c.b, 0.85f);
            Gizmos.DrawWireCube(transform.position, zoneSize);
        }
    }
}
