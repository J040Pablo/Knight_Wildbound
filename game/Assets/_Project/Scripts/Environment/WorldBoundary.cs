using UnityEngine;

namespace Roguelite.Environment
{
    public class WorldBoundary : MonoBehaviour
    {
        [Header("Boundary Dimensions")]
        [SerializeField] private Vector3 center = Vector3.zero;
        [SerializeField] private Vector3 size = new Vector3(50f, 30f, 50f);
        [SerializeField] private bool createPhysicalInvisibleWalls = true;

        public Vector3 Center => center;
        public Vector3 Size => size;

        private Bounds worldBounds;

        private void Awake()
        {
            UpdateBounds();
            if (createPhysicalInvisibleWalls)
            {
                CreatePerimeterWalls();
            }
        }

        public void SetupBoundary(Vector3 boundaryCenter, Vector3 boundarySize)
        {
            // Reset parent transform position to zero so center is explicit in world coordinates
            transform.position = Vector3.zero;
            center = boundaryCenter;
            size = boundarySize;
            UpdateBounds();

            if (createPhysicalInvisibleWalls)
            {
                CreatePerimeterWalls();
            }
        }

        public void UpdateBounds()
        {
            Vector3 worldCenter = transform.position + center;
            worldBounds = new Bounds(worldCenter, size);
        }

        public bool IsPositionInsideBoundary(Vector3 position, float margin = 0.5f)
        {
            UpdateBounds();

            Vector3 min = worldBounds.min + new Vector3(margin, -50f, margin);
            Vector3 max = worldBounds.max - new Vector3(margin, -50f, margin);

            return position.x >= min.x && position.x <= max.x &&
                   position.z >= min.z && position.z <= max.z &&
                   position.y >= (worldBounds.min.y - 15f);
        }

        private void CreatePerimeterWalls()
        {
            // Remove any old boundary walls under this object
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("InvisibleWall_"))
                {
                    Destroy(child.gameObject);
                }
            }

            float wallThickness = 2.0f;
            float wallHeight = size.y > 0 ? size.y : 30.0f;
            Vector3 c = center;

            // 1. North Wall (+Z)
            CreateWall("InvisibleWall_North", new Vector3(c.x, c.y, c.z + size.z / 2f + wallThickness / 2f), new Vector3(size.x + wallThickness * 2f, wallHeight, wallThickness));
            // 2. South Wall (-Z)
            CreateWall("InvisibleWall_South", new Vector3(c.x, c.y, c.z - size.z / 2f - wallThickness / 2f), new Vector3(size.x + wallThickness * 2f, wallHeight, wallThickness));
            // 3. East Wall (+X)
            CreateWall("InvisibleWall_East", new Vector3(c.x + size.x / 2f + wallThickness / 2f, c.y, c.z), new Vector3(wallThickness, wallHeight, size.z));
            // 4. West Wall (-X)
            CreateWall("InvisibleWall_West", new Vector3(c.x - size.x / 2f - wallThickness / 2f, c.y, c.z), new Vector3(wallThickness, wallHeight, size.z));
        }

        private GameObject CreateWall(string wallName, Vector3 wallLocalPos, Vector3 wallSize)
        {
            GameObject wall = new GameObject(wallName);
            wall.transform.parent = transform;
            wall.transform.localPosition = wallLocalPos;
            wall.layer = LayerMask.NameToLayer("Default");

            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = wallSize;
            collider.isTrigger = false; // Physical barrier preventing player exit

            return wall;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1.0f, 0.85f, 0.1f, 0.6f); // Bright YELLOW for world boundary
            Vector3 currentCenter = transform.position + center;
            Gizmos.DrawWireCube(currentCenter, size);
        }
    }
}
