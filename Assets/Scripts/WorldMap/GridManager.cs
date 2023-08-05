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

namespace Assets.Scripts.WorldMap
{
    [System.Serializable]
    public class GridManager : MonoBehaviour
    {
        public PlanetGenerator planetGenerator;

        public HexSettings HexSettings;

        public GameObject hexParent;

        public HexChunk hexChunkPrefab;
        private List<HexChunk> hexChunks;

        Dictionary<Axial, HexTile> HexTiles;

        public List<Texture2D> Texutures;

        public HexDisplay hexDisplay;

        public enum HexDisplay { Color, Texture}

        public bool useChunks = false;

        public Mesh aMesh;
        HexTile hex;

        public InputField xInput;
        public InputField yInput;

        public Button genBtn;

        private void Awake()
        {
            //Application.targetFrameRate = -1;
            
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            rp = new RenderParams(material);

            HexTile.hexSettings = HexSettings;

            HexChunk.rp = rp;

            if(useGpu)
            {
                createmesh();
                GenerateGridInstance();
            }
            else
            {
                GenerateGrid();       
            }

            genBtn.onClick.AddListener(GenClick);
        }

        Stopwatch timer = new Stopwatch();

        public Material material;

        public Vector2 MapSize;
        public void GenerateGrid()
        {
            HexTiles.Clear();
            HexTile hex;

            for (int x = 0; x < MapSize.x; x++)
            {
                for (int y = 0; y < MapSize.y; y++)
                {
                    hex = new HexTile(this, x, y);

                    HexTiles.Add(hex.AxialCoordinates, hex);
                }
            }

            UseChunks();
        }

        public void GenClick()
        {
            int x = int.Parse(xInput.text);
            int y = int.Parse(yInput.text);

            MapSize.x = x;
            MapSize.y = y;

            GenerateGrid();
        }
    
        public void ChooseGrid()
        {
            if (useGpu)
            {
                ready = false;
                createmesh();
                GenerateGridInstance();
            }
            else
            {              
                GenerateGrid();
            }
        }

        InstanceData[][] allIdataArray;
        List<List<InstanceData>> allIdata = new List<List<InstanceData>>();
        
        RenderParams rp;
        List<InstanceData> IDatas = new List<InstanceData>();

        public bool useGpu;
        bool ready = false;
        struct InstanceData
        {
            public Matrix4x4 objectToWorld;
        };
        void createmesh()
        {
            hex = new HexTile(this, 0, 0);

            hex.CreateMesh();

            hex.DrawMesh();
        }
        void splitList()
        {
            allIdata.Clear();

            int chunkSize = 1023;

            int chunkCount = Mathf.CeilToInt(IDatas.Count / (float)chunkSize);

            for (int i = 0; i < chunkCount; i++)
            {
                allIdata.Add(IDatas.GetRange(i * chunkSize, Mathf.Min(chunkSize, IDatas.Count - (i * chunkSize))));
            }

            Debug.Log("IData size: " + allIdata.Count);

            allIdataArray = allIdata.Select(a => a.ToArray()).ToArray();
        }
        public void GenerateGridInstance()
        {
            HexTiles.Clear();

            IDatas.Clear();

            for (int x = 0; x < MapSize.x; x++)
            {
                for (int z = 0; z < MapSize.y; z++)
                {
                    InstanceData data;

                    data.objectToWorld = Matrix4x4.Translate(GetPosition(x, z));

                    IDatas.Add(data);
                }
            }

            splitList();
            ready = true;           
        }
        private void GpuInstance()
        {
            if (useGpu && ready)
            {
                Span<InstanceData[]> spanData = allIdataArray.AsSpan();

                for (int i = 0; i < spanData.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp, hex.HexMesh, 0, spanData[i]);
                }
            }
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
        }

        public int ChunkSizeX, ChunkSizeZ;
        private void CreateHexChunks()
        {
            // create the appropriate ammount of chunks to cover the whole map
            DestroyChildren();

            hexChunks.Clear();

            int chunkCountX = Mathf.CeilToInt(MapSize.x / ChunkSizeX);
            int chunkCountZ = Mathf.CeilToInt(MapSize.y / ChunkSizeZ);

            HexChunk chunk;
            
            for (int z = 0; z < chunkCountZ; z++)
            {
                for (int x = 0; x < chunkCountX; x++)
                {
                    chunk = Instantiate(hexChunkPrefab, hexParent.transform);     
                    hexChunks.Add(chunk);
                }
            }

            Debug.Log("Chunk count: " + hexChunks.Count);
        }

        JobHandle handle;

        public float updateInterval = 0.5f; // Time interval to update the frame rate
        private float accumulatedFrames = 0;
        private float timeLeft;
        public Text textCom;
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

        private void Update()
        {
            if(useGpu)
            {
                GpuInstance();
            }
            else
            {
                foreach (HexChunk chunk in hexChunks)
                {
                    chunk.DrawChunk();
                }

                CountFrame();
            }       
        }


        private void LateUpdate()
        {
            handle.Complete();
        }

        public static List<HexChunk> hallo;
        public struct Chunker : IJob
        {
            public void Execute()
            {
                foreach (HexChunk chunk in hallo)
                {
                    chunk.DrawChunk();
                }
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
        private Texture2D RandomTextures()
        {
            return Texutures[UnityEngine.Random.Range(0, Texutures.Count)];
        }

        private void ComputePlanetNoise()
        {
            planetGenerator.SetComputeSize();
            planetGenerator.ComputeBiomeNoise();
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
                exampleScript.ChooseGrid();
            }
        }
    }
#endif
    
}
