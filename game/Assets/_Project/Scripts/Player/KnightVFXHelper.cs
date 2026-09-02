using UnityEngine;

namespace Roguelite.Player
{
    /// <summary>
    /// Procedural VFX helpers for Knight abilities, following the same primitive-based
    /// convention as Player/Mage/MageVFXHelper.cs (the project currently ships with zero
    /// imported art, so all VFX are built from tinted primitives at runtime).
    /// </summary>
    public static class KnightVFXHelper
    {
        /// <summary>
        /// Spawns a small dark, translucent puff at the given position (used at both the
        /// origin and landing point of the Shadow Helm teleport). Self-destructs after
        /// "duration" seconds.
        /// </summary>
        public static GameObject CreateShadowPuff(Vector3 position, float scale, float duration)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "ShadowHelmPuffVFX";
            puff.transform.position = position + Vector3.up * 1.0f;
            puff.transform.localScale = Vector3.one * scale;

            Collider col = puff.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer rend = puff.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.15f, 0.08f, 0.22f, 0.75f); // dark violet-black smoke
            }

            // A couple of smaller wisps flung slightly outward make the puff read as a burst
            // rather than a single flat sphere.
            for (int i = 0; i < 4; i++)
            {
                float angle = 90f * i + Random.Range(-15f, 15f);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                SpawnWisp(position + Vector3.up * 0.6f, dir, scale * 0.35f, duration * 0.8f);
            }

            Object.Destroy(puff, duration);
            return puff;
        }

        private static void SpawnWisp(Vector3 origin, Vector3 direction, float scale, float duration)
        {
            GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            wisp.name = "ShadowHelmWispVFX";
            wisp.transform.position = origin;
            wisp.transform.localScale = Vector3.one * scale;

            Collider col = wisp.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Renderer rend = wisp.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.25f, 0.15f, 0.35f, 0.6f);
            }

            var runner = wisp.AddComponent<ShadowWispRunner>();
            runner.Run(direction, duration);

            Object.Destroy(wisp, duration + 0.05f);
        }

        /// <summary>
        /// Tiny self-contained mover so a wisp can drift outward and fade without needing
        /// an external MonoBehaviour to host a coroutine for it.
        /// </summary>
        private class ShadowWispRunner : MonoBehaviour
        {
            private Vector3 direction;
            private float duration;
            private float elapsed;
            private Vector3 startScale;
            private Renderer rend;

            public void Run(Vector3 dir, float dur)
            {
                direction = dir;
                duration = Mathf.Max(0.01f, dur);
                startScale = transform.localScale;
                rend = GetComponent<Renderer>();
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);

                transform.position += direction * (1.2f * Time.deltaTime);
                transform.localScale = startScale * (1f - p);

                if (rend != null)
                {
                    Color c = rend.material.color;
                    c.a = Mathf.Lerp(0.6f, 0f, p);
                    rend.material.color = c;
                }
            }
        }
    }
}
