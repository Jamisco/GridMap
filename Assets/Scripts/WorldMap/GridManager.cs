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

        public List<Texture2D> Texutures;

        public HexDisplay hexDisplay;
        public enum HexDisplay { Color, Texture}

        private RenderParams rp;

        HexTile hex;

        [Header("UI")]
        public InputField xInput;
        public InputField yInput;
        public InputField chunkInput;
        public Text textCom;
        public Toggle simplifyToggle;
        public Toggle gpuToggle;
        public Button genBtn;
        public Text triangles;
        public Text vertices;

        public bool UseInstance = true;
        public bool Simplify = true;  
        private void Awake()
        {
            Application.targetFrameRate = -1;
            
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            rp = new RenderParams(material);
            
            HexTile.hexSettings = HexSettings;

            GenerateGridChunks();
        }

        public Material material;

        public Vector2Int MapSize;

        public int ChunkSizeX, ChunkSizeZ;

        Stopwatch timer = new Stopwatch();
        public void GenerateGridChunks()
        {
            timer.Start();
            HexTiles.Clear();

            HexTiles = HexTile.CreatesHexes(MapSize, this);

            UseChunks();

            timer.Stop();

            TimeSpan elapsedTime = timer.Elapsed;
            string formattedTime = $"{elapsedTime.Minutes}m : {elapsedTime.Seconds} s";

            Debug.Log("Generation Took : " + formattedTime);

            timer.Reset();
        }
        void createmesh()
        {
            hex = new HexTile(this, 0, 0);

            hex.CreateMesh();

            hex.DrawMesh();
        }
        private void CreateHexChunks()
        {
            // create the appropriate amount of chunks to cover the whole map
            DestroyChildren();

            hexChunks.Clear();

            int chunkCountX = Mathf.CeilToInt(MapSize.x / ChunkSizeX);
            int chunkCountZ = Mathf.CeilToInt(MapSize.y / ChunkSizeZ);

            HexChunk chunk;

            for (int z = 0; z < chunkCountZ; z++)
            {
                for (int x = 0; x < chunkCountX; x++)
                {
                    chunk = new HexChunk();
                    hexChunks.Add(chunk);
                }
            }

            Debug.Log("Chunk count: " + hexChunks.Count);
        }
        public void UseChunks()
        {
            CreateHexChunks();

            HexChunk hc;
            
            foreach (HexTile hexTile in HexTiles.Values)
            {
                hc = GetHexChunk(hexTile.GridCoordinates.x, hexTile.GridCoordinates.y);

                hc.AddHex(hexTile);
            }

            foreach (HexChunk chunk in hexChunks)
            {
                chunk.CombinesMeshes();
            }

            GenerateHexObjects();
        }
        private void GenerateHexObjects()
        {
            foreach (HexChunk chunk in hexChunks)
            {
                GameObject newChunk = Instantiate(hexChunkPrefab, hexParent.transform);
                newChunk.GetComponent<MeshFilter>().mesh = chunk.mesh;
            }
        }
        private HexChunk GetHexChunk(int x, int z)
        {
            // base on the x and z coordinates of the hex, return the appropriate chunk index
            
            int chunkCountX = Mathf.CeilToInt(MapSize.x / ChunkSizeX);
            int chunkCountZ = Mathf.CeilToInt(MapSize.y / ChunkSizeZ);

            int chunkX = (Mathf.FloorToInt((float)x / ChunkSizeX) % chunkCountX);
            int chunkZ = (Mathf.FloorToInt((float)z / ChunkSizeZ) % chunkCountZ);

            int index = chunkX + chunkZ * chunkCountZ;

            return hexChunks[index];
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

        //////////////////////////////////////////////////// UI CODE \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

        public float updateInterval = 0.5f; // Time interval to update the frame rate
        private float accumulatedFrames = 0;
        private float timeLeft;
        void CountFrame()
        {
            timeLeft -= Time.deltaTime;
            accumulatedFrames++;

            if (timeLeft <= 0)
            {
                float framesPerSecond = accumulatedFrames / updateInterval;
                textCom.text = framesPerSecond.ToString("F2");

                accumulatedFrames = 0;
                timeLeft = updateInterval;
            }
        }
        public void GenClick()
        {
            int x = int.Parse(xInput.text);
            int y = int.Parse(yInput.text);

            if (chunkInput.text != "")
            {
                int chunk = int.Parse(chunkInput.text);
                ChunkSizeX = chunk;
                ChunkSizeZ = chunk;
            }

            MapSize.x = x;
            MapSize.y = y;

            GenerateGridChunks();
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
