using UnityEngine;
using Roguelite.Core;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    /// <summary>
    /// Generic "player enters the grove → announce it → activate the mini-boss" gate, following
    /// the same shape as BossActivationTrigger but decoupled from any specific boss type (that
    /// script is hardcoded to HollowTreeBossAI) via a serialized reference instead. Used to gate
    /// the Giant Toxic Mushroom encounter without touching the Hollow Tree boss flow.
    /// </summary>
    public class MiniBossActivationTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject miniBossObject;
        [SerializeField] private string bannerText = "⚠️ A TOXIC PRESENCE STIRS...";

        private bool hasTriggered = false;

        private void Awake()
        {
            // The mini-boss GameObject starts disabled in the scene and is only switched on
            // once the player actually enters the grove.
            if (miniBossObject != null)
            {
                miniBossObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered || !PlayerDetectionUtility.IsPlayerCollider(other)) return;
            TriggerMiniBoss();
        }

        public void TriggerMiniBoss()
        {
            if (hasTriggered) return;
            hasTriggered = true;

            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner(bannerText);
            }

            if (miniBossObject != null)
            {
                miniBossObject.SetActive(true);
            }
        }
    }
}
