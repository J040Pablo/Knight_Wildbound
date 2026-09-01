using UnityEngine;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    public class TransitionArea : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private string completionMessage = "Biome 2 - Coming Soon";
        [SerializeField] private string subMessage = "The journey continues...";

        private bool messageTriggered = false;

        public void BuildTransitionPass(Transform parent, System.Func<float, float, float> getTerrainHeight)
        {
            transform.SetParent(parent, false);

            float startZ = 705f;
            float endZ = 760f;

            // Build ancient stone path slabs & side cliffs
            for (float z = startZ; z < endZ; z += 10f)
            {
                float y = getTerrainHeight != null ? getTerrainHeight(0, z) : 0f;

                // Ancient Stone Road Slabs
                GameObject road = WorldPlaceholderFactory.Build(PlaceholderAssetKey.StoneSteps, transform, null, 2.5f);
                road.transform.position = new Vector3(0, y, z);
                road.transform.rotation = Quaternion.identity;

                // Foggy Cliff Pillars framing the mountain pass
                GameObject cliffLeft = WorldPlaceholderFactory.Build(PlaceholderAssetKey.CliffPillar, transform, null, 1.8f);
                cliffLeft.transform.position = new Vector3(-18f, y, z);

                GameObject cliffRight = WorldPlaceholderFactory.Build(PlaceholderAssetKey.CliffPillar, transform, null, 1.8f);
                cliffRight.transform.position = new Vector3(18f, y, z);
            }

            // Teaser Signpost at end of pass (Z = 748)
            float endY = getTerrainHeight != null ? getTerrainHeight(0, 748f) : 0f;
            GameObject sign = WorldPlaceholderFactory.Build(PlaceholderAssetKey.TransitionGateSign, transform, null, 1.4f);
            sign.transform.position = new Vector3(0, endY, 748f);

            // Trigger Volume for completion prompt
            GameObject trigObj = new GameObject("TransitionCompletionTrigger");
            trigObj.transform.SetParent(transform, false);
            trigObj.transform.position = new Vector3(0, endY + 2f, 745f);

            BoxCollider col = trigObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(25f, 8f, 10f);

            trigObj.AddComponent<TransitionTriggerHandler>().Initialize(this);
        }

        public void OnPlayerReachedEnd()
        {
            if (messageTriggered) return;
            messageTriggered = true;

            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner($"🌄 {completionMessage.ToUpper()} — {subMessage}");
            }

            // Debug.Log($"[TransitionArea] Player reached transition end: {completionMessage}");
        }
    }

    public class TransitionTriggerHandler : MonoBehaviour
    {
        private TransitionArea transitionArea;

        public void Initialize(TransitionArea area)
        {
            transitionArea = area;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponentInParent<Player.PlayerController>() != null)
            {
                if (transitionArea != null)
                {
                    transitionArea.OnPlayerReachedEnd();
                }
            }
        }
    }
}
