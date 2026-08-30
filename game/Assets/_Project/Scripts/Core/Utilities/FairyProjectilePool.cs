using System.Collections.Generic;
using UnityEngine;
using Roguelite.Combat;
using Roguelite.Player;

namespace Roguelite.Core.Utilities
{
    public class FairyProjectilePool : MonoBehaviour
    {
        public static FairyProjectilePool Instance { get; private set; }

        private readonly Queue<GameObject> pool = new Queue<GameObject>();
        private const int INITIAL_POOL_SIZE = 16;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                GameObject proj = CreateNewProjectileObject();
                proj.SetActive(false);
                pool.Enqueue(proj);
            }
        }

        private GameObject CreateNewProjectileObject()
        {
            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "PooledFairyProjectile";
            proj.transform.parent = transform;

            Collider col = proj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            return proj;
        }

        public GameObject GetProjectile(Vector3 startPos, Color color, float scale = 0.4f)
        {
            GameObject proj = pool.Count > 0 ? pool.Dequeue() : CreateNewProjectileObject();
            proj.transform.position = startPos;
            proj.transform.localScale = Vector3.one * scale;

            Renderer r = proj.GetComponent<Renderer>();
            if (r != null) r.material.color = color;

            proj.SetActive(true);
            return proj;
        }

        public void ReturnProjectile(GameObject proj)
        {
            if (proj == null) return;
            proj.SetActive(false);
            proj.transform.parent = transform;
            pool.Enqueue(proj);
        }
    }
}
