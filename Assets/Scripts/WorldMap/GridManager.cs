using Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Assets.Scripts.Miscellaneous;
using UnityEditor;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine.Events;
using System;
using Unity.VisualScripting;
using static Assets.Scripts.WorldMap.Planet;

namespace Assets.Scripts.WorldMap
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField]  public float updateInterval = 1f;
        
        private Grid BaseGrid;

        private Tilemap TileMap1;
        private Tilemap TileMap2;

        public Sprite MainSprite;
        private HexTile MainTile;

        public Material[] spriteMaterials;

        private bool Initialized = false;

        private PlanetGenerator biomeNoiseManager;
        private SpriteContainer spriteContainer;

        public Planet MainPlanet;

        [SerializeField] public Gradient Temperature;

        [SerializeField] public Gradient OceanGradient;

        [SerializeField] public Gradient LandGradient;

        [SerializeField] public DisplayColor displayColor = DisplayColor.All;

        public enum DisplayColor { Temperature, Precipitation, Ocean, Land, LandOcean, All };

        public bool pause = false;
        public void Initialize()
        {
            BaseGrid = GetComponent<Grid>();

            TileMap1 = BaseGrid.GetComponentByName<Tilemap>("map1");
            TileMap2 = BaseGrid.GetComponentByName<Tilemap>("map2");

            biomeNoiseManager = BaseGrid.GetComponent<PlanetGenerator>();

            biomeNoiseManager.MainPlanet = MainPlanet;

            spriteContainer = BaseGrid.GetComponent<SpriteContainer>();

            MainTile = new HexTile();

            MainTile.SetMap(this, TileMap1);

            MainTile.sprite = MainSprite;

            //TileMap1.GetComponent<TilemapRenderer>().material = spriteMaterials[0];

            Initialized = true;
        }

        private void Awake()
        {
            Initialize();
            GenerateMap();
        }

        void OnValidate()
        {
            
        }

        public void GenerateMap()
        {
            bool prevPayse = pause;
            pause = true;
            
            TileMap1.ClearAllTiles();
            TileMap2.ClearAllTiles();

            biomeNoiseManager.MainPlanet = MainPlanet;
            biomeNoiseManager.SetComputeSize();
            biomeNoiseManager.ComputeBiomeNoise();             

            Vector3Int tempPos;
            TileMap1.size = (Vector3Int)MainPlanet.PlanetSize;

            for (int x = 0; x < TileMap1.size.x; x++)
            {
                for (int y = 0; y < TileMap1.size.y; y++)
                {
                    tempPos = new Vector3Int(x, y, 0);

                    float percent = biomeNoiseManager.GetTemperature(x, y);                  

                    TileMap1.SetTile(tempPos, MainTile);

                    //Vector3Int temp = new Vector3Int(x, y);

                    //TileMap1.SetTileFlags(temp, TileFlags.None);

                    //TileMap1.SetColor(temp, new Color(1, 1, 1, percent));
                }
            }

            cutOff = OceanGradient.colorKeys[0].time;
            TileMap1.RefreshAllTiles();
            pause = prevPayse;
            
        }

        List<Vector2> PermanentOcean = new List<Vector2>();
        float cutOff;
        public Color GetColor(int x, int y)
        {
            float temp = biomeNoiseManager.GetTemperature(x, y);

            float rain = biomeNoiseManager.GetPrecipitation(x, y);

            float land = biomeNoiseManager.GetLand(x, y);

            float ocean = biomeNoiseManager.GetOcean(x, y);
  
            Vector2 position = new Vector2(x, y);

            if (ocean <= cutOff)
            {
                if (!PermanentOcean.Contains(position))
                {
                    PermanentOcean.Add(position);
                }
            }

            Biome b = MainPlanet.GetBiome(temp, rain);

            switch (displayColor)
            {
                case DisplayColor.Temperature:
                    
                    return Temperature.Evaluate(temp);
                    
                case DisplayColor.Precipitation:

                    return Temperature.Evaluate(rain);

                case DisplayColor.Ocean:

                    return OceanGradient.Evaluate(ocean);

                case DisplayColor.Land:

                    return LandGradient.Evaluate(land);

                case DisplayColor.LandOcean:
                            
                    if(!IsOcean(position))
                    {
                       return LandGradient.Evaluate(land);
                    }
                    else
                    {
                        return OceanGradient.Evaluate(ocean);
                    }

                case DisplayColor.All:

                    return MainPlanet.GetColor(b);
                    
                default:
                    return MainPlanet.GetColor(b);
            }  

            bool IsOcean(Vector2 pos)
            {
                if(PermanentOcean.Contains(pos))
                {
                    return true;
                }

                return false;
            }
        }

        public void UpdateMap()
        {
            biomeNoiseManager.MainPlanet = MainPlanet;
            biomeNoiseManager.SetComputeSize();
            
            biomeNoiseManager.ComputeBiomeNoise();
            TileMap1.RefreshAllTiles();

            PermanentOcean.Clear();
        }


        UniTask tasker;
        void Update()
        {
            if(!pause)
            {
                UpdateMap();
            }
        }
    }

    [CustomEditor(typeof(GridManager))]
    public class GridManagerUI : Editor
    {       
        public override void OnInspectorGUI()
        {
            GridManager myScript = (GridManager)target;

            if (GUILayout.Button("Initialize"))
            {
                myScript.Initialize();
            }

            if (GUILayout.Button("Generate Map"))
            {
                myScript.GenerateMap();
            }

            if (GUILayout.Button("RegenerateMap TileMap"))
            {
                myScript.Invoke("GenerateMap", 0);
            }

            base.OnInspectorGUI();
        }
    }

}