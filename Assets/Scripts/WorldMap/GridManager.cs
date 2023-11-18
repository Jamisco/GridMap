using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.WorldMap.HexTile;
using System.Diagnostics;
using System;
using Axial = Assets.Scripts.WorldMap.HexTile.Axial;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using System.Collections;
using Assets.Scripts.WorldMap.Biosphere;
using Unity.VisualScripting;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Namespace.
namespace Assets.Scripts.WorldMap
{
    [System.Serializable]
    public class GridManager : MonoBehaviour
    {
        [Serializable]
        public struct GridData
        {
            [Tooltip("The Maximum amount of hexes allowed in a chunk. Set this reasonable number because everytime a chunk is modified, the whole thing has to be redrawn. Large chunk will cause significant lag upon modification.")]
            public int MaxHexPerChunk;

            public Vector2Int MapSize;

            public HexSettings HexSettings;
            public Material MainMaterial;
            public Material InstanceMaterial;


            public GridData(Vector2Int mapSize, int maxHexPerChunk, HexSettings hexSettings, Material mainMaterial, Material instanceMaterial)
            {
                MapSize = mapSize;

                MaxHexPerChunk = maxHexPerChunk;

                HexSettings = hexSettings;

                MainMaterial = mainMaterial;
                InstanceMaterial = instanceMaterial;
            }

            public void SetMapSize(Vector2Int size)
            {
                MapSize = size;
            }
        }

        public HexSettings HexSettings { get { return Data.HexSettings; } }

        private HexChunk hexChunkPrefab;
        private GameObject chunkParent;

        private List<HexChunk> hexChunks;
        private Dictionary<Vector2Int, HexTile> HexTiles;

        private Vector2Int MapSize { get { return Data.MapSize; } }
        private Bounds MapBounds;

        public GridData Data { get; private set; }

        public void SetGridData(GridData data)
        {
            this.Data = data;
        }
        private void CreatePrefabs()
        {
            if (chunkParent == null)
            {
                chunkParent = new GameObject("Hex Chunks");
                chunkParent.transform.SetParent(transform);
            }

            if (hexChunkPrefab == null)
            {
                GameObject chunkObj = new GameObject("Chunk Prefab");

                hexChunkPrefab = chunkObj.AddComponent<HexChunk>();
                hexChunkPrefab.transform.SetParent(transform);

                hexChunkPrefab.GetComponent<MeshRenderer>().material = Data.MainMaterial;
            }
        }
        private void SetBounds()
        {
            Vector3 center
                = HexTile.GetPosition(MapSize.x / 2, 0, MapSize.y / 2, Data.HexSettings);

            Vector3 start = GetPosition(Vector3Int.zero, Data.HexSettings);
            Vector3 end = GetPosition(MapSize.x, 0,
                MapSize.y, Data.HexSettings);

            MapBounds = new Bounds(center, end - start);
        }

        #region Hex Generation Methods

        public void InitializeGrid(GridData gridData, List<HexVisualData> data = null)
        {
            Stopwatch timer = Stopwatch.StartNew();
            
            HexTiles = new Dictionary<Vector2Int, HexTile>();
            hexChunks = new List<HexChunk>();

            this.Data = gridData;

            CreatePrefabs();
            CreateHexChunks();

            HexTiles = HexTile.CreatesHexes(this, MapSize, ref hexChunks, data);
            SetBounds();

            timer.Stop();

            LogTimer("Initialized Time: ", timer.ElapsedMilliseconds);
        }

        public void GenerateGrid(bool spawnAsync = true)
        {
            DrawGridChunks(spawnAsync);
        }

        // 6 for the base hex, 6 for each slope on each side of the hex
        static readonly int maxHexVertCount = 6;

        // the max vert count of combined mesh. Unity side limit
        static readonly int maxVertCount = 65535;

        Vector2Int ChunkCount = new Vector2Int();
        int ChunkSize2Use = -1;

