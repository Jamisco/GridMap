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
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Legacy;

namespace Assets.Scripts.WorldMap
{
    [System.Serializable]
    public class GridManager : MonoBehaviour
    {
        public PlanetGenerator planetGenerator;

        public HexSettings HexSettings;

        public HexTile hexPrefab;
        public GameObject hexParent;

        public HexChunk hexChunkPrefab;
        private List<HexChunk> hexChunks;

        Dictionary<Axial, HexTile> HexTiles;

        public List<Texture2D> Texutures;

        public HexDisplay hexDisplay;

        public enum HexDisplay { Color, Texture}

        public bool useChunks = false;

        private void Awake()
        {
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            hexPrefab = GetComponentInChildren<HexTile>();
            
            hex = Instantiate(hexPrefab, hexParent.transform);

            rp = new RenderParams(material);

            HexTile.hexSettings = HexSettings;

            if(useGpu)
            {
                createmesh();
                GenerateGridInstance();
            }
            else
            {
                GenerateGrid();
            }
        }

        public bool useGpu;

        Stopwatch timer = new Stopwatch();

        public Material material;

        struct InstanceData
        {
            public Matrix4x4 objectToWorld;
        };
        public void GenerateGrid()
        {
            DestroyChildren();
            hexPrefab = GetComponentInChildren<HexTile>();
            HexTiles.Clear();

            ComputePlanetNoise();

            HexTile hex;

            timer.Start();

            for (int x = 0; x < planetGenerator.PlanetSize.x; x++)
            {
                for (int z = 0; z < planetGenerator.PlanetSize.y; z++)
                {
                    if (useChunks)
                    {
                        hex = Instantiate(hexPrefab);
                    }
                    else
                    {
                        hex = Instantiate(hexPrefab, hexParent.transform);
                    }
                   
                    hex.Initialize(this, x, z);

                    HexTiles.Add(hex.AxialCoordinates, hex);

                    hex.CreateMesh();
                }
            }

            foreach (HexTile hexTile in HexTiles.Values)
            {
                hexTile.CreateSlopeMesh();
                
                if (hexDisplay == HexDisplay.Color)
                {
                    Color aColor = planetGenerator.GetBiomeColor(hexTile.GridCoordinates.x, hexTile.GridCoordinates.y);

                    hexTile.SetColors(aColor);
                }
                else
                {
                    Texture2D texture = planetGenerator.GetBiomeTexture(hexTile.GridCoordinates.x, hexTile.GridCoordinates.y);

                    hexTile.SetTexture(texture);
                }
                
                hexTile.DrawMesh();
            }

            if (useChunks)
            {
                UseChunks();
            }

            timer.Stop();
            TimeSpan ts = timer.Elapsed;

            string formattedTime = $"{ts:mm\\m\\ ss\\s\\ fff\\m\\s}";

            Debug.Log(formattedTime);
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

        RenderParams rp;
        List<InstanceData> IDatas;
        List<Matrix4x4> IMatrixs;
        HexTile hex;

        bool ready = false;
        public void GenerateGridInstance()
        {
            HexTiles.Clear();
            
            IDatas = new List<InstanceData>();
            IMatrixs = new List<Matrix4x4>();

            timer.Start();

            for (int x = 0; x < planetGenerator.PlanetSize.x; x++)
            {
                for (int z = 0; z < planetGenerator.PlanetSize.y; z++)
                {
                    InstanceData data;

                    data.objectToWorld = Matrix4x4.Translate(GetPosition(x, z));

                    IDatas.Add(data);
                    IMatrixs.Add(data.objectToWorld);
                }
            }

            splitList();
            ready = true;
            
            timer.Stop();
            TimeSpan ts = timer.Elapsed;

            string formattedTime = $"{ts:mm\\m\\ ss\\s\\ fff\\m\\s}";

           // Debug.Log(formattedTime);
        }

        List<List<InstanceData>> allIdata = new List<List<InstanceData>>();
        List<List<Matrix4x4>> allMatrix = new List<List<Matrix4x4>>();
        
        InstanceData[][] allIdataArray;
        Matrix4x4[][] allMatrixArray;

        void splitList()
        {
            allIdata.Clear();
            allMatrix.Clear();
           

            int chunkSize = 1023;

            int chunkCount = Mathf.CeilToInt(IDatas.Count / (float)chunkSize);

            for (int i = 0; i < chunkCount; i++)
            {
                allIdata.Add(IDatas.GetRange(i * chunkSize, Mathf.Min(chunkSize, IDatas.Count - (i * chunkSize))));

                allMatrix.Add(IMatrixs.GetRange(i * chunkSize, Mathf.Min(chunkSize, IMatrixs.Count - (i * chunkSize))));

            }

            Debug.Log("IData size: " + allIdata.Count);
            Debug.Log("AllMatrix size: " + allMatrix.Count);

            allIdataArray = allIdata.Select(a => a.ToArray()).ToArray();
            allMatrixArray = allMatrix.Select(a => a.ToArray()).ToArray();
        }

            
        void createmesh()
        {
            hex.Initialize(this, 0, 0);

           // HexTiles.Add(hex.AxialCoordinates, hex);

           // hex.SetVector();

            hex.CreateMesh();

            hex.DrawMesh();
        }

        bool spanned = false;

        private Matrix4x4[] matrices; // Array to store transformation matrices
        void tester()
        {
            int instanceCount = 10;
            
            matrices = new Matrix4x4[instanceCount];

            for (int i = 0; i < instanceCount; ++i)
            {
                matrices[i] = Matrix4x4.Translate(new Vector3(5 + i, 0.0f, 5.0f));
            }
        }

        public Mesh aMesh;
        private void Update()
        {
            if(useGpu && ready)
            {
                //Span<Matrix4x4[]> spanData = allMatrixArray.AsSpan();

                //for (int i = 0; i < spanData.Length; i++)
                //{
                //    Graphics.DrawMeshInstanced(hex.HexMesh, 0, material, spanData[i]);
                //}

                Span<InstanceData[]> spanData = allIdataArray.AsSpan();
                for (int i = 0; i < spanData.Length; i++)
                {
                    Graphics.RenderMeshInstanced(rp, aMesh, 0, spanData[i]);
                }
            }

            // ComputePlanetNoise();
            // Check4Click();
        }

        public void UseChunks()
        {
            CreateHexChunks();

            foreach (HexTile hexTile in HexTiles.Values)
            {
                HexChunk hc = GetHexChunk(hexTile.GridCoordinates.x, hexTile.GridCoordinates.y);

                hc.AddHex(hexTile);
            }

            foreach (HexChunk chunk in hexChunks)
            {
                chunk.DrawChunk();
            }

        }

        public int ChunkSizeX, ChunkSizeZ;
        private void CreateHexChunks()
        {
            // create the appropriate ammount of chunks to cover the whole map

            DestroyChildren();

            hexChunks.Clear();

            int chunkCountX = Mathf.CeilToInt(planetGenerator.PlanetSize.x / ChunkSizeX);
            int chunkCountZ = Mathf.CeilToInt(planetGenerator.PlanetSize.y / ChunkSizeZ);

            HexChunk chunk;
            
            for (int z = 0; z < chunkCountZ; z++)
            {
                for (int x = 0; x < chunkCountX; x++)
                {
                    chunk = Instantiate(hexChunkPrefab, hexParent.transform);

                    hexChunks.Add(chunk);
                }
            }
        }

        private HexChunk GetHexChunk(int x, int z)
        {
            // base on the x and z coordinates of the hex, return the appropriate chunk index
            
            int chunkCountX = Mathf.CeilToInt(planetGenerator.PlanetSize.x / ChunkSizeX);
            int chunkCountZ = Mathf.CeilToInt(planetGenerator.PlanetSize.y / ChunkSizeZ);

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

        public void Check4Click()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo))
                {
                    if (hitInfo.collider != null)
                    {
                        // get hextile from hitinfo

                        HexTile hex = hitInfo.collider.GetComponent<HexTile>();

                        hex.ToggleInnerHighlight();

                        Debug.Log(hex.AxialCoordinates.ToString());
                    }
                }
            }
            else if(Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;

                if (Physics.Raycast(ray, out hitInfo))
                {
                    if (hitInfo.collider != null)
                    {
                        // get hextile from hitinfo

                        HexTile hex = hitInfo.collider.GetComponent<HexTile>();

                        hex.ToggleOuterHighlight();

                        Debug.Log(hex.AxialCoordinates.ToString());
                    }
                }
            }
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
                Destroy(child.gameObject);
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
