using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.WorldMap.HexTile;
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
using System.Diagnostics;
using Debug = UnityEngine.Debug;

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

            public Vector2Int GridSize;

            public HexSettings HexSettings;
            public Material MainMaterial;
            public Material InstanceMaterial;
            public Material Sprites_Default;


            public GridData(Vector2Int mapSize, int maxHexPerChunk, HexSettings hexSettings, Material mainMaterial, Material instanceMaterial, Material sprites_Default)
            {
                GridSize = mapSize;

                MaxHexPerChunk = maxHexPerChunk;

                HexSettings = hexSettings;

                MainMaterial = mainMaterial;
                InstanceMaterial = instanceMaterial;
                Sprites_Default = sprites_Default;
            }

            public void SetMapSize(Vector2Int size)
            {
                GridSize = size;
            }
        }

        public HexSettings HexSettings { get { return Data.HexSettings; } }

        private HexChunk hexChunkPrefab;
        private GameObject chunkParent;

        private List<HexChunk> hexChunks;
        private Dictionary<Vector2Int, HexTile> HexTiles;

        private Vector2Int GridSize { get { return Data.GridSize; } }

        /// <summary>
        /// The bounds of the map. Use this to check if a gridPosition is within the map. I recommend you use this with a box collider.
        /// </summary>
        public Bounds GridBounds
        {
            get
            {
                try
                {
                    Vector3 min = hexChunks[0].WorldBounds.min;
                    Vector3 max = hexChunks[hexChunks.Count - 1].WorldBounds.max;

                    return new Bounds((min + max) / 2, max - min);
                }
                catch (Exception)
                {

                    return new Bounds();
                }

            }
        }

        public Vector3 GridDimensions 
        {
            get
            {
                Vector3 min = hexChunks[0].WorldBounds.min;
                Vector3 max = hexChunks[hexChunks.Count - 1].WorldBounds.max;

                return max - min;
            }
        }

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
        public void GetBoxCollider(ref BoxCollider boxCollider)
        {
            Bounds bounds = GridBounds;

            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;
        }

        public bool PositionInGrid(Vector3 worldPosition)
        {
            worldPosition.y = 0;
            return GridBounds.Contains(worldPosition);
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

            HexTiles = HexTile.CreatesHexes(this, GridSize, ref hexChunks, data);

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
            if (ChunkSize2Use * ChunkSize2Use > GridSize.x * GridSize.y)
            {
                // Since all chunks will be squares, we use the smaller of the two map sizes
                ChunkSize2Use = Mathf.Max(GridSize.x, GridSize.y);
            }

            //ChunkSize2Use -= 1;

            ChunkCount.x = Mathf.CeilToInt((float)GridSize.x / ChunkSize2Use);
            ChunkCount.y = Mathf.CeilToInt((float)GridSize.y / ChunkSize2Use);
        }
        private void CreateHexChunks()
        {
            hexChunks.Clear();

            DestroyHexChunks();

            CalculateChunkSizes();

            HexChunk chunk;

            // this is so we dont have to keep creating new structs every time we create a chunk
            GridData dataCopy = Data;

            for (int z = 0; z < ChunkCount.y; z++)
            {
                for (int x = 0; x < ChunkCount.x; x++)
                {
                    bool inX = (x + 1) * ChunkSize2Use <= GridSize.x;
                    bool inZ = (z + 1) * ChunkSize2Use <= GridSize.y;

                    Vector3Int start = new Vector3Int();
                    Vector3Int size = new Vector3Int();

                    start.x = x * ChunkSize2Use;
                    start.y = 0;
                    start.z = z * ChunkSize2Use;

                    size.y = 0;

                    if (inX)
                    {
                        size.x = ChunkSize2Use;
                    }
                    else
                    {
                        size.x = GridSize.x - start.x;
                    }

                    if (inZ)
                    {
                        size.z = ChunkSize2Use;
                    }
                    else
                    {
                        size.z = GridSize.y - start.z;
                    }

                    BoundsInt bounds = new BoundsInt(start, size);

                    chunk = Instantiate(hexChunkPrefab, chunkParent.transform);
                    chunk.Initialize(this, ref dataCopy, bounds);
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
            SetAllChunksStatus(false);
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
            foreach (HexChunk chunk in hexChunks)
            {
                chunk.gameObject.SetActive(status);
            }
        }
        public void SetChunkStatusIfInBounds(Bounds bounds, bool status)
        {
            foreach (HexChunk chunk in hexChunks)
            {
                if (chunk.IsInsideBounds(bounds))
                {
                    chunk.gameObject.SetActive(status);
                }
            }
        }
        public void SetChunkStatusIfNotInBounds(Bounds bounds, bool status)
        {
            foreach (HexChunk chunk in hexChunks)
            {
                if (!chunk.IsInsideBounds(bounds))
                {
                    chunk.gameObject.SetActive(status);
                }
            }
        }

        /// <summary>
        /// Will set the chunk status to the status passed in if the chunk is inside the bounds, otherwise it will set the chunk status to the opposite of the status if the chunk is not inside the bounds
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="status"></param>
        public void SetChunkStatusIfInBoundsOtherwise(Bounds bounds, bool status)
        {
            foreach (HexChunk chunk in hexChunks)
            {
                if (chunk.IsInsideBounds(bounds))
                {
                    chunk.gameObject.SetActive(status);
                }
                else
                {
                    chunk.gameObject.SetActive(!status);
                }
            }
        }

        #endregion

        #region Get Hex Data

        private HexTile GetHexTile(Vector2Int position)
        {
            HexTile hex = null;
            HexTiles.TryGetValue(position, out hex);

            if (hex != null)
            {
                return hex;
            }

            new HexException(position, HexException.ErrorType.NotInGrid).LogMessage();

            return null;
        }

        /// <summary>
        /// This is the main function to get a hex tiles. Uses a dictionary, so it is super fast
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        private HexTile GetHexTile(Axial gridPosition)
        {
            Vector2Int vec = Axial.FromAxial(gridPosition);

            HexTile hex = null;
            HexTiles.TryGetValue(vec, out hex);

            if (hex != null)
            {
                return hex;
            }

            new HexException(vec, HexException.ErrorType.NotInGrid).LogMessage();

            return null;
        }
        private HexChunk GetHexChunk(Vector2Int gridPosition)
        {
            // formula is ((y - 1) * xc) + x)
            // y = gridPosition.y / chunkSize
            // xc = number of chunks in a width. Example, if map width is 100, and chunk width is 25, xc = 100/25 = 4
            // x = gridPosition.x  / chunkSize

            try
            {
                int x = Mathf.CeilToInt((float)gridPosition.x / ChunkSize2Use);
                int y = Mathf.CeilToInt((float)gridPosition.y / ChunkSize2Use);

                int index = (Mathf.Max(0, (y - 1)) * ChunkCount.x) + x;

                index -= 1;
                index = Math.Clamp(index, 0, ChunkCount.sqrMagnitude);
                
                return hexChunks[index];
            }
            catch (Exception)
            {
                string msg = ($"Chunk Not Found For Hex At Position {gridPosition}");

                Debug.Log(msg);
            }

            return null;
        }
        private HexChunk GetHexChunk(Vector3 localPosition)
        {
            // formula is ((z - 1) * xc) + x)
            // z = gridPosition.z / chunkSize
            // xc = number of chunks in a width. Example, if map width is 100, and chunk width is 25, xc = 100/25 = 4
            // x = gridPosition.x  / chunkSize

            string msg = ($"Chunk Not Found For Hex At Position {localPosition}");

            try
            {
                if (GridBounds.Contains(localPosition) == false)
                {
                    throw new Exception();
                }

                int x = Mathf.CeilToInt(localPosition.x / ChunkSize2Use);
                int y = Mathf.CeilToInt(localPosition.z / ChunkSize2Use);

                int index = (Mathf.Max(0, (y - 1)) * ChunkCount.x) + x;
                HexChunk chunk = hexChunks[index - 1];

                return hexChunks[index - 1];
            }
            catch (Exception)
            {
                Debug.Log(msg);
            }

            return null;
        }
        public HexData GetHexData(Vector2Int gridPosition)
        {
            HexTile hex = null;

            HexTiles.TryGetValue(gridPosition, out hex);

            if (hex != null)
            {
                HexChunk chunk = GetHexChunk(gridPosition);

                if (chunk != null)
                {
                    return new HexData(chunk, hex);
                }
            }

            new HexException(gridPosition, HexException.ErrorType.NotInGrid).LogMessage();

            return new HexData();
        }
        public HexData GetHexDataAtPosition(Vector3 worldPosition)
        {
            HexData data;

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            Vector2Int gridPosition = HexTile.GetGridCoordinate(localPosition, HexSettings);

            HexTile foundHex = GetHexTile(gridPosition);

            if (foundHex == null)
            {
                new HexException(worldPosition, HexException.ErrorType.NotInGrid).LogMessage();
                return new HexData();
            }

            HexChunk chunk = GetHexChunk(gridPosition);

            if (chunk == null)
            {
                string msg = $"Chunk Not Found at World Position {worldPosition}";
                Debug.Log(msg);
                return new HexData();
            }

            data = new HexData(chunk, foundHex);

            return data;

            new HexException(worldPosition, HexException.ErrorType.NotInGrid).LogMessage();

            return new HexData();
        }
        #endregion

        #region Set Hex Visual Data
        public HexVisualData GetVisualData(Vector2Int position)
        {
            HexTile hex = GetHexTile(position);
            return hex.VisualData;
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
        /// Note that the order is from gridPosition (0, 0) --> mapsize
        /// </summary>
        /// <param name="visualData"></param>
        public void SetVisualData(Vector2Int[] positions, HexVisualData[] visualData)
        {
            if (positions.Length != visualData.Length)
            {
                string msg = "Length of positions and visual data must be thesame";

                Debug.Log(msg);
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

        /// <summary>
        /// We use this struct to store the data of a hex. This way we dont have to find the chunk. Used for highlighting
        /// </summary>
        /// 
        public struct HexData
        {
            private HexChunk chunk;
            private HexTile hex;

            public Vector2Int GridCoordinates { get { return hex.GridCoordinates;  } }
            public int Hash { get { return GetHashCode(); } }

            static readonly Exception NotInChunk = new Exception("Hex is Not In Chunk");
            private void ThrowIfInvalid(HexChunk chunk, HexTile hex)
            {
                if (!chunk.IsInChunk(hex))
                {
                    throw new HexException(hex.GridCoordinates, HexException.ErrorType.NotInChunk);
                }

                this.chunk = chunk;
                this.hex = hex;
            }
            public HexData(HexChunk chunk, HexTile hex)
            {
                if (!chunk.IsInChunk(hex))
                {
                    throw new HexException(hex.GridCoordinates, HexException.ErrorType.NotInChunk);
                }

                this.chunk = chunk;
                this.hex = hex;
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
                }
            }
            public void Remove()
            {
                if (chunk != null && hex != null)
                {
                    chunk.RemoveHex(hex);
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