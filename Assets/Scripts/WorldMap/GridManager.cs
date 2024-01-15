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
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using static Assets.Scripts.WorldMap.HexChunk;
using Unity.VisualScripting;
using Object = UnityEngine.Object;
using static Assets.Scripts.WorldMap.GridManager.ChunkComparer;
using UnityEngine.Tilemaps;

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
            [Tooltip("The Maximum amount of hexes allowed in a chunk. Set this to a reasonable number because everytime a chunk is modified, the whole thing has to be redrawn. Large chunk will cause significant lag upon modification.")]
            public int MaxHexPerChunk;

            public Vector2Int GridSize;

            public HexSettings HexSettings;
            public Material MainMaterial;
            public Material InstanceMaterial;
            public Material HighlightShader;


            public GridData(Vector2Int mapSize, int maxHexPerChunk, HexSettings hexSettings, Material mainMaterial, Material instanceMaterial, Material sprites_Default)
            {
                GridSize = mapSize;

                MaxHexPerChunk = maxHexPerChunk;

                HexSettings = hexSettings;

                MainMaterial = mainMaterial;
                InstanceMaterial = instanceMaterial;
                HighlightShader = sprites_Default;
            }

            public void SetMapSize(Vector2Int size)
            {
                GridSize = size;
            }
        }

        #region Map variables and properties
        public HexSettings HexSettings { get { return Data.HexSettings; } }

        private HexChunk hexChunkPrefab;
        private GameObject chunkParent;
        
        private Dictionary<Vector2Int, HexTile> HexTiles;

        ChunkComparer chunkComparer = new ChunkComparer();
        private SortedDictionary<Vector2Int, HexChunk> sortedChunks;

        private Vector2Int GridSize { get { return Data.GridSize; } }

        private Bounds _gridBounds;
        
        /// <summary>
        /// The bounds of the map. Use this to check if a gridPosition is within the map.
        /// The gridbounds start from (0,0) and end at map grid size
        /// </summary>
        public Bounds GridBounds
        {
            get
            {
                return _gridBounds;
            }
        }
        public Vector3 GridDimensions 
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public GridData Data { get; private set; }

        /// <summary>
        /// Set this to true if you want to log errors. This will log errors(print to console) such as trying to access a hex that doesn't exist.
        /// </summary>
        /// 

#if UNITY_EDITOR
        
        public readonly bool LogErrors = true;
#else
        public readonly bool LogErrors = false;
