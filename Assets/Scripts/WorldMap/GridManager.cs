using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.WorldMap.Planet;
using Assets.Scripts.WorldMap;
using static Assets.Scripts.WorldMap.HexTile;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;
using UnityEditor;
using static Assets.Scripts.Miscellaneous.HexFunctions;
using Axial = Assets.Scripts.WorldMap.HexTile.Axial;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Assets.Scripts.WorldMap
{
    [System.Serializable]
    public class GridManager : MonoBehaviour
    {
        public HexSettings HexSettings;
        public GameObject hexParent;
        public GameObject hexChunkPrefab;
        
        private List<HexChunk> hexChunks;
        Dictionary<Axial, HexTile> HexTiles;

        public Material MainMaterial;
        public List<Texture2D> Texutures;

        public PlanetGenerator planetGenerator;
        Planet mainPlanet;

        private Vector2Int MapSize;
        public int ChunkSize;

        private void Awake()
        {  
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            HexChunk.MainMaterial = MainMaterial;
            HexTile.hexSettings = HexSettings;

            mainPlanet = planetGenerator.MainPlanet;
            MapSize = mainPlanet.PlanetSize;

            GenerateGridChunks();
        }
        
        Stopwatch timer = new Stopwatch();
        string formattedTime = "";
        TimeSpan elapsedTime;
        
        public void GenerateGridChunks()
        {
            timer.Start();
            HexTiles.Clear();

            planetGenerator.SetComputeSize();
            planetGenerator.ComputeBiomeNoise();

            HexTiles = HexTile.CreatesHexes(MapSize, this, planetGenerator);

            //Debug.Log("Creation Took : " + (timer.ElapsedMilliseconds / 1000f).ToString("0.00") + " seconds");

            HexTile.CreateSlopes(HexTiles);

            //Debug.Log("Slopes Elapsed : " + (timer.ElapsedMilliseconds / 1000f).ToString("0.00") + " seconds");
            
            UseChunks();

            timer.Stop();

            elapsedTime = timer.Elapsed;
            
            formattedTime = $"{elapsedTime.Minutes}m : {elapsedTime.Seconds} s : {elapsedTime.Milliseconds} ms";

            Debug.Log("Generation Took : " + formattedTime);

            timer.Reset();
        }
        private void CreateHexChunks()
        {
            hexChunks.Clear();

            // 6 for the base hex, 6 for each slope on each side of the hex
            
            int maxHexVertCount = 42;

            int maxVertCount = 65535;

            int maxHexCount = maxVertCount / maxHexVertCount;

            ChunkSize = (int)Mathf.Sqrt(maxHexCount);

            if (ChunkSize > MapSize.x || ChunkSize > MapSize.y)
            {
                // Since all chunks will be squares, we use the smaller of the two map sizes
                ChunkSize = Mathf.Min(MapSize.x, MapSize.y);
            }

            ChunkSize -= 1;

            int chunkCountX = Mathf.CeilToInt((float)MapSize.x / ChunkSize);
            int chunkCountZ = Mathf.CeilToInt((float)MapSize.y / ChunkSize);

            HexChunk chunk;

            for (int z = 0; z < chunkCountZ; z++)
            {
                for (int x = 0; x < chunkCountX; x++)
                {
                    bool inX = (x + 1) * ChunkSize <= MapSize.x;
                    bool inZ = (z + 1) * ChunkSize <= MapSize.y;

                    Vector3Int start = new Vector3Int();
                    Vector3Int size = new Vector3Int();

                    start.x = x * ChunkSize;
                    start.y = z * ChunkSize;

                    if (inX)
                    {
                        size.x = ChunkSize;
                    }
                    else
                    {
                        size.x = MapSize.x - start.x;
                    }

                    if (inZ)
                    {
                        size.y = ChunkSize;
                    }
                    else
                    {
                        size.y = MapSize.y - start.y;
                    }

                    BoundsInt bounds = new BoundsInt(start, size);

                    chunk = new HexChunk(bounds);
                    hexChunks.Add(chunk);
                }
            }
            
            //Debug.Log("Chunk Size: " + ChunkSize);
            //Debug.Log("Chunk count: " + hexChunks.Count);
        }
        public void UseChunks()
        {
            CreateHexChunks();

            AddHexToChunks();

            //Debug.Log("Adding To Chunk Elapsed : " + (timer.ElapsedMilliseconds / 1000f).ToString("0.00") + " seconds");


            foreach (HexChunk chunk in hexChunks)
            {
                chunk.CombinesMeshes();
            }

            GenerateHexObjects();
        }
        private void GenerateHexObjects()
        {
            // create the appropriate amount of chunks to cover the whole map
            DestroyChildren();
            
            foreach (HexChunk chunk in hexChunks)
            {
                GameObject newChunk = Instantiate(hexChunkPrefab, hexParent.transform);
                newChunk.GetComponent<MeshFilter>().mesh = chunk.mesh;
                newChunk.GetComponent<Renderer>().materials = chunk.materials.ToArray();
            }
        }
        private void AddHexToChunks()
        {
            Parallel.ForEach(HexTiles.Values, (hexTile) =>
            {
                hexChunks.First(h => h.IsInChunk(hexTile.X, hexTile.Y)).AddHex(hexTile); 
            });

        }
        public HexTile GetHexTile(Axial coordinates)
        {
            HexTile hex = null;
            HexTiles.TryGetValue(coordinates, out hex);

            return hex;
        }
        private void DestroyChildren()
        {
            int childCount = hexParent.transform.childCount;

            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = hexParent.transform.GetChild(i);
                DestroyImmediate(child.gameObject);
                // If you want to use DestroyImmediate instead, replace the line above with:
                // DestroyImmediate(child.gameObject);
            }
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
                exampleScript.GenerateGridChunks();
            }
        }
    }
#endif
    
}
