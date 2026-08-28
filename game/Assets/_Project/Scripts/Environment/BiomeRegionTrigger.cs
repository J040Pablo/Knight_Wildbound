using System;
using System.Collections;
using UnityEngine;
using Roguelite.Core;

namespace Roguelite.Environment
{
    public class BiomeRegionTrigger : MonoBehaviour
    {
        [Header("Region Info")]
        [SerializeField] private string regionName = "Unknown Region";

        [Header("Atmosphere Configuration")]
        [SerializeField] private Color sunColor = new Color(1.0f, 0.9f, 0.75f);
        [SerializeField] private float sunIntensity = 1.2f;
        [SerializeField] private Color ambientColor = new Color(0.4f, 0.4f, 0.5f);
        [SerializeField] private float fogDensity = 0.015f;
        [SerializeField] private Color fogColor = new Color(0.4f, 0.4f, 0.5f);
        [SerializeField] private float transitionDuration = 1.8f;

        public string RegionName => regionName;
        public static string CurrentRegionName { get; private set; } = "Ruins";

        public static event Action<string> OnRegionChanged;

        private static Coroutine activeTransitionCoroutine;

        private void OnTriggerEnter(Collider other)
        {
            if (PlayerDetectionUtility.IsPlayerCollider(other))
            {
                ApplyRegionSettings();
            }
        }

        public void ApplyRegionSettings()
        {
            if (CurrentRegionName == regionName && activeTransitionCoroutine != null) return;

            CurrentRegionName = regionName;
            OnRegionChanged?.Invoke(regionName);

            MonoBehaviour runner = FindFirstObjectByType<MonoBehaviour>();
            if (runner != null)
            {
                if (activeTransitionCoroutine != null) runner.StopCoroutine(activeTransitionCoroutine);
                activeTransitionCoroutine = runner.StartCoroutine(TransitionAtmosphereRoutine());
            }
            else
            {
                ApplyInstantSettings();
            }
        }

        private IEnumerator TransitionAtmosphereRoutine()
        {
            Light sunLight = null;
            GameObject sunObj = GameObject.Find("Directional Sun Light");
            if (sunObj != null) sunLight = sunObj.GetComponent<Light>();

            Color startSunColor = sunLight != null ? sunLight.color : sunColor;
            float startSunIntensity = sunLight != null ? sunLight.intensity : sunIntensity;
            Color startAmbientColor = RenderSettings.ambientLight;
            Color startFogColor = RenderSettings.fogColor;
            float startFogDensity = RenderSettings.fogDensity;

            RenderSettings.fog = false;
            RenderSettings.fogMode = FogMode.Exponential;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                if (sunLight != null)
                {
                    sunLight.color = Color.Lerp(startSunColor, sunColor, t);
                    sunLight.intensity = Mathf.Lerp(startSunIntensity, sunIntensity, t);
                }

                RenderSettings.ambientLight = Color.Lerp(startAmbientColor, ambientColor, t);
                RenderSettings.fogColor = Color.Lerp(startFogColor, fogColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, fogDensity, t);

                yield return null;
            }

            ApplyInstantSettings();
            activeTransitionCoroutine = null;
        }

        private void ApplyInstantSettings()
        {
            GameObject sun = GameObject.Find("Directional Sun Light");
            if (sun != null)
            {
                Light l = sun.GetComponent<Light>();
                if (l != null)
                {
                    l.color = sunColor;
                    l.intensity = sunIntensity;
                }
            }

            RenderSettings.ambientLight = ambientColor;
            RenderSettings.fog = false;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
        }

        public void SetupRegion(string name, Color sColor, float sIntensity, Color aColor, float fDensity, Color fColor)
        {
            regionName = name;
            sunColor = sColor;
            sunIntensity = sIntensity;
            ambientColor = aColor;
            fogDensity = fDensity;
            fogColor = fColor;
        }
    }
}