        private void CalculateChunkSizes()
        {
            int maxHexCount = maxVertCount / maxHexVertCount;

            int maxHex = Data.MaxHexPerChunk;

            if (maxHexCount > maxHex && maxHex > 0)
            {
                maxHexCount = maxHex;
            }

            ChunkSize2Use = (int)Mathf.Sqrt(maxHexCount);

            // if the map is small enough such that it can fit in our chunk size, we use the map size instead
            if (ChunkSize2Use * ChunkSize2Use > MapSize.x * MapSize.y)
            {
                // Since all chunks will be squares, we use the smaller of the two map sizes
                ChunkSize2Use = Mathf.Max(MapSize.x, MapSize.y);
            }

            //ChunkSize2Use -= 1;

            ChunkCount.x = Mathf.CeilToInt((float)MapSize.x / ChunkSize2Use);
            ChunkCount.y = Mathf.CeilToInt((float)MapSize.y / ChunkSize2Use);
        }
        private void CreateHexChunks()
        {
            hexChunks.Clear();

            DestroyHexChunks();

            CalculateChunkSizes();

            HexChunk chunk;

            for (int z = 0; z < ChunkCount.y; z++)
            {
                for (int x = 0; x < ChunkCount.x; x++)
                {
                    bool inX = (x + 1) * ChunkSize2Use <= MapSize.x;
                    bool inZ = (z + 1) * ChunkSize2Use <= MapSize.y;

                    Vector3Int start = new Vector3Int();
                    Vector3Int size = new Vector3Int();

                    start.x = x * ChunkSize2Use;
                    start.y = z * ChunkSize2Use;

                    if (inX)
                    {
                        size.x = ChunkSize2Use;
                    }
                    else
                    {
                        size.x = MapSize.x - start.x;
                    }

                    if (inZ)
                    {
                        size.y = ChunkSize2Use;
                    }
                    else
                    {
                        size.y = MapSize.y - start.y;
                    }

                    BoundsInt bounds = new BoundsInt(start, size);

                    chunk = Instantiate(hexChunkPrefab, chunkParent.transform);
                    chunk.Initialize(this, Data, bounds);
                    hexChunks.Add(chunk);
                }
            }
        }
        public void UpdateHexSettings()
        {
            Data.HexSettings.ResetVariables();
        }

        [NonSerialized] public float time;
        private void DrawGridChunks(bool drawAsync = true)
        {
            #region Time Stats
            // Time starts for 300 x 300

            //Computer Biome : .044-- 1.2 %
            //Create Chunks: .05-- 1.4 %
            //Create Hexes: 1.257-- 36.4 %

            //Splitting : .355 - 10.3 %

            //Fusing : 2.029 - 58.87 %

            //Draw Mesh: 2.406 - 69.82 %

            //Generation : 3.446

            // Time stats for 1000 x 1000

            // Compute Biome: .457
            // Create Chunks: .516
            // Create Hexes: 14.943

            // be advised this time is when we drew the chunks using the coroutine
            // What ultimately matters is the time it takes to draw the chunks, which can be circumvented by using a coroutine to draw them bit by bit, or else the game will freeze for a while depending on the size of the map

            // Generation Time: 14.976

            #endregion

            time = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (drawAsync)
            {
                StartCoroutine(SpawnChunkEveryXSeconds(0));
            }
            else
            {
                for (int i = 0; i < hexChunks.Count; i++)
                {
                    hexChunks[i].InitiateDrawProtocol();
                }
            }

            sw.Stop();
            time = sw.ElapsedMilliseconds;
            LogTimer("Generation Time: ", sw.ElapsedMilliseconds);
        }

        private IEnumerator SpawnChunkEveryXSeconds(float time)
        {
            for (int i = 0; i < hexChunks.Count; i++)
            {
                hexChunks[i].InitiateDrawProtocol();
                yield return new WaitForSeconds(time);
            }
        }
        public void DrawChunkInstanced()
        {
            // 100 x100 10 fps
            foreach (HexChunk chunk in hexChunks)
            {
                chunk.DrawInstanced();
            }
        }
        private void DestroyHexChunks()
        {
            int childCount = chunkParent.transform.childCount;

            for (int i = childCount - 1; i > 0; i--)
            {
                Transform child = chunkParent.transform.GetChild(i);
                DestroyImmediate(child.gameObject);
                // If you want to use DestroyImmediate instead, replace the line above with:
                // DestroyImmediate(child.gameObject);
            }
        }

        #endregion

        #region Chunk Status Methods
        public void SetAllChunksStatus(bool status)
        {
            int childCount = chunkParent.transform.childCount;

            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = chunkParent.transform.GetChild(i);
                child.gameObject.SetActive(status);
            }
        }
        public void SetSpecificChunkStatus(HexTile hex, bool status)
        {
            hexChunks.First(h => h.IsInChunk(hex)).gameObject.SetActive(status);
        }
        public void SetSpecificChunkStatus(HexChunk chunk, bool status)
        {
            chunk.gameObject.SetActive(status);
        }
        public void SetChunkStatusIfInBounds(Bounds bounds, bool status)
        {
            foreach (HexChunk chunk in hexChunks)
            {
                if (chunk.IsIntersected(bounds))
                {
                    chunk.gameObject.SetActive(status);
                }
            }
        }
        public void SetChunkStatusIfNotInBounds(Bounds bounds, bool status)
        {
            foreach (HexChunk chunk in hexChunks)
            {
                if (!chunk.IsIntersected(bounds))
                {
                    chunk.gameObject.SetActive(status);
                }
            }
        }

