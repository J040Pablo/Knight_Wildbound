using UnityEngine;

namespace Roguelite.Environment
{
    public class ArenaBuilder : MonoBehaviour
    {
        [Header("Arena Setup")]
        [SerializeField] private float arenaRadius = 30.0f;
        [SerializeField] private int treeCount = 45;
        [SerializeField] private int rockCount = 25;

        private void Awake()
        {
            BuildForestArena();
        }

        public void BuildForestArena()
        {
            // 1. Ground Plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "ArenaGround";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(arenaRadius * 2.2f, 0.5f, arenaRadius * 2.2f);
            
            Renderer gRenderer = ground.GetComponent<Renderer>();
            if (gRenderer != null)
            {
                gRenderer.material.color = new Color(0.22f, 0.52f, 0.28f); // Soft forest green
            }

            // 2. Lighting & Sky
            GameObject sun = new GameObject("Directional Sun Light");
            Light lightComp = sun.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1.0f, 0.95f, 0.85f);
            lightComp.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientLight = new Color(0.35f, 0.45f, 0.55f);

            // 3. Boundary Wall Rocks
            int boundaryCount = 36;
            for (int i = 0; i < boundaryCount; i++)
            {
                float angle = (i / (float)boundaryCount) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * (arenaRadius + 1f), 1.5f, Mathf.Sin(angle) * (arenaRadius + 1f));
                
                GameObject wallRock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallRock.name = "BoundaryCliff";
                wallRock.transform.position = pos;
                wallRock.transform.localScale = new Vector3(4f, 6f, 4f);
                wallRock.transform.rotation = Quaternion.Euler(Random.Range(-10, 10), Random.Range(0, 360), Random.Range(-10, 10));
                
                Renderer r = wallRock.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.35f, 0.35f, 0.40f);
            }

            // 4. Scatter Trees
            for (int i = 0; i < treeCount; i++)
            {
                Vector2 randPos = Random.insideUnitCircle * (arenaRadius - 4f);
                if (randPos.magnitude < 6f) continue; // Keep safe center area clear

                Vector3 pos = new Vector3(randPos.x, 0, randPos.y);
                CreateLowPolyTree(pos);
            }

            // 5. Scatter Rocks
            for (int i = 0; i < rockCount; i++)
            {
                Vector2 randPos = Random.insideUnitCircle * (arenaRadius - 3f);
                if (randPos.magnitude < 5f) continue;

                Vector3 pos = new Vector3(randPos.x, 0.4f, randPos.y);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "ForestRock";
                rock.transform.position = pos;
                float scale = Random.Range(1.0f, 2.5f);
                rock.transform.localScale = new Vector3(scale, scale * 0.7f, scale);
                Renderer r = rock.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.45f, 0.45f, 0.48f);
            }

            // 6. Central Campfire Prop
            GameObject campfire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            campfire.name = "CampfireCenter";
            campfire.transform.position = new Vector3(0, 0.1f, 0);
            campfire.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);
            Renderer cRenderer = campfire.GetComponent<Renderer>();
            if (cRenderer != null) cRenderer.material.color = new Color(0.3f, 0.2f, 0.1f);

            // 7. Authoritative Player Spawn Point
            GameObject playerSpawnObj = new GameObject("PlayerSpawnPoint");
            playerSpawnObj.transform.position = GetSafePlayerSpawnPosition();
        }

        public Vector3 GetSafePlayerSpawnPosition()
        {
            Vector3 centerPos = new Vector3(0, 5.0f, -3.0f);
            if (Physics.Raycast(centerPos, Vector3.down, out RaycastHit hit, 10.0f))
            {
                return hit.point + new Vector3(0, 1.0f, 0);
            }
            return new Vector3(0, 1.2f, -3.0f);
        }

        private void CreateLowPolyTree(Vector3 position)
        {
            GameObject treeGroup = new GameObject("LowPolyTree");
            treeGroup.transform.position = position;

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = treeGroup.transform;
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            Renderer tR = trunk.GetComponent<Renderer>();
            if (tR != null) tR.material.color = new Color(0.4f, 0.25f, 0.15f);

            // Foliage Cone 1
            GameObject foliage1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foliage1.name = "FoliageBottom";
            foliage1.transform.parent = treeGroup.transform;
            foliage1.transform.localPosition = new Vector3(0, 3.2f, 0);
            foliage1.transform.localScale = new Vector3(3.0f, 1.0f, 3.0f);
            Renderer fR1 = foliage1.GetComponent<Renderer>();
            if (fR1 != null) fR1.material.color = new Color(0.15f, 0.45f, 0.2f);

            // Foliage Cone 2
            GameObject foliage2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foliage2.name = "FoliageTop";
            foliage2.transform.parent = treeGroup.transform;
            foliage2.transform.localPosition = new Vector3(0, 4.4f, 0);
            foliage2.transform.localScale = new Vector3(2.0f, 1.0f, 2.0f);
            Renderer fR2 = foliage2.GetComponent<Renderer>();
            if (fR2 != null) fR2.material.color = new Color(0.18f, 0.52f, 0.25f);
        }
    }
}
