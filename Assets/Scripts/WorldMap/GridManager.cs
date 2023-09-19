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
using static Assets.Scripts.WorldMap.HexChunk;

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
        public Material InstanceMaterial;

        public PlanetGenerator planetGenerator;
        Planet mainPlanet;

        private Vector2Int MapSize;
        public int ChunkSize;

        public enum BiomeVisual { Color, Material }

        public BiomeVisual biomeVisual;

        private void SetGridSettings()
        {
            HexChunk.MainMaterial = MainMaterial;
            HexChunk.InstanceMaterial = InstanceMaterial;

            HexChunk.BiomeVisual = biomeVisual;

            HexTile.Grid = this;
            HexTile.Planet = planetGenerator;
            
            HexTile.hexSettings = HexSettings;

            planetGenerator.MainPlanet.Initialize();

            mainPlanet = planetGenerator.MainPlanet;
            MapSize = mainPlanet.PlanetSize;
        }

        private void Awake()
        {  
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            SetGridSettings();

            GenerateGridChunks();
        }

        private void Update()
        {
            UpdateHexProperties();
            //UpdateMeshInstanced();
        }

        Stopwatch timer = new Stopwatch();
        string formattedTime = "";
        TimeSpan elapsedTime;
        
        public void GenerateGridChunks()
        {
            timer.Start();
            HexTiles.Clear();

            SetGridSettings();

            planetGenerator.SetComputeSize();
            planetGenerator.ComputeBiomeNoise();

            HexTiles = HexTile.CreatesHexes(MapSize);
            hexes = HexTiles.Values.ToArray();

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
        
        List<Renderer> chunkRenderers = new List<Renderer>();
        private void GenerateHexObjects()
        {
            DestroyChildren();
            chunkRenderers.Clear();
            // create the appropriate amount of chunks to cover the whole map
            foreach (HexChunk chunk in hexChunks)
            {
                GameObject newChunk = Instantiate(hexChunkPrefab, hexParent.transform);

                newChunk.GetComponent<MeshFilter>().mesh = chunk.mesh;

                Renderer ren = newChunk.GetComponent<Renderer>();

                chunkRenderers.Add(ren);

                chunk.SetMaterialPropertyBlocks(ref ren);
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


        public bool UpdateMap = false;

        public void UpdateHexProperties()
        {
            if (!UpdateMap)
            {
                EnableChildren();
                return;
            }

            DisableChildren();

            Application.targetFrameRate = -1;

            planetGenerator.SetComputeSize();
            planetGenerator.ComputeBiomeNoise();

            // 100 x100 10 fps
            foreach (HexChunk chunk in hexChunks)
            {
                chunk.RenderMesh();
            }

            //// 100 x 100 5 fps
            //for (int i = 0; i < hexChunks.Count; i++)
            //{
            //    Renderer ren = chunkRenderers[i];
            //    hexChunks[i].UpdateMaterialBlock(ref ren);
            //}

            // for a 100 x 100, takes .200 seconds to update the whole map
            //Debug.Log("Update Took: " + timer.ElapsedMilliseconds / 1000f + "s");
        }

        List<List<MyInstanceData>> data2 = new List<List<MyInstanceData>>();
        MyInstanceData[] data;
        private void SetData()
        {
            // Data
            data = new MyInstanceData[hexes.Length];
            data2.Clear();

            Parallel.For(0, hexes.Length, x =>
            {
                MyInstanceData d = new MyInstanceData();
                d.objectToWorld = Matrix4x4.Translate(hexes[x].Position);
                d.renderingLayerMask = 0;
                d.hex = hexes[x].AxialCoordinates;

                data[x] = d;
            });

            while (data.Any())
            {
                data2.Add(data.Take(1023).ToList());
                data = data.Skip(1023).ToArray();
            }
        }

        List<List<Vector4>> color2 = new List<List<Vector4>>();
        Vector4[] color;
        private void SetColor()
        {
            color = new Vector4[hexes.Length];
            color2.Clear();

            Vector4[] aColor;
            // Color
            foreach (List<MyInstanceData> item in data2)
            {
                aColor = new Vector4[item.Count];

                Parallel.For(0, item.Count, x =>
                {
                    int z = item[x].hex.X;

                    if (z % 2 == 0)
                    {
                        aColor[x] = Color.green;
                    }
                    else
                    {
                        aColor[x] = Color.red;
                    }
                    // color[x] = hexes[x].HexBiomeProperties.BiomeColor;
                });

                color2.Add(aColor.ToList());
            }

            //Parallel.For(0, hexes.Length, x =>
            //{
            //    int z = hexes[x].GridCoordinates.x;

            //    if(z % 2 == 0)
            //    {
            //        color[x] = Color.green;
            //    }
            //    else
            //    {
            //        color[x] = Color.red;
            //    }
            //   // color[x] = hexes[x].HexBiomeProperties.BiomeColor;
            //});

            //while (color.Any())
            //{
            //    color2.Add(color.Take(1023).ToList());
            //    color = color.Skip(1023).ToArray();
            //}
        }

        HexTile[] hexes;

        public struct MyInstanceData
        {
            public Matrix4x4 objectToWorld; // We must specify object-to-world transformation for each instance
            public uint renderingLayerMask; // In addition we also like to specify rendering layer mask per instence.

            public Axial hex;
        };

        private void UpdateMeshInstanced()
        {
            if (!UpdateMap)
            {
                EnableChildren();
                return;
            }

            DisableChildren();

            planetGenerator.SetComputeSize();
            planetGenerator.ComputeBiomeNoise();

            if (data2.Count == 0)
            {
                SetData();
            }
               
            SetColor();

            int i = 0;

            foreach (List<MyInstanceData> item in data2)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                RenderParams renderParams = new RenderParams(InstanceMaterial);

                Vector4[] v = color2.ElementAt(i).ToArray();

                //SampleColor(v, item);

                block.SetVectorArray("_MeshColors", v);
                renderParams.matProps = block;
                HexTile newHex = null;
                HexTiles.TryGetValue(item[0].hex, out newHex);
                Mesh aMesh = newHex.DrawMesh();
                Graphics.RenderMeshInstanced(renderParams, aMesh, 0, item);

                i++;
            }
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
        private void EnableChildren()
        {
            if (chunkOn)
            {
                return;
            }

            int childCount = hexParent.transform.childCount;

            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = hexParent.transform.GetChild(i);
                child.gameObject.SetActive(true);
            }

            chunkOn = true;
        }
        
        bool chunkOn = true;
        private void DisableChildren()
        {
            if (!chunkOn)
            {
                return;
            }
            
            int childCount = hexParent.transform.childCount;

            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = hexParent.transform.GetChild(i);
                child.gameObject.SetActive(false);
            }

            chunkOn = false;
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
