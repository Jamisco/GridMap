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

       
        private void Awake()
        {
            HexTiles = new Dictionary<Axial, HexTile>();

            hexChunks = new List<HexChunk>();

            HexTile.hexSettings = HexSettings;

            GenerateGrid();
        }

        Stopwatch timer = new Stopwatch();
        public void GenerateGrid()
        {
            CreateHexChunks();
            
            hexPrefab = GetComponentInChildren<HexTile>();
            HexTiles.Clear();
            //DestroyAllChildren();

            ComputePlanetNoise();

            HexTile hex;

            timer.Start();

            for (int x = 0; x < planetGenerator.PlanetSize.x; x++)
            {
                for (int z = 0; z < planetGenerator.PlanetSize.y; z++)
                {
                    hex = Instantiate(hexPrefab);
                    
                    HexChunk hc = GetHexChunk(x, z);
                    
                    hc.AddHex(hex);

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

            foreach (HexChunk chunk in hexChunks)
            {
                chunk.DrawChunk();
            }

            timer.Stop();
            TimeSpan ts = timer.Elapsed;

            string formattedTime = $"{ts:mm\\m\\ ss\\s\\ fff\\m\\s}";

            Debug.Log(formattedTime);
        }

        public int ChunkSizeX, ChunkSizeZ;
        private void CreateHexChunks()
        {
            // create the appropriate ammount of chunks to cover the whole map

            DestroyChunks();

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
        private void Update()
        {
           // ComputePlanetNoise();
            Check4Click();
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
        private void DestroyChunks()
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
                exampleScript.GenerateGrid();
            }
        }
    }
#endif
    
}