        #endregion

        #region Get Hex Data

        /// <summary>
        /// This is slower because it will loop through all the hexes
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        ///     
        private HexTile GetHexTile(Vector2Int position)
        {
            HexTile hex = null;
            HexTiles.TryGetValue(position, out hex);

            if (hex != null)
            {
                return hex;
            }

            throw HexNotFoundException;
        }
        /// <summary>
        /// This is the main function to get a hex tiles. Uses a dictionary, so it is super fast
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private HexTile GetHexTile(Axial position)
        {
            Vector2Int vec = Axial.FromAxial(position);

            HexTile hex = null;
            HexTiles.TryGetValue(vec, out hex);

            if (hex != null)
            {
                return hex;
            }

            throw HexNotFoundException;
        }
        private HexChunk GetHexChunk(Vector2Int position)
        {
            // formula is ((y - 1) * xc) + x)
            // y = position.y / chunkSize
            // xc = number of chunks in a width. Example, if map width is 100, and chunk width is 25, xc = 100/25 = 4
            // x = position.x  / chunkSize

            try
            {
                int x = Mathf.CeilToInt(position.x / ChunkSize2Use);
                int y = Mathf.CeilToInt(position.y / ChunkSize2Use);

                int index = (Mathf.Max(0, (y - 1)) * ChunkCount.x) + x;
                HexChunk chunk = hexChunks[index - 1];

                return hexChunks[index - 1];
            }
            catch (Exception)
            {
                throw new Exception("Chunk Not Found Exception");
            }
        }
        public HexData GetHexData(Vector2Int position)
        {
            HexTile hex = null;

            HexTiles.TryGetValue(position, out hex);

            if (hex != null)
            {
                HexChunk chunk = GetHexChunk(position);

                if (chunk != null)
                {
                    return new HexData(chunk, hex);
                }
            }

            return new HexData();
        }
        private HexData GetHexDataAtPosition(Vector3 position)
        {
            HexData data;

            Ray ray = Camera.main.ScreenPointToRay(position);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000))
            {
                HexChunk chunk = hit.collider.GetComponentInParent<HexChunk>();

                if (chunk == null)
                {
                    throw HexNotFoundException;
                }

                // we subtract the position of the chunk because the hexes are positioned relative to the chunk, so if a chunk is at 0,10 and the hex is at 0,0, the hex is actually at 0,10,0 in world position

                HexTile foundHex = chunk.GetClosestHex(hit.point - transform.position);

                data = new HexData(chunk, foundHex);

                return data;
            }

            throw HexNotFoundException;
        }
        private HexData GetHexDataAtMousePosition()
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            return GetHexDataAtPosition(mousePosition);
        }
        #endregion

        #region Set Hex Visual Data
        public void GetVisualData(Vector2Int position, out HexVisualData data)
        {
            HexTile hex = GetHexTile(position);
            data = hex.VisualData;
        }
        public void SetVisualData(Vector2Int position, HexVisualData data)
        {
            HexTile hex = GetHexTile(position);

            if (hex != null)
            {
                hex.VisualData = data;
            }
        }
        public void UpdateVisualData(Vector2Int position)
        {
            HexData data = GetHexData(position);

            data.UpdateVisualData();
        }

        /// <summary>
        /// Note that the order is from position (0, 0) --> mapsize
        /// </summary>
        /// <param name="visualData"></param>
        public void SetVisualData(Vector2Int[] positions, HexVisualData[] visualData)
        {
            if (positions.Length != visualData.Length)
            {
                Exception size = new Exception("Length of positions and visual data must be thesame");

                throw size;
            }

            Parallel.For(0, visualData.Length, i =>
            {
                HexTile hex = null;

                HexTiles.TryGetValue(positions[i], out hex);

                if (hex != null)
                {
                    hex.VisualData = visualData[i];
                }
            });
        }

        #endregion



        #region Test Method delete when done

        Dictionary<int, HexData> HighlightedHexes = new Dictionary<int, HexData>();
        Dictionary<int, HexData> ActivatedBorderHexes = new Dictionary<int, HexData>();
        private void OnMouseClick()
        {
            HexData newData;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                newData = GetHexDataAtMousePosition();

                if (newData.IsNullOrEmpty())
                {
                    return;
                }

                HighlightHex(newData);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                newData = GetHexDataAtMousePosition();

                if (newData.IsNullOrEmpty())
                {
                    return;
                }

                UnHighlightHex(newData);
            }
        }

        HexData previousBorder = new HexData();
        private void HighlightOnHover()
        {
            HexData newData = GetHexDataAtMousePosition();

            if (newData.IsNullOrEmpty() || newData == previousBorder)
            {
                return;
            }

            //Debug.Log("Hex at: " + newData.hex.GridCoordinates);

            if (!ActivatedBorderHexes.ContainsKey(newData.Hash))
            {
                ActivatedBorderHexes.Remove(previousBorder.Hash);

                if (!previousBorder.IsNullOrEmpty())
                {
                    previousBorder.DeactivateBorder();
                }

                ActivatedBorderHexes.Add(newData.Hash, newData);
                newData.ActivateBorder();
                previousBorder = newData;
            }
        }

        public void HighlightHex(HexData hex)
        {
            if (hex.IsNullOrEmpty())
            {
                return;
            }

            if (!HighlightedHexes.ContainsKey(hex.Hash))
            {
                HighlightedHexes.Add(hex.Hash, hex);
                // hex.Highlight();


                hex.ChangeColor(Color.black);
            }
        }
        public void UnHighlightHex(HexData hex)
        {
            if (hex.IsNullOrEmpty())
            {
                return;
            }

            if (HighlightedHexes.ContainsKey(hex.Hash))
            {
                HighlightedHexes.Remove(hex.Hash);
                // hex.UnHighlight();
                //hex.UnHighlight();

                Color ogColor = Color.red;

                hex.ChangeColor(ogColor);
            }
        }

        #endregion

        /// <summary>
        /// We use this struct to store the data of a hex. This way we dont have to find the chunk. Used for highlighting
        /// </summary>
        /// 
        public struct HexData
        {
            private HexChunk chunk;
            private HexTile hex;
            public int Hash { get { return GetHashCode(); } }

            static readonly Exception notInChunk = new Exception("Hex is Not In Chunk");
            private void ThrowIfInvalid(HexChunk chunk, HexTile hex)
            {
                if (!chunk.IsInChunk(hex))
                {
                    throw notInChunk;
                }

                this.chunk = chunk;
                this.hex = hex;
            }
            public HexData(HexChunk chunk, HexTile aHex)
            {
                if (chunk)
                    if (!chunk.IsInChunk(aHex))
                    {
                        throw notInChunk;
                    }

                this.chunk = chunk;
                this.hex = aHex;
            }

            public void SetData(HexChunk chunk, HexTile hex)
            {
                ThrowIfInvalid(chunk, hex);
            }

            public void UpdateVisualData()
            {
                chunk.UpdateVisualData(hex);
            }

            public void Highlight()
            {
                if (chunk != null && hex != null)
                {
                    chunk.HighlightHex(hex);
                }
            }
            public void UnHighlight()
            {
                if (chunk != null && hex != null)
                {
                    chunk.UnHighlightHex(hex);
                    ResetData();
                }
            }
            public void ActivateBorder()
            {
                if (!IsNullOrEmpty())
                {
                    chunk.ActivateHexBorder(hex);
                }
            }
            public void DeactivateBorder()
            {
                if (!IsNullOrEmpty())
                {
                    chunk.DeactivateHexBorder(hex);
                    ResetData();
                }
            }
            public void Remove()
            {
                if (chunk != null && hex != null)
                {
                    chunk.RemoveHex(hex);
                    ResetData();
                }
            }

            public void ChangeColor(Color color)
            {
                if ((!IsNullOrEmpty()))
                {
                    chunk.ChangeColor(hex, color);
                }
            }
            public void ResetData()
            {
                chunk = null;
                hex = null;
            }
            public static bool operator ==(HexData hex1, HexData hex2)
            {
                return hex1.Equals(hex2);
            }
            public static bool operator !=(HexData hex1, HexData hex2)
            {
                return !hex1.Equals(hex2);
            }
            bool Equals(HexData other)
            {
                if (chunk == other.chunk)
                {
                    if (hex == other.hex)
                    {
                        return true;
                    }
                }

                return false;
            }
            public override bool Equals(object obj)
            {
                if (obj != null)
                {
                    HexData hex;

                    try
                    {
                        hex = (HexData)obj;
                    }
                    catch (Exception)
                    {
                        return false;
                    }

                    return Equals(hex);
                }

                return false;
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(hex, chunk);
            }

            public bool IsNullOrEmpty()
            {
                if (chunk == null && hex == null)
                {
                    return true;
                }

                return false;
            }
        }


#if UNITY_EDITOR
        [CustomEditor(typeof(GridManager))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                GridManager exampleScript = (GridManager)target;

                if (GUILayout.Button("Generate Grid"))
                {
                    exampleScript.GenerateGrid();
                }
            }
        }
#endif

    }
}