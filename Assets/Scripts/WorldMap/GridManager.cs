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

namespace Assets.Scripts.WorldMap
{
    [System.Serializable]
    public class GridManager : MonoBehaviour
    {
        public PlanetGenerator planetGenerator;

        public HexSettings HexSettings;

        public HexTile hexPrefab;
        public GameObject hexParent;

        Dictionary<Axial, HexTile> HexTiles;

        private void Awake()
        {
            HexTiles = new Dictionary<Axial, HexTile>();

            HexTile.hexSettings = HexSettings;


            //HexTile.outerRadius = outerRadius;
            //HexTile.innerRadius = innerRadius;
            //HexTile.stepDistance = stepDistance;

            GenerateGrid();
        }

        Stopwatch timer = new Stopwatch();
        public void GenerateGrid()
        {
            HexTiles.Clear();
            DestroyAllChildren();

            ComputePlanetNoise();

            HexTile hex;

            timer.Start();

            for (int x = 0; x < planetGenerator.PlanetSize.x; x++)
            {
                for (int z = 0; z < planetGenerator.PlanetSize.y; z++)
                {
                    hex = Instantiate(hexPrefab, hexParent.transform);
                    hex.Initialize(this, x, z);
                    
                    HexTiles.Add(hex.AxialCoordinates, hex);

                    hex.CreateMesh();
                }
            }

            foreach (HexTile hexTile in HexTiles.Values)
            {
                hexTile.CreateSlopeMesh();

                hexTile.DrawMesh();
            }

            timer.Stop();
            TimeSpan ts = timer.Elapsed;

            string formattedTime = $"{ts:mm\\m\\ ss\\s\\ fff\\m\\s}";

            Debug.Log(formattedTime);
        }

        private void ComputePlanetNoise()
        {
            planetGenerator.SetComputeSize();
            planetGenerator.ComputeBiomeNoise();
        }
        private void Update()
        {
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

        private void DestroyAllChildren()
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
}