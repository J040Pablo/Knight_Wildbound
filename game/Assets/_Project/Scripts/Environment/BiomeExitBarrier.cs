using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    public class BiomeExitBarrier : MonoBehaviour
    {
        [Header("Barrier Visual Components")]
        [SerializeField] private List<Renderer> barrierRenderers = new List<Renderer>();
        [SerializeField] private List<GameObject> rootTendrils = new List<GameObject>();
        [SerializeField] private List<GameObject> crackObjects = new List<GameObject>();
        [SerializeField] private ParticleSystem corruptionParticles;

        private Color intactColor = new Color(0.22f, 0.10f, 0.14f);
        private Color weakenedColor = new Color(0.38f, 0.26f, 0.20f);
        private Color corruptedGlowColor = new Color(0.85f, 0.15f, 0.25f);
        private Color fadedGlowColor = new Color(0.25f, 0.20f, 0.15f);

        private bool isWeakened = false;

        private void Awake()
        {
            CollectChildVisuals();
        }

        private void CollectChildVisuals()
        {
            barrierRenderers.Clear();
            rootTendrils.Clear();
            crackObjects.Clear();

            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                barrierRenderers.Add(r);
                if (r.gameObject.name.Contains("Tendril"))
                {
                    rootTendrils.Add(r.gameObject);
                }
                if (r.gameObject.name.Contains("Crack"))
                {
                    crackObjects.Add(r.gameObject);
                    r.gameObject.SetActive(false);
                }
            }
        }

        public void ApplyBossDefeatedWeakening()
        {
            if (isWeakened) return;
            isWeakened = true;

            StartCoroutine(PerformWeakeningTransition());
        }

        private IEnumerator PerformWeakeningTransition()
        {
            float duration = 2.5f;
            float elapsed = 0f;

            // Retract roots slightly
            Vector3[] startPositions = new Vector3[rootTendrils.Count];
            Vector3[] targetPositions = new Vector3[rootTendrils.Count];
            for (int i = 0; i < rootTendrils.Count; i++)
            {
                if (rootTendrils[i] == null) continue;
                startPositions[i] = rootTendrils[i].transform.localPosition;
                targetPositions[i] = startPositions[i] + new Vector3(0, -0.6f, (i % 2 == 0 ? 0.4f : -0.4f));
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Retract tendrils
                for (int i = 0; i < rootTendrils.Count; i++)
                {
                    if (rootTendrils[i] != null)
                    {
                        rootTendrils[i].transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                    }
                }

                // Shift material colors from corrupted purple to faded wood
                foreach (var r in barrierRenderers)
                {
                    if (r == null) continue;
                    if (r.gameObject.name.Contains("GlowVein"))
                    {
                        r.material.color = Color.Lerp(corruptedGlowColor, fadedGlowColor, t);
                    }
                    else
                    {
                        r.material.color = Color.Lerp(intactColor, weakenedColor, t);
                    }
                }

                yield return null;
            }

            // Spawn initial subtle surface cracks
            SetCrackStage(0.25f);

            if (corruptionParticles != null)
            {
                var main = corruptionParticles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.4f, 0.4f, 0.4f, 0.3f));
            }
        }

        public void SetCrackStage(float damagePercent)
        {
            // Create visible procedural crack slabs as health decreases
            int activeCracks = Mathf.FloorToInt(damagePercent * 4f);
            for (int i = 0; i < crackObjects.Count; i++)
            {
                if (crackObjects[i] != null)
                {
                    crackObjects[i].SetActive(i < activeCracks);
                }
            }

            // If crack objects aren't pre-built, dynamically add visual crack slabs
            if (crackObjects.Count == 0 && damagePercent > 0.1f)
            {
                int cracksToSpawn = Mathf.Min(4, Mathf.FloorToInt(damagePercent * 4f));
                for (int i = 0; i < cracksToSpawn; i++)
                {
                    GameObject crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crack.name = $"BarrierCrack_{i}";
                    crack.transform.SetParent(transform, false);
                    crack.transform.localPosition = new Vector3((i - 1.5f) * 4.5f, 5.0f + (i % 2) * 1.5f, -1.4f);
                    crack.transform.localScale = new Vector3(0.4f, 3.5f, 0.15f);
                    crack.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 25f : -25f));

                    Collider c = crack.GetComponent<Collider>();
                    if (c != null) Destroy(c);

                    Renderer r = crack.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.95f, 0.80f, 0.40f);

                    crackObjects.Add(crack);
                }
            }
        }
    }
}
