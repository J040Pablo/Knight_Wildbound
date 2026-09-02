using System.Collections.Generic;
using UnityEngine;
using Roguelite.Player;
using Roguelite.UI;

namespace Roguelite.Enemy
{
    /// <summary>
    /// Trigger volume surrounding the Stone Titan Arena (Z: 580, X: path+45).
    /// Locks perimeter stone barriers upon entry and initializes the World Boss HUD bar widget.
    /// </summary>
    public class StoneTitanArenaTrigger : MonoBehaviour
    {
        private bool isTriggered = false;
        private readonly List<GameObject> barrierPillars = new List<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                isTriggered = true;
                LockArena();

                // Find Stone Titan AI in arena
                AncientStoneTitanAI titan = Object.FindFirstObjectByType<AncientStoneTitanAI>();
                if (titan != null)
                {
                    titan.ActivateCombat();
                }
            }
        }

        private void LockArena()
        {
            Vector3 center = transform.position;
            const int count = 12;
            const float radius = 85f;

            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.PI * 2f / count);
                Vector3 pPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                pPos.y = Environment.SceneEnvironmentBuilder.GetTerrainHeightY(pPos.x, pPos.z);

                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                p.name = "TitanArenaBarrierPillar";
                p.transform.position = pPos;
                p.transform.localScale = new Vector3(3.5f, 12f, 3.5f);

                Renderer r = p.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.25f, 0.25f, 0.28f);

                barrierPillars.Add(p);
            }

            ThirdPersonCamera cam = Object.FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null) cam.TriggerShake(0.6f, 0.4f);
        }

        public void UnlockArena()
        {
            foreach (var p in barrierPillars)
            {
                if (p != null) Destroy(p);
            }
            barrierPillars.Clear();
        }
    }
}