#endif

        #endregion

        #region Map Bounds/Position Checking
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
        private void SetGridBounds()
        {
            try
            {
                // There might exist the possiblility that a given chunk will empty and thus has been deleted. Because of that, we need to manually get the bounds of the chunks in order to get the bounds of the grid.
                
                BoundsInt bounds = new BoundsInt(Vector3Int.zero, ChunkSize);
                Bounds minWorldBounds = HexTile.GetWorldBounds(bounds, HexSettings);

                Vector3 min = minWorldBounds.min;


                Vector3Int start = new Vector3Int();
                
                // this will give us the starting gridPosition of the last chunk
                start.x = (GridChunkCount.x - 1) * ChunkHexCount;
                start.z = (GridChunkCount.y - 1) * ChunkHexCount;
                bounds = new BoundsInt(start, ChunkSize);
                
                Bounds maxWorldBounds = HexTile.GetWorldBounds(bounds, HexSettings);
                
                Vector3 max = maxWorldBounds.max;

                _gridBounds = new Bounds((min + max) / 2, max - min);
            }
            catch (Exception)
            {
                _gridBounds = new Bounds();
            }
        }
        public bool PositionIsInGridBounds(Vector3 worldPosition)
        {
            worldPosition.y = 0;
            Vector3 localPosition = transform.InverseTransformVector(worldPosition);

            return GridBounds.Contains(localPosition);
        }
        public bool PositionIsInMeshBounds(Vector3 worldPosition)
        {
            worldPosition.y = 0;

            Vector3 localPosition = transform.InverseTransformVector(worldPosition);

            HexChunk chunk = GetHexChunk(localPosition);

            if (chunk == null)
            {
                return false;
            }
            else
            {
                return chunk.MeshWorldBounds.Contains(localPosition);
            }
        }

        /// <summary>
        /// Use this to check if a worldPosition is over a drawn hex. This will return false if the worldPosition is over a hex whose mesh has not been drawn yet or if the hex doesnt exists at all
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public bool PositionHasDrawnHex(Vector3 worldPosition)
        {
            worldPosition.y = 0;

            Vector3 localPosition = transform.InverseTransformVector(worldPosition);

            HexChunk chunk = GetHexChunk(localPosition);

            if (chunk == null)
            {
                return false;
            }
            else
            {
                Vector2Int gridPosition = HexTile.GetGridCoordinate(localPosition, HexSettings);
                return chunk.HexIsDrawn(gridPosition);
            }
        }

        #endregion

        #region Map Generation Methods

        public void InitializeGrid(GridData gridData, List<HexVisualData> data = null)
        {
            ClearMap();

            Stopwatch timer = Stopwatch.StartNew();
            
            HexTiles = new Dictionary<Vector2Int, HexTile>();
            List<HexChunk> hexChunks = new List<HexChunk>();

            sortedChunks = new SortedDictionary<Vector2Int, HexChunk>(chunkComparer);

            this.Data = gridData;

            CreatePrefabs();
            CreateHexChunks();

            foreach (HexChunk item in sortedChunks.Values)
            {
                hexChunks.Add(item);
            }

            HexTiles = HexTile.CreatesHexes(this, GridSize, hexChunks, data);

            SetGridBounds();
            
            timer.Stop();

            LogTimer("Initialized Time: ", timer.ElapsedMilliseconds);

        }

        [NonSerialized] public float time;

        /// <summary>
        /// This will split all the hexes in the chunks into their specific biome groups, fuse their respective meshes together, then draw them. Use this to initially draw a chunk or if you have quick added hexes to a chunk and wish to draw them now
        /// </summary>
        /// <param name="drawAsync"></param>
        public void InitiateDrawProtocol(bool drawAsync = true)
        {
            #region Performance Stats
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
                foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
                {
                    item.Value.InitiateDrawProtocol();
                }
            }

            sw.Stop();
            time = sw.ElapsedMilliseconds;
            LogTimer("Generation Time: ", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// This will instantly draw the hexes that are already in their biome groups. Use this to redraw a chunks once you have modified it (via adding and removing hexes)
        /// </summary>
        /// <param name="drawAsync"></param>
        public void ImmediateDrawGrid(bool drawAsync = true)
        {
            if (drawAsync)
            {
                StartCoroutine(SpawnChunkEveryXSeconds(0));
            }
            else
            {
                foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
                {
                    item.Value.DrawFusedMeshes();
                }
            }
        }

        #endregion

        #region Chunk Size Variables
        // 6 for the base hex. Hex has 6 vertices, 1 for each corner.
        static readonly int maxHexVertCount = 6;

        // the max vert count of combined mesh. Unity side limit
        static readonly int maxVertCount = 65535;

        // the numbers of chunks on the x and y axis. So a 100 x 100 map, with 25 x 25 chunks will have a gridchunkcount of (4, 4)
        Vector2Int GridChunkCount = new Vector2Int();
        Vector3Int ChunkSize = new Vector3Int();
        int ChunkHexCount = -1;

        #endregion

        #region Map Spawning

        private void CalculateChunkSizes()
        {
            int maxHexCount = maxVertCount / maxHexVertCount;

            int maxHex = Data.MaxHexPerChunk;

            if (maxHexCount > maxHex && maxHex > 0)
            {
                maxHexCount = maxHex;
            }

            ChunkHexCount = (int)Mathf.Sqrt(maxHexCount);

            // if the map is small enough such that it can fit in our chunk size, we use the map size instead
            if (ChunkHexCount * ChunkHexCount > GridSize.x * GridSize.y)
            {
                // Since all chunks will be squares, we use the smaller of the two map sizes
                ChunkHexCount = Mathf.Max(GridSize.x, GridSize.y);
            }

            ChunkSize = new Vector3Int(ChunkHexCount, 0, ChunkHexCount);

            GridChunkCount.x = Mathf.CeilToInt((float)GridSize.x / ChunkHexCount);
            GridChunkCount.y = Mathf.CeilToInt((float)GridSize.y / ChunkHexCount);
        }
        private void UpdateGridChunks()
        {
            Vector2Int prevSize = GridChunkCount;

            // update this value
            GridChunkCount.x = Mathf.CeilToInt((float)GridSize.x / ChunkHexCount);
            GridChunkCount.y = Mathf.CeilToInt((float)GridSize.y / ChunkHexCount);

            // add and delete chunks
            HexChunk chunk;
            // this is so we dont have to keep creating new structs every time we create a chunk
            GridData dataCopy = Data;

            // when resizing bounds, we need to make sure for the given grid size, all the chunks are present in the tempChunks.
            // so we must StartPosition from gridPosition (0,0) and check if a chunk exists, if it doesnt create one
            for (int z = 0; z < GridChunkCount.y; z++)
            {
                for (int x = 0; x < GridChunkCount.x; x++)
                {
                    Vector2Int start = new Vector2Int();

                    start.x = x * ChunkHexCount;
                    start.y = z * ChunkHexCount;

                    sortedChunks.TryGetValue(start, out chunk);

                    if (chunk == null)
                    {
                        sortedChunks.Add(start, CreateHexChunk(start));
                    }
                    else
                    {
                        // we delete all the chunks that currently have no hexes in them.
                        // Note, a chunk can have a hex, but said hex is not drawn. In such a case, said chunk is not empty
                        if (chunk.IsEmpty)
                        {
                            DestroyChunk(chunk);
                        }
                    }
                }
            }

            // every time we add or delete chunks, we need to update bounds of the grid
            SetGridBounds();
        }
        private void CreateHexChunks()
        {          
            DestroyHexChunks();

            CalculateChunkSizes();

            // this is so we dont have to keep creating new structs every time we create a chunk
            GridData dataCopy = Data;

            for (int z = 0; z < GridChunkCount.y; z++)
            {
                for (int x = 0; x < GridChunkCount.x; x++)
                {
                    Vector2Int start = new Vector2Int();

                    start.x = x * ChunkHexCount;
                    start.y = z * ChunkHexCount;

                    HexChunk chunk = CreateHexChunk(start);

                    sortedChunks.Add(chunk.StartPosition, chunk);
                }
            }
        }

        public void ClearMap()
        {
            DestroyHexChunks();
        }

        private HexChunk CreateHexChunk(Vector3Int gridPosition)
        {
            Vector2Int start = new Vector2Int(gridPosition.x, gridPosition.z);

            return CreateHexChunk(start);  
        }

        /// <summary>
        /// Given a grid gridPosition, gets the start gridPosition of the chunk said grid will be in
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        private Vector2Int GetChunkStartPosition(Vector2Int gridPosition)
        {
            int x = Mathf.FloorToInt((float)gridPosition.x / ChunkHexCount);
            int y = Mathf.FloorToInt((float)gridPosition.y / ChunkHexCount);

            Vector2Int start = new Vector2Int();

            start.x = ChunkSize.x * x;
            start.y = ChunkSize.z * y;

            return start;
        }


        /// <summary>
        /// Will create the hex chunks that will contain said gridPosition. Make sure you have already called the CalculateChunkSizes method before calling this
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        private HexChunk CreateHexChunk(Vector2Int gridPosition)
        {
            HexChunk chunk;

            Vector3Int start = GetChunkStartPosition(gridPosition).ToBoundsGridPos();

            // the minimum bounds is (0,0)
            if (start.x < 0 || start.y < 0)
            {
                return null;
            }

            BoundsInt bounds = new BoundsInt(start, ChunkSize);

            chunk = Instantiate(hexChunkPrefab, chunkParent.transform);
            chunk.Initialize(this, Data, bounds);

            return chunk;
        }
        public void UpdateHexSettings()
        {
            Data.HexSettings.ResetVariables();
        }

        private IEnumerator SpawnChunkEveryXSeconds(float time)
        {
            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                item.Value.InitiateDrawProtocol();
                yield return new WaitForSeconds(time);
            }
        }

        private IEnumerator UpdateChunkEveryXSeconds(float time)
        {
            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                item.Value.DrawFusedMeshes();
                yield return new WaitForSeconds(time);
            }
        }

        /// <summary>
        /// Will send the mesh directly to the gpu to be drawn instead of using mesh renderers.
        /// This is a faster method, but it can only draw mesh using colors.
        /// This will temporarlity disable the chunk object, draw the instance, then re-enable the chunk object. This is drawn on a per frame basis, thus, you need to call this every frame you want to draw the chunks. The only reason to use this is because it much faster than using mesh renderers. Allowing you to make visual changes to map in real time. Will only draw chunks that are currently active
        /// </summary>
        public void DrawChunkInstanced()
        {
            //SetAllChunksStatus(false);
            
            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                if(item.Value.gameObject.activeSelf)
                {
                    item.Value.gameObject.SetActive(false);
                    item.Value.DrawInstanced();
                    item.Value.gameObject.SetActive(true);
                }
            }           
        }
        private void DestroyHexChunks()
        {
            if (sortedChunks == null)
            {
                return;
            }

            if(sortedChunks.Count == 0)
            {
                return;
            }
            
            // Create a copy of the dictionary keys to avoid modification errors during iteration
            List<Vector2Int> keysToDelete = new List<Vector2Int>(sortedChunks.Keys);

            // Iterate through the copied keys and delete corresponding chunks
            foreach (Vector2Int key in keysToDelete)
            {
                HexChunk chunkToDelete = sortedChunks[key];
                DestroyChunk(chunkToDelete);
                sortedChunks.Remove(key);
            }
        }

        #endregion

        #region Chunk Status Methods
        public void SetAllChunksStatus(bool status)
        {
            if (sortedChunks == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                item.Value.gameObject.SetActive(status);
            }
        }
        public void SetChunkStatusIfInBounds(Bounds bounds, bool status)
        {
            if (sortedChunks == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                if (item.Value.IsInsideBounds(bounds))
                {
                    item.Value.gameObject.SetActive(status);
                }
            }
        }
        public void SetChunkStatusIfNotInBounds(Bounds bounds, bool status)
        {
            if (sortedChunks == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                if (!item.Value.IsInsideBounds(bounds))
                {
                    item.Value.gameObject.SetActive(status);
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
            if (sortedChunks == null)
            {
                return;
            }

            foreach (KeyValuePair<Vector2Int, HexChunk> item in sortedChunks)
            {
                if (item.Value.IsInsideBounds(bounds))
                {
                    item.Value.gameObject.SetActive(status);
                }
                else
                {
                    item.Value.gameObject.SetActive(!status);
                }
            }
        }

        #endregion

        #region Get Hex Data

        private HexTile GetHexTile(Vector2Int gridPosition)
        {
            HexTile hex = null;
            HexTiles.TryGetValue(gridPosition, out hex);

            if (hex != null)
            {
                return hex;
            }

            if(LogErrors)
            {
                new HexException(gridPosition, HexException.ErrorType.NotInGrid).LogMessage();
            }

            return null;
        }

        private HexTile GetHexTile(Vector3 localPosition)
        {
            Vector2Int gridPosition = HexTile.GetGridCoordinate(localPosition, HexSettings);

            return GetHexTile(gridPosition);
        }

        private HexChunk GetHexChunk(Vector2Int gridPosition)
        {
            Vector2Int startPosition = GetChunkStartPosition(gridPosition);

            HexChunk chunk = null;

            sortedChunks.TryGetValue(startPosition, out chunk);

            return chunk;
        }

        public HexChunk GetHexChunk(Vector3 localPosition)
        {
            Vector2Int startPosition = HexTile.GetGridCoordinate(localPosition, HexSettings);

            return GetHexChunk(startPosition);
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
            
            if (LogErrors)
            {
                new HexException(gridPosition, HexException.ErrorType.NotInGrid).LogMessage();
            }

            return new HexData();
        }
        public HexData GetHexData(Vector3 worldPosition)
        {
            HexData data;

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            Vector2Int gridPosition = HexTile.GetGridCoordinate(localPosition, HexSettings);

            HexTile foundHex = GetHexTile(gridPosition);

            if (foundHex == null)
            {
                if (LogErrors)
                {
                    new HexException(worldPosition, HexException.ErrorType.NotInGrid)
                        .LogMessage();
                }
                
                return new HexData();
            }

            HexChunk chunk = GetHexChunk(gridPosition);

            if (chunk == null)
            {
                if (LogErrors)
                {
                    new HexException(worldPosition, HexException.ErrorType.NotInChunk)
                        .LogMessage();
                }

                return new HexData();
            }

            data = new HexData(chunk, foundHex);
            
            return data;
        }
        public Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            HexTile hex = GetHexTile(gridPosition);

            if (hex != null)
            {
                return transform.TransformDirection(hex.LocalPosition);
            }

            if (LogErrors)
            {
                new HexException(gridPosition, HexException.ErrorType.NotInGrid).LogMessage();
            }

            return Vector3.zero;
        }

        public Vector3 GetLocalPosition(Vector2Int gridPosition)
        {
            HexTile hex = GetHexTile(gridPosition);

            if (hex != null)
            {
                return hex.LocalPosition;
            }

            if (LogErrors)
            {
                new HexException(gridPosition, HexException.ErrorType.NotInGrid).LogMessage();
            }

            return Vector3.zero;
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

                return;
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

        #region Map Modifications

        private void UpdateGridSize()
        {
            int maxX = HexTiles.Values.Max(x => x.X);
            int maxY = HexTiles.Values.Max(y => y.Y);

            GridData data = Data;

            data.GridSize.x = maxX;
            data.GridSize.y = maxY;

            Data = data;

            // everytime the gridSize is updated, we must then calculate the new chunksizes
            CalculateChunkSizes();

            // every time we add or delete chunks, we need to update bounds of the grid
            SetGridBounds();
        }
        public void AddHex(Vector2Int gridPosition, HexVisualData visual, bool draw = false)
        {
            if (HexTiles.ContainsKey(gridPosition))
            {
                if (LogErrors)
                {
                    new HexException(gridPosition, HexException.ErrorType.AlreadyInGrid).LogMessage();
                }

                return;
            }

            HexTile hex = new HexTile(gridPosition.x, gridPosition.y, HexSettings);

            hex.VisualData = visual;

            HexTiles.Add(gridPosition, hex);

            UpdateGridSize();

            // we see if there is already a chunk that the gridPosition can fit in
            HexChunk chunk = GetHexChunk(gridPosition);

            if(chunk == null)
            {
                // if there isnt already a chunk we create a new one
                chunk = CreateHexChunk(gridPosition);
                sortedChunks.Add(chunk.StartPosition, chunk);
            }

            chunk.AddHex(hex, draw);
        }
        public void RemoveHex(Vector2Int gridPosition, bool draw = false)
        {
            HexTile hex = GetHexTile(gridPosition);

            if (hex == null)
            {
                if (LogErrors)
                {
                    new HexException(gridPosition, 
                        HexException.ErrorType.NotInGrid).LogMessage();
                }
                
                return;
            }

            HexChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveHex(hex, draw);
                HexTiles.Remove(gridPosition);

                UpdateGridSize();

                if(chunk.IsEmpty)
                {
                    sortedChunks.Remove(chunk.StartPosition);
                    Destroy(chunk.gameObject);
                }
            }
        }

        public void AddChunk(Vector3 worldPosition, bool draw = false)
        {
            Vector3 localPosition = transform.InverseTransformVector(worldPosition);
            Vector2Int gridPos = HexTile.GetGridCoordinate(localPosition, HexSettings);

            AddChunk(gridPos, draw);
        }
        public void AddChunk(Vector2Int gridPosition, bool draw = false)
        {
            HexChunk chunk = GetHexChunk(gridPosition);

            if(chunk == null)
            {
                chunk = CreateHexChunk(gridPosition);

                if(chunk == null)
                {
                    Debug.LogError($"Cant add chunk");
                    return;
                }

                sortedChunks.Add(chunk.StartPosition, chunk);

                if(draw)
                {
                    chunk.AddAllHexes(GridSize, true);
                }
            }
            else
            {
                Debug.LogError($"Cant add chunk");
            }
        }

        public void RemoveChunk(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformVector(worldPosition);

            HexChunk chunk = GetHexChunk(localPosition);

            DestroyChunk(chunk);
        }
        public void RemoveChunk(Vector2Int gridPositionm)
        {
            HexChunk chunk = GetHexChunk(gridPositionm);

            DestroyChunk(chunk);
        }

        private void DestroyChunk(HexChunk chunk, bool destroyImmediate = false)
        {
            if (chunk != null)
            {
                chunk.RemoveChunkHexesFromExternalList(HexTiles);
                sortedChunks.Remove(chunk.StartPosition);
                
                if(destroyImmediate)
                {
                    DestroyImmediate(chunk.gameObject);
                }
                else
                {
                    if (Application.isPlaying)
                    {
                        Destroy(chunk.gameObject); 
                    }
                    else
                    {
                        DestroyImmediate(chunk.gameObject);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// We use this struct to store the chunk of a hex and the hex. It is SLIGHTLY faster when doing some hex operations since we do not need to find the chunk a hex is located in
        /// </summary>
        /// 
        public struct HexData
        {
            private HexChunk chunk;
            private HexTile hex;

            public Vector2Int GridCoordinates { get { return hex.GridPosition;  } }
            
            public Vector3 LocalPosition { get { return hex.LocalPosition;  } }

            public Vector3 WorldPosition { get {
                            return chunk.gameObject.transform.position + hex.LocalPosition; } }

            public HexVisualData GetHexVisualData { get { return hex.VisualData;  } }
            public int Hash { get { return GetHashCode(); } }
            public HexData(HexChunk chunk, HexTile hex)
            {
                if (!chunk.IsInChunk(hex))
                {
                    throw new HexException(hex.GridPosition, HexException.ErrorType.NotInChunk);
                }

                this.chunk = chunk;
                this.hex = hex;
            }
            public void UpdateVisualData()
            {
                chunk.UpdateVisualData(hex);
            }
            public void Highlight(Color color)
            {
                if (chunk != null && hex != null)
                {
                    chunk.HighlightHex(hex, color);
                }
            }
            public void UnHighlight()
            {
                if (chunk != null && hex != null)
                {
                    chunk.UnHighlightHex(hex);
                }
            }
            public void ActivateBorder(int side, Color color)
            {
                if (!IsNullOrEmpty())
                {
                    chunk.ActivateHexBorder(hex, side, color);
                }
            }

            public void Test()
            {
                if (!IsNullOrEmpty())
                {
                    chunk.test();
                }
            }

            public void AddLayer(int layer)
            {
                if (!IsNullOrEmpty())
                {
                    chunk.AddMeshLayer("test", layer);
                }
            }
            public void ActivateAllBorders(Color[] colors)
            {
                if (!IsNullOrEmpty())
                {
                    chunk.ActivateAllHexBorders(hex, colors);
                }
            }

            public void ActivateAllBorders(Color color)
            {
                if (!IsNullOrEmpty())
                {
                    chunk.ActivateAllHexBorders(hex, color);
                }
            }

            public void DeactivateBorder(int side)
            {
                if (!IsNullOrEmpty())
                {
                    chunk.DeactivateHexBorder(hex, side);
                }
            }
            public void DeactivateAllBorders()
            {
                if (!IsNullOrEmpty())
                {
                    chunk.DeactivateAllHexBorders(hex);
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

        public class ChunkComparer : IComparer<Vector2Int>
        {
            /// <summary>
            /// Compares two chunks start positions. Used for sorting the chunks.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"></exception>
            /// <exception cref="ArgumentException"></exception>
            public int Compare(Vector2Int chunk1, Vector2Int chunk2)
            {
                // First, compare by the Y-coordinate in descending order (bottom to top)
                int compareY = chunk1.y.CompareTo(chunk2.y);

                if (compareY != 0)
                {
                    return compareY;
                }

                // If Y-coordinates are the same, compare by X-coordinate in ascending order (left to right)
                int com = chunk1.x.CompareTo(chunk2.x);

                return com;
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
                    exampleScript.ImmediateDrawGrid();
                }
            }
        }
#endif

    }
}