using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelite.Environment
{
    public class StylizedSkyManager : MonoBehaviour
    {
        public static StylizedSkyManager Instance { get; private set; }

        [Header("Cloud Configuration")]
        [SerializeField] private int cloudClusterCount = 24;
        [SerializeField] private float driftSpeed = 0.3f;
        [SerializeField] private Vector2 cloudAltitudeRange = new Vector2(85f, 135f);
        [SerializeField] private Vector2 cloudBoundsX = new Vector2(-220f, 220f);
        [SerializeField] private Vector2 cloudBoundsZ = new Vector2(-60f, 780f);

        private Material skyboxMat;
        private Material cloudMat;
        private Transform cloudParent;
        private List<Transform> cloudClusters = new List<Transform>();

        // Palette Colors
        private readonly Color colTopDefault = new Color(0.247f, 0.463f, 0.710f, 1.0f);    // #3F76B5
        private readonly Color colMidDefault = new Color(0.412f, 0.651f, 0.847f, 1.0f);    // #69A6D8
        private readonly Color colHorDefault = new Color(0.663f, 0.780f, 0.835f, 1.0f);    // #A9C7D5
        private readonly Color colSunDefault = new Color(1.000f, 0.898f, 0.639f, 1.0f);    // #FFE5A3

        private readonly Color colCloudPrimary = new Color(0.949f, 0.961f, 0.949f, 1.0f);  // #F2F5F2
        private readonly Color colCloudShadow  = new Color(0.796f, 0.851f, 0.875f, 1.0f);  // #CBD9DF

        private Coroutine skyTransitionCoroutine;

        public static void InitializeSky()
        {
            if (Instance == null)
            {
                GameObject skyObj = new GameObject("StylizedSkyManager");
                Instance = skyObj.AddComponent<StylizedSkyManager>();
                DontDestroyOnLoad(skyObj);
            }
            Instance.SetupSkyAndClouds();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetupSkyAndClouds()
        {
            SetupSkyboxMaterial();
            SetupCloudMaterial();
            GenerateLowPolyClouds();

            // Subscribe to region transitions
            BiomeRegionTrigger.OnRegionChanged -= OnRegionChanged;
            BiomeRegionTrigger.OnRegionChanged += OnRegionChanged;

            // Debug.Log($"[STYLIZED SKY] Initialized Built-in Skybox & {cloudClusters.Count} Low-Poly Clouds.");
        }

        private void SetupSkyboxMaterial()
        {
            Shader skyShader = Shader.Find("Roguelite/StylizedSkybox");
            if (skyShader == null)
            {
                Debug.LogError("[STYLIZED SKY] ERROR: Shader 'Roguelite/StylizedSkybox' not found!");
                return;
            }

            skyboxMat = new Material(skyShader);
            skyboxMat.name = "StylizedSkybox_Material";
            skyboxMat.SetColor("_SkyTopColor", colTopDefault);
            skyboxMat.SetColor("_SkyMidColor", colMidDefault);
            skyboxMat.SetColor("_SkyHorizonColor", colHorDefault);
            skyboxMat.SetColor("_SunColor", colSunDefault);
            
            // Match Sun Light direction (Euler 52, -35, 0 => direction forward)
            Vector3 sunDir = Quaternion.Euler(52f, -35f, 0f) * Vector3.forward;
            skyboxMat.SetVector("_SunDir", new Vector4(-sunDir.x, -sunDir.y, -sunDir.z, 0f));
            skyboxMat.SetFloat("_SunSize", 0.035f);
            skyboxMat.SetFloat("_SunHalo", 0.04f);

            RenderSettings.skybox = skyboxMat;
        }

        private void SetupCloudMaterial()
        {
            Shader cloudShader = Shader.Find("Roguelite/StylizedCloud") ?? Shader.Find("Standard");
            cloudMat = new Material(cloudShader);
            cloudMat.name = "StylizedCloud_Material";

            if (cloudMat.HasProperty("_TopColor")) cloudMat.SetColor("_TopColor", colCloudPrimary);
            if (cloudMat.HasProperty("_ShadowColor")) cloudMat.SetColor("_ShadowColor", colCloudShadow);
            
            Vector3 sunDir = Quaternion.Euler(52f, -35f, 0f) * Vector3.forward;
            if (cloudMat.HasProperty("_SunDir")) cloudMat.SetVector("_SunDir", new Vector4(-sunDir.x, -sunDir.y, -sunDir.z, 0f));

            if (cloudMat.HasProperty("_Color")) cloudMat.SetColor("_Color", colCloudPrimary);
            if (cloudMat.HasProperty("_BaseColor")) cloudMat.SetColor("_BaseColor", colCloudPrimary);
        }

        private void GenerateLowPolyClouds()
        {
            if (cloudParent != null) Destroy(cloudParent.gameObject);

            cloudParent = new GameObject("_StylizedCloudLayer").transform;
            cloudClusters.Clear();

            Random.State oldState = Random.state;
            Random.InitState(1337); // Seeded deterministic placement

            Mesh baseSphereMesh = CreateLowPolySphereMesh();

            for (int i = 0; i < cloudClusterCount; i++)
            {
                GameObject clusterObj = new GameObject($"StylizedCloudCluster_{i}");
                clusterObj.transform.SetParent(cloudParent, false);

                float posX = Random.Range(cloudBoundsX.x, cloudBoundsX.y);
                float posY = Random.Range(cloudAltitudeRange.x, cloudAltitudeRange.y);
                float posZ = Random.Range(cloudBoundsZ.x, cloudBoundsZ.y);
                clusterObj.transform.position = new Vector3(posX, posY, posZ);

                int puffCount = Random.Range(4, 8);
                for (int p = 0; p < puffCount; p++)
                {
                    GameObject puff = new GameObject($"Puff_{p}");
                    puff.transform.SetParent(clusterObj.transform, false);

                    Vector3 localOffset = new Vector3(
                        Random.Range(-12f, 12f),
                        Random.Range(-3f, 4f),
                        Random.Range(-10f, 10f)
                    );
                    puff.transform.localPosition = localOffset;

                    Vector3 scale = new Vector3(
                        Random.Range(8f, 18f),
                        Random.Range(5f, 10f),
                        Random.Range(8f, 18f)
                    );
                    puff.transform.localScale = scale;
                    puff.transform.localRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

                    MeshFilter mf = puff.AddComponent<MeshFilter>();
                    mf.sharedMesh = baseSphereMesh;

                    MeshRenderer mr = puff.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = cloudMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }

                cloudClusters.Add(clusterObj.transform);
            }

            Random.state = oldState;
        }

        private Mesh CreateLowPolySphereMesh()
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh origMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            Mesh copyMesh = Instantiate(origMesh);
            Destroy(primitive);
            copyMesh.name = "LowPolyCloudMesh";
            return copyMesh;
        }

        private void Update()
        {
            if (cloudClusters == null || cloudClusters.Count == 0) return;

            // Single update controller for lightweight horizontal cloud drift
            float deltaX = driftSpeed * Time.deltaTime;
            for (int i = 0; i < cloudClusters.Count; i++)
            {
                Transform t = cloudClusters[i];
                if (t == null) continue;

                Vector3 pos = t.position;
                pos.x += deltaX;
                if (pos.x > cloudBoundsX.y)
                {
                    pos.x = cloudBoundsX.x;
                }
                t.position = pos;
            }
        }

        private void OnRegionChanged(string regionName)
        {
            Color targetTop = colTopDefault;
            Color targetMid = colMidDefault;
            Color targetHor = colHorDefault;

            if (regionName.Contains("Forest"))
            {
                targetTop = new Color(0.231f, 0.435f, 0.620f, 1.0f);
                targetMid = new Color(0.373f, 0.580f, 0.710f, 1.0f);
                targetHor = new Color(0.596f, 0.737f, 0.780f, 1.0f);
            }
            else if (regionName.Contains("Boss") || regionName.Contains("Hollow"))
            {
                targetTop = new Color(0.290f, 0.247f, 0.420f, 1.0f);
                targetMid = new Color(0.482f, 0.361f, 0.502f, 1.0f);
                targetHor = new Color(0.620f, 0.459f, 0.522f, 1.0f);
            }

            if (skyTransitionCoroutine != null) StopCoroutine(skyTransitionCoroutine);
            skyTransitionCoroutine = StartCoroutine(TransitionSkyRoutine(targetTop, targetMid, targetHor, 2.5f));
        }

        private IEnumerator TransitionSkyRoutine(Color top, Color mid, Color hor, float duration)
        {
            if (skyboxMat == null) yield break;

            Color startTop = skyboxMat.GetColor("_SkyTopColor");
            Color startMid = skyboxMat.GetColor("_SkyMidColor");
            Color startHor = skyboxMat.GetColor("_SkyHorizonColor");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                skyboxMat.SetColor("_SkyTopColor", Color.Lerp(startTop, top, t));
                skyboxMat.SetColor("_SkyMidColor", Color.Lerp(startMid, mid, t));
                skyboxMat.SetColor("_SkyHorizonColor", Color.Lerp(startHor, hor, t));

                yield return null;
            }
        }

        private void OnDestroy()
        {
            BiomeRegionTrigger.OnRegionChanged -= OnRegionChanged;
        }
    }
}
