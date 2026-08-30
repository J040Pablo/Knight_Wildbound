using System.Collections;
using UnityEngine;
using Roguelite.Environment;

namespace Roguelite.Core
{
    public class PlayerSpawnTracker : MonoBehaviour
    {
        [Header("Debug Spheres Control")]
        [SerializeField] private bool createVisualDebugSpheres = false; // Default disabled per user requirement

        private GameObject redSphereWorldOrigin;
        private GameObject greenSphereSpawnPoint;
        private GameObject blueSpherePlayerPos;

        private Vector3 lastLoggedPos;

        public static PlayerSpawnTracker Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            lastLoggedPos = transform.position;
            if (createVisualDebugSpheres)
            {
                CreateDebugVisualSpheres();
            }
        }

        public void CreateDebugVisualSpheres()
        {
            // Clean up existing spheres if any
            DestroyVisualDebugSpheres();

            // 1. Red Sphere = World Origin (0,0,0)
            redSphereWorldOrigin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            redSphereWorldOrigin.name = "DEBUG_RED_WorldOrigin_0_0_0";
            redSphereWorldOrigin.transform.position = Vector3.zero;
            redSphereWorldOrigin.transform.localScale = Vector3.one * 1.5f;
            Collider rCol = redSphereWorldOrigin.GetComponent<Collider>();
            if (rCol != null) { rCol.enabled = false; Destroy(rCol); }
            Renderer rR = redSphereWorldOrigin.GetComponent<Renderer>();
            if (rR != null) rR.material.color = Color.red;

            // 2. Green Sphere = SpawnPoint Position
            PlayerSpawnPoint spNode = Object.FindFirstObjectByType<PlayerSpawnPoint>();
            Vector3 spPos = spNode != null ? spNode.transform.position : new Vector3(0, 0.5f, 8.0f);

            greenSphereSpawnPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            greenSphereSpawnPoint.name = "DEBUG_GREEN_SpawnPoint";
            greenSphereSpawnPoint.transform.position = spPos;
            greenSphereSpawnPoint.transform.localScale = Vector3.one * 1.5f;
            Collider gCol = greenSphereSpawnPoint.GetComponent<Collider>();
            if (gCol != null) { gCol.enabled = false; Destroy(gCol); }
            Renderer gR = greenSphereSpawnPoint.GetComponent<Renderer>();
            if (gR != null) gR.material.color = Color.green;

            // 3. Blue Sphere = Final Player Position
            blueSpherePlayerPos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blueSpherePlayerPos.name = "DEBUG_BLUE_PlayerPos";
            blueSpherePlayerPos.transform.position = transform.position;
            blueSpherePlayerPos.transform.localScale = Vector3.one * 1.2f;
            Collider bCol = blueSpherePlayerPos.GetComponent<Collider>();
            if (bCol != null) { bCol.enabled = false; Destroy(bCol); }
            Renderer bR = blueSpherePlayerPos.GetComponent<Renderer>();
            if (bR != null) bR.material.color = Color.blue;
        }

        public void DestroyVisualDebugSpheres()
        {
            if (redSphereWorldOrigin != null) Destroy(redSphereWorldOrigin);
            if (greenSphereSpawnPoint != null) Destroy(greenSphereSpawnPoint);
            if (blueSpherePlayerPos != null) Destroy(blueSpherePlayerPos);
        }

        public void LogInitial(Vector3 pos)
        {
            lastLoggedPos = pos;
        }

        public void LogAfterSpawnManager(Vector3 pos)
        {
            lastLoggedPos = pos;
        }

        public void LogCCDetails(CharacterController cc, Vector3 posBeforeEnable, Vector3 posAfterEnable)
        {
            lastLoggedPos = posAfterEnable;
        }

        public void LogAfterBootstrap(Vector3 pos)
        {
            lastLoggedPos = pos;
        }

        private IEnumerator Start()
        {
            yield return null;
        }

        private void Update()
        {
        }

        private void LateUpdate()
        {
            if (createVisualDebugSpheres && blueSpherePlayerPos != null)
            {
                blueSpherePlayerPos.transform.position = transform.position + Vector3.up * 1.0f;
            }
        }
    }
}
