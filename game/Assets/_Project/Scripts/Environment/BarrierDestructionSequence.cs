using System.Collections;
using UnityEngine;
using Roguelite.Wave;

namespace Roguelite.Environment
{
    public class BarrierDestructionSequence : MonoBehaviour
    {
        [SerializeField] private float collapseDuration = 2.0f;

        public void ExecuteDestruction()
        {
            StartCoroutine(PerformCollapseSequence());
        }

        private IEnumerator PerformCollapseSequence()
        {
            // 1. Status Banner Notification
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.TriggerBanner("💥 BARRIER DESTROYED! THE PATH IS OPEN!");
            }

            // 2. Disable blocking colliders immediately so player can pass through
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }

            // 3. Spawn Debris Shard Burst
            for (int i = 0; i < 16; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"WoodShard_{i}";
                shard.transform.position = transform.position + new Vector3(Random.Range(-12f, 12f), Random.Range(2f, 8f), Random.Range(-1f, 1f));
                shard.transform.localScale = new Vector3(Random.Range(0.4f, 1.2f), Random.Range(0.4f, 1.5f), Random.Range(0.4f, 1.2f));

                Renderer r = shard.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.28f, 0.16f, 0.12f);

                Rigidbody rb = shard.AddComponent<Rigidbody>();
                rb.AddForce(new Vector3(Random.Range(-5f, 5f), Random.Range(4f, 10f), Random.Range(3f, 8f)), ForceMode.Impulse);

                Destroy(shard, 2.5f);
            }

            // 4. Collapse Animation (barrier sinks into ground & scales down)
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + new Vector3(0, -10f, 0);
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = new Vector3(startScale.x, 0.05f, startScale.z);

            float elapsed = 0f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / collapseDuration);

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            gameObject.SetActive(false);
            Destroy(gameObject, 0.5f);
        }
    }
}
