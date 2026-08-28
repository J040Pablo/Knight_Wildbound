using System.Collections.Generic;
using UnityEngine;
using Roguelite.Progression;

namespace Roguelite.Player
{
    public class ClassVisuals : MonoBehaviour
    {
        private readonly Dictionary<VisualPartId, GameObject> visualPartsCache = new Dictionary<VisualPartId, GameObject>();
        private Transform visualContainer;

        private void OnEnable()
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnMasteryUnlocked += HandleMasteryUnlocked;
                ProgressionManager.Instance.OnClassSelected += HandleClassSelected;
            }
        }

        private void OnDisable()
        {
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.OnMasteryUnlocked -= HandleMasteryUnlocked;
                ProgressionManager.Instance.OnClassSelected -= HandleClassSelected;
            }
        }

        private void Start()
        {
            CreateVisualContainerAndParts();
            // Apply existing tiers if loading run
            if (ProgressionManager.Instance != null && ProgressionManager.Instance.CurrentClass != ClassType.None)
            {
                RefreshAllVisuals();
            }
        }

        private void CreateVisualContainerAndParts()
        {
            visualContainer = new GameObject("ClassVisualParts").transform;
            visualContainer.SetParent(transform, false);

            // Pre-create Helmet Path Low-Poly Parts
            // 1. Cape (N1)
            GameObject cape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cape.name = "HelmetCape_N1";
            cape.transform.SetParent(visualContainer, false);
            cape.transform.localPosition = new Vector3(0f, 0.9f, -0.28f);
            cape.transform.localScale = new Vector3(0.55f, 0.9f, 0.08f);
            SetPartColor(cape, new Color(0.75f, 0.15f, 0.18f));
            RemoveCollider(cape);
            visualPartsCache[VisualPartId.HelmetCape] = cape;

            // 2. Visor Helmet (N2)
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "HelmetVisor_N2";
            visor.transform.SetParent(visualContainer, false);
            visor.transform.localPosition = new Vector3(0f, 1.45f, 0.05f);
            visor.transform.localScale = new Vector3(0.48f, 0.42f, 0.48f);
            SetPartColor(visor, new Color(0.2f, 0.25f, 0.35f));
            RemoveCollider(visor);
            visualPartsCache[VisualPartId.HelmetVisor] = visor;

            // 3. Helmet Crest & Pauldrons (N3)
            GameObject crest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crest.name = "HelmetCrest_N3";
            crest.transform.SetParent(visualContainer, false);
            crest.transform.localPosition = new Vector3(0f, 1.72f, -0.05f);
            crest.transform.localScale = new Vector3(0.12f, 0.25f, 0.25f);
            crest.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            SetPartColor(crest, new Color(0.95f, 0.82f, 0.2f));
            RemoveCollider(crest);
            visualPartsCache[VisualPartId.HelmetCrest] = crest;

            // Pre-create Sword Path Low-Poly Parts
            // 1. Scaled Sword (N1)
            GameObject swordLarge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            swordLarge.name = "SwordLarge_N1";
            swordLarge.transform.SetParent(visualContainer, false);
            swordLarge.transform.localPosition = new Vector3(0.42f, 0.7f, 0.3f);
            swordLarge.transform.localScale = new Vector3(0.08f, 1.1f, 0.15f);
            SetPartColor(swordLarge, new Color(0.85f, 0.88f, 0.95f));
            RemoveCollider(swordLarge);
            visualPartsCache[VisualPartId.SwordLarge] = swordLarge;

            // 2. Broadsword (N2)
            GameObject swordBroad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            swordBroad.name = "SwordBroad_N2";
            swordBroad.transform.SetParent(visualContainer, false);
            swordBroad.transform.localPosition = new Vector3(0.42f, 0.75f, 0.35f);
            swordBroad.transform.localScale = new Vector3(0.12f, 1.35f, 0.22f);
            SetPartColor(swordBroad, new Color(0.95f, 0.95f, 1.0f));
            RemoveCollider(swordBroad);
            visualPartsCache[VisualPartId.SwordBroad] = swordBroad;

            // 3. Greatsword & Energy Glow (N3)
            GameObject swordGreat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            swordGreat.name = "SwordGreat_N3";
            swordGreat.transform.SetParent(visualContainer, false);
            swordGreat.transform.localPosition = new Vector3(0.42f, 0.85f, 0.40f);
            swordGreat.transform.localScale = new Vector3(0.16f, 1.6f, 0.30f);
            SetPartColor(swordGreat, new Color(0.3f, 0.8f, 1.0f));
            RemoveCollider(swordGreat);
            visualPartsCache[VisualPartId.SwordGreat] = swordGreat;

            // Pre-create Armor Path Low-Poly Parts
            // 1. Left Shield (N1)
            GameObject shieldSmall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shieldSmall.name = "ShieldSmall_N1";
            shieldSmall.transform.SetParent(visualContainer, false);
            shieldSmall.transform.localPosition = new Vector3(-0.45f, 0.8f, 0.2f);
            shieldSmall.transform.localScale = new Vector3(0.1f, 0.6f, 0.45f);
            SetPartColor(shieldSmall, new Color(0.4f, 0.45f, 0.55f));
            RemoveCollider(shieldSmall);
            visualPartsCache[VisualPartId.ShieldSmall] = shieldSmall;

            // 2. Tower Shield (N2)
            GameObject shieldTower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shieldTower.name = "ShieldTower_N2";
            shieldTower.transform.SetParent(visualContainer, false);
            shieldTower.transform.localPosition = new Vector3(-0.52f, 0.75f, 0.25f);
            shieldTower.transform.localScale = new Vector3(0.12f, 0.95f, 0.60f);
            SetPartColor(shieldTower, new Color(0.25f, 0.3f, 0.42f));
            RemoveCollider(shieldTower);
            visualPartsCache[VisualPartId.ShieldTower] = shieldTower;

            // 3. Heavy Guardian Armor (N3)
            GameObject shieldGuardian = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shieldGuardian.name = "ShieldGuardian_N3";
            shieldGuardian.transform.SetParent(visualContainer, false);
            shieldGuardian.transform.localPosition = new Vector3(-0.58f, 0.75f, 0.3f);
            shieldGuardian.transform.localScale = new Vector3(0.16f, 1.15f, 0.75f);
            SetPartColor(shieldGuardian, new Color(0.95f, 0.75f, 0.2f));
            RemoveCollider(shieldGuardian);
            visualPartsCache[VisualPartId.ShieldGuardian] = shieldGuardian;

            // Initially hide all prebuilt visual parts
            foreach (var kvp in visualPartsCache)
            {
                if (kvp.Value != null) kvp.Value.SetActive(false);
            }
        }

        private void SetPartColor(GameObject obj, Color col)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = col;
            }
        }

        private void RemoveCollider(GameObject obj)
        {
            Collider c = obj.GetComponent<Collider>();
            if (c != null)
            {
                Destroy(c);
            }
        }

        private void HandleClassSelected(ClassType cType)
        {
            RefreshAllVisuals();
        }

        private void HandleMasteryUnlocked(MasteryPath path, MasteryTier tier)
        {
            RefreshAllVisuals();
        }

        private void RefreshAllVisuals()
        {
            if (ProgressionManager.Instance == null || ProgressionManager.Instance.CurrentClass == ClassType.None) return;

            MasteryTier t1 = ProgressionManager.Instance.GetTier(MasteryPath.Path1);
            MasteryTier t2 = ProgressionManager.Instance.GetTier(MasteryPath.Path2);
            MasteryTier t3 = ProgressionManager.Instance.GetTier(MasteryPath.Path3);

            // Path 1 (Helmet)
            SetPartActive(VisualPartId.HelmetCape, t1 >= MasteryTier.N1);
            SetPartActive(VisualPartId.HelmetVisor, t1 >= MasteryTier.N2);
            SetPartActive(VisualPartId.HelmetCrest, t1 >= MasteryTier.N3);

            // Path 2 (Sword)
            SetPartActive(VisualPartId.SwordLarge, t2 == MasteryTier.N1);
            SetPartActive(VisualPartId.SwordBroad, t2 == MasteryTier.N2);
            SetPartActive(VisualPartId.SwordGreat, t2 == MasteryTier.N3);

            // Path 3 (Armor)
            SetPartActive(VisualPartId.ShieldSmall, t3 == MasteryTier.N1);
            SetPartActive(VisualPartId.ShieldTower, t3 == MasteryTier.N2);
            SetPartActive(VisualPartId.ShieldGuardian, t3 == MasteryTier.N3);
        }

        private void SetPartActive(VisualPartId partId, bool active)
        {
            if (visualPartsCache.TryGetValue(partId, out GameObject obj) && obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}
