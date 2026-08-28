using UnityEngine;
using Roguelite.Player;

namespace Roguelite.Core
{
    public static class PlayerDetectionUtility
    {
        /// <summary>
        /// Unified helper to detect if a Collider belongs to the Player character or a Horse currently carrying the mounted Player.
        /// </summary>
        public static bool IsPlayerCollider(Collider other)
        {
            if (other == null) return false;

            // 1. Direct tag check
            if (other.CompareTag("Player")) return true;

            // 2. Direct component check on collider or parent
            if (other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                return true;
            }

            // 3. Mounted rider check (horse collider carrying mounted player)
            MountSystem mount = other.GetComponent<MountSystem>();
            if (mount == null)
            {
                mount = other.GetComponentInParent<MountSystem>();
            }

            if (mount != null && mount.IsPlayerMounted)
            {
                return true;
            }

            return false;
        }
    }
}
