using UnityEngine;

namespace Roguelite.Player.Mage
{
    public static class MageVFXHelper
    {
        public static GameObject CreateGroundRune(Vector3 position, float radius, Color color, float duration)
        {
            GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rune.name = "MageGroundRuneVFX";
            rune.transform.position = position + Vector3.up * 0.05f;
            rune.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

            var col = rune.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = rune.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
            }

            Object.Destroy(rune, duration);
            return rune;
        }

        public static GameObject CreateImpactExplosion(Vector3 position, float radius, Color color, float duration = 0.35f)
        {
            GameObject exp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            exp.name = "MageImpactVFX";
            exp.transform.position = position;
            exp.transform.localScale = new Vector3(radius * 1.8f, radius * 1.8f, radius * 1.8f);

            var col = exp.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = exp.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
            }

            Object.Destroy(exp, duration);
            return exp;
        }

        public static GameObject CreateLightningStreak(Vector3 start, Vector3 end, Color color, float duration = 0.2f)
        {
            GameObject lineObj = new GameObject("LightningStreakVFX");
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.25f;
            lr.endWidth = 0.08f;
            lr.positionCount = 4;

            // Random zig-zag midpoints
            Vector3 dir = (end - start);
            Vector3 mid1 = start + dir * 0.33f + Random.insideUnitSphere * 0.4f;
            Vector3 mid2 = start + dir * 0.66f + Random.insideUnitSphere * 0.4f;

            lr.SetPosition(0, start);
            lr.SetPosition(1, mid1);
            lr.SetPosition(2, mid2);
            lr.SetPosition(3, end);

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.2f);

            Object.Destroy(lineObj, duration);
            return lineObj;
        }

        public static GameObject CreatePortalRing(Vector3 position, Vector3 forward, Color color, float scale = 2.0f, float duration = 2.0f)
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = "PortalRingVFX";
            portal.transform.position = position;
            Vector3 fwd = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            Vector3 upAxis = Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion rot = Quaternion.LookRotation(fwd, upAxis) * Quaternion.Euler(90f, 0, 0);
            rot.Normalize();
            portal.transform.rotation = rot;
            portal.transform.localScale = new Vector3(scale, 0.05f, scale);

            var col = portal.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var rend = portal.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = color;
            }

            Object.Destroy(portal, duration);
            return portal;
        }

        public static GameObject CreateSpectralHandVisual(Vector3 position, Color color, float duration = 1.2f)
        {
            GameObject handParent = new GameObject("SpectralHandVFX");
            handParent.transform.position = position;

            // Hand Palm
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.transform.SetParent(handParent.transform, false);
            palm.transform.localPosition = new Vector3(0, 0.4f, 0);
            palm.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            Object.Destroy(palm.GetComponent<Collider>());
            var pR = palm.GetComponent<Renderer>();
            if (pR != null) pR.material.color = color;

            // 4 Fingers
            for (int i = 0; i < 4; i++)
            {
                GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                finger.transform.SetParent(handParent.transform, false);
                finger.transform.localPosition = new Vector3(-0.45f + i * 0.3f, 1.0f, 0.5f);
                finger.transform.localScale = new Vector3(0.18f, 0.5f, 0.18f);
                Object.Destroy(finger.GetComponent<Collider>());
                var fR = finger.GetComponent<Renderer>();
                if (fR != null) fR.material.color = color;
            }

            Object.Destroy(handParent, duration);
            return handParent;
        }
    }
}
