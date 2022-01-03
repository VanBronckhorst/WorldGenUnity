using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    const float viewerMoveThresholdForUpdate = 25f;
    const float sqrViewerMoveThresholdForUpdate = viewerMoveThresholdForUpdate * viewerMoveThresholdForUpdate;


    public LODInfo[] levelsOfDetail;
    public static float maxViewDist;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 oldViewerPosition;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkdict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleLastFrame = new List<TerrainChunk>();

    private void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist = levelsOfDetail[levelsOfDetail.Length - 1].visibleDstThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        if ((oldViewerPosition - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForUpdate) {
            oldViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks() {

        for (int i = 0; i < visibleLastFrame.Count; i++) {
            visibleLastFrame[i].SetVisible(false);
        }
        visibleLastFrame.Clear();

        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (terrainChunkdict.ContainsKey(viewedChunkCoord)) {
                    terrainChunkdict[viewedChunkCoord].UpdateTerrainChunk();
                } else {
                    terrainChunkdict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, levelsOfDetail, transform, mapMaterial));
                }
            }
        }

    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 pos;

        Bounds bounds;

        MapData mapData;
        bool mapDatareceived;
        int previousLODIndex = -1;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;
        LODMesh collisionMesh;

        LODInfo[] levels;
        LODMesh[] lodMeshes;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] levels, Transform parent, Material material) {
            this.levels = levels;
            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posV3 = new Vector3(pos.x, 0, pos.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();


            meshObject.transform.position = posV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
            SetVisible(false);

            lodMeshes = new LODMesh[levels.Length];
            for (int i = 0; i < levels.Length; i++) {
                lodMeshes[i] = new LODMesh(levels[i].lod, UpdateTerrainChunk);
                if (levels[i].useForCollider) {
                    collisionMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(pos, onMapDataReceived);
        }

        public void UpdateTerrainChunk() {
            if (!mapDatareceived) {
                return;
            }

            float viewerDist = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDist <= maxViewDist;

            if (visible) {
                int lodIndex = 0;

                for (int i = 0; i < levels.Length - 1; i++) {
                    if (viewerDist > levels[i].visibleDstThreshold) {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex) {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh) {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    } else if (!lodMesh.hasRequested) {
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if (lodIndex == 0) {
                    if (collisionMesh.hasMesh) {
                        meshCollider.sharedMesh = collisionMesh.mesh;
                    } else if (!collisionMesh.hasRequested) {
                        collisionMesh.RequestMesh(mapData);
                    }
                }

                visibleLastFrame.Add(this);
            }
            SetVisible(visible);
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool isVisible() {
            return meshObject.activeSelf;
        }

        void onMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            this.mapDatareceived = true;

            UpdateTerrainChunk();
        }
    }

    class LODMesh {
        public Mesh mesh;
        public bool hasRequested;
        public bool hasMesh;
        public int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }
        void onMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }
        public void RequestMesh(MapData mapData) {
            hasRequested = true;
            mapGenerator.RequestMeshData(mapData, lod, onMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;
    }
}


