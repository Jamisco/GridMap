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
        
        private ComputeBuffer argsBuffer;  // Indirect buffer
        private void Awake()
        {
            Application.targetFrameRate = -1;
            
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            rp = new RenderParams(material);

            HexChunk.simplify = Simplify;
            HexTile.hexSettings = HexSettings;

            createmesh();
            GenerateGridChunks();

            simplifyToggle.onValueChanged.AddListener(SimplifyToggleClick);
            gpuToggle.onValueChanged.AddListener(GpuToggleClick);

            SimplifyToggleClick(Simplify);
            GpuToggleClick(UseInstance);

            genBtn.onClick.AddListener(GenClick);
        }

        public Material material;

        public Vector2Int MapSize;

        public void SimplifyToggleClick(bool value)
        {
            if (simplifyToggle.isOn)
            {
                simplifyToggle.GetComponentInChildren<Image>().color = Color.green;
                    
                Simplify = true;
            }
            else
            {
                if(MapSize.x * MapSize.y > 490000)
                {
                    Debug.LogError("Map Size is too big. It must be simplified");
                    return;
                }
                simplifyToggle.GetComponentInChildren<Image>().color = Color.white;
                Simplify = false;
            }

            HexChunk.simplify = Simplify;

            // so we can rerender game objects
            foreach (HexChunk chunk in hexChunks)
            {
                chunk.SwitchMesh();
            }

            DestroyChildren();

            SetStats();
        }

        public void GpuToggleClick(bool value)
        {
            if (gpuToggle.isOn)
            {
                gpuToggle.GetComponentInChildren<Image>().color = Color.green;
                UseInstance = true;
            }
            else
            {
                gpuToggle.GetComponentInChildren<Image>().color = Color.white;
                UseInstance = false;
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
        void createmesh()
        {
            hex = new HexTile(this, 0, 0);

            hex.CreateMesh();

            hex.DrawMesh();
        }

        Stopwatch timer = new Stopwatch();
        public void GenerateGridChunks()
        {
            if (MapSize.x * MapSize.y > 490000 && Simplify == false)
            {
                Debug.LogError("Map Size is too big. It must be simplified");
                return;
            }

            timer.Start();
            HexTiles.Clear();

            HexChunk.ResetStats();

            HexChunk.simplify = Simplify;


            HexTiles = HexTile.CreatesHexes(MapSize, this);

            UseChunks();

            timer.Stop();

            TimeSpan elapsedTime = timer.Elapsed;
            string formattedTime = $"{elapsedTime.Minutes}m : {elapsedTime.Seconds} s";

            Debug.Log("Generation Took : " + formattedTime);

            timer.Reset();
        }

        public int ChunkSizeX, ChunkSizeZ;
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

            meshes.Clear();

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            argsBuffer = new ComputeBuffer(hexChunks.Count, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            //uint[] args = new uint[5] { mesh.GetIndexCount(0), (uint)0, mesh.GetIndexStart(0), mesh.GetBaseVertex(0), 0 };
            argsBuffer.SetData(args);

            foreach (HexChunk chunk in hexChunks)
            {
                chunk.CombinesMeshes();
                meshes.Add(chunk.mesh);
            }

            SetStats();

            if (!UseInstance)
            {
                GenerateHexObjects();
            }
        }

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

        public void SetStats()
        {
            triangles.text = HexChunk.Triangles.ToString("0.00") + "K";
            vertices.text = HexChunk.Vertices.ToString("0.00") + "K";
        }

        Matrix4x4 SpawnPosition = Matrix4x4.Translate(Vector3.zero);

        private List<Mesh> meshes = new List<Mesh>();
        private void Update()
        {
            if(timer.IsRunning)
            {
                return;
            }

            if(UseInstance)
            {
                RenderInstance();
            }
            else
            {
                GenerateHexObjects();
            }
            
            CountFrame();
        }

        public void RenderInstance()
        {
            if(rendered)
            {
                DestroyChildren();
            }

            foreach (HexChunk chunk in hexChunks)
            {
                Graphics.RenderMesh(rp, chunk.mesh, 0, SpawnPosition);
            }
        }

        bool rendered = false;
        public void GenerateHexObjects()
        {
            if(rendered)
            {
                return;
            }

            foreach (HexChunk chunk in hexChunks)
            {
                GameObject newChunk = Instantiate(hexChunkPrefab, hexParent.transform);
                newChunk.GetComponent<MeshFilter>().mesh = chunk.mesh;
            }

            rendered = true;
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

            rendered = false;
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
