using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Player.Mage
{
    public class MageObjectPool : MonoBehaviour
    {
        private static MageObjectPool instance;
        public static MageObjectPool Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("MageObjectPool");
                    instance = go.AddComponent<MageObjectPool>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private readonly Dictionary<string, Queue<GameObject>> pool = new Dictionary<string, Queue<GameObject>>();

        public GameObject GetPrimitiveSphere(string poolKey, Color color, Vector3 scale)
        {
            if (!pool.TryGetValue(poolKey, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pool[poolKey] = queue;
            }

            GameObject obj;
            if (queue.Count > 0)
            {
                obj = queue.Dequeue();
                if (obj == null) return GetPrimitiveSphere(poolKey, color, scale);
                obj.SetActive(true);
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = poolKey;
                var col = obj.GetComponent<SphereCollider>();
                if (col != null) col.isTrigger = true;
            }

            obj.transform.localScale = scale;
            var rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
            }

            return obj;
        }

        public void ReturnToPool(string poolKey, GameObject obj, float delay = 0f)
        {
            if (obj == null) return;
            if (delay > 0f)
            {
                StartCoroutine(ReturnDelayed(poolKey, obj, delay));
            }
            else
            {
                Recycle(poolKey, obj);
            }
        }

        private System.Collections.IEnumerator ReturnDelayed(string poolKey, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Recycle(poolKey, obj);
        }

        private void Recycle(string poolKey, GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(transform, false);

            if (!pool.TryGetValue(poolKey, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                pool[poolKey] = queue;
            }
            queue.Enqueue(obj);
        }
    }
}
