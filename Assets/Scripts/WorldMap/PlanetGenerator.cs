
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.Planet;
using static FastNoiseLite;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace Assets.Scripts.WorldMap
{
    public class PlanetGenerator : MonoBehaviour
    {
        [SerializeField] public Planet MainPlanet;
        [SerializeField] public bool CircularSun = false;
        [SerializeField] public bool VerticalSun = false;

        [Range(1, 20)]
        [SerializeField] int planetFractal = 1;

        [SerializeField] private bool PrintArrays = false;
        [SerializeField] private bool RandomBiomeColor = false;

        [Header("Land Noise")]
        
        [SerializeField] private int LandSeed = 1337;
        
        [Range(0, .3f)]
        [SerializeField] private float LandFrequemcy = .01f;
        
        FastNoiseLite landFNoise = new FastNoiseLite();
        FastNoiseLite OceanFNoise = new FastNoiseLite();

        // Land values
        [SerializeField] private FractalType LFractal = FractalType.None;
        
        [SerializeField] private NoiseType LNoise = NoiseType.Perlin;

        [Range(0, 10)]
        [SerializeField] float landMultiplier;
        [Range(0, 10)]
        [SerializeField] float landLevelScale;
        
        [SerializeField] Vector2Int landOffset;

        [Header("Ocean Noise")]
        
        [SerializeField] private int OceanSeed = 1337;

        [Range(0, .3f)]
        [SerializeField] private float OceanFrequemcy = .01f;
        
        [SerializeField] private FractalType OFractal = FractalType.None;
        [SerializeField] private NoiseType ONoise = NoiseType.Perlin;

        [Range(0, 10)]
        [SerializeField] float marineMultiplier;
        [Range(0, 10)]
        [SerializeField] float marineLevelScale;

        [Tooltip("This denotes the level at which the ocean ends. Ocean starts from 0")]
        [Range(0,1)]
        [SerializeField] float marineLevel;

        [SerializeField] Vector2Int oceanOffset;


        private void Awake()
        {
            UpdateNoise();
        }

        public Vector2Int PlanetSize
        {
            get
            {
                return MainPlanet.PlanetSize;
            }            
        }

        private void UpdateNoise()
        {
            landFNoise.SetSeed(LandSeed);
            landFNoise.SetNoiseType(LNoise);
            landFNoise.SetFractalType(LFractal);
            landFNoise.SetFrequency(LandFrequemcy);

            OceanFNoise.SetSeed(OceanSeed);
            OceanFNoise.SetNoiseType(ONoise);
            OceanFNoise.SetFractalType(OFractal);
            OceanFNoise.SetFrequency(OceanFrequemcy);
        }

        private void OnValidate()
        {
            if(PrintArrays == true)
            {
                PrintArrays = false;
                PrintDistrubutionPercent();
            }

            if (RandomBiomeColor == true)
            {
                RandomBiomeColor = false;
            }

            UpdateNoise();
        }

        private List<int> LandStore = new List<int>(); // 0
        private List<int> PrecipitationStore = new List<int>(); // 1
        private List<int> TemperatureStore = new List<int>(); // 2
        private List<int> OceanStore = new List<int>(); // 3


        public void SetComputeSize()
        {
            PlanetTemperature = new float[MainPlanet.PlanetSize.x, MainPlanet.PlanetSize.y];
            PlanetPrecipitation = new float[MainPlanet.PlanetSize.x, MainPlanet.PlanetSize.y];
            PlanetSurface = new float[MainPlanet.PlanetSize.x, MainPlanet.PlanetSize.y];
            PlanetOcean = new float[MainPlanet.PlanetSize.x, MainPlanet.PlanetSize.y];

            // clear arrays first

            LandStore.Clear();
            PrecipitationStore.Clear();
            TemperatureStore.Clear();
            OceanStore.Clear();
            

            for (int i = 0; i < 11; i++)
            {
                LandStore.Add(0);
            }

            for (int i = 0; i < 11; i++)
            {
                PrecipitationStore.Add(0);
            }

            for (int i = 0; i < 11; i++)
            {
                TemperatureStore.Add(0);
            }

            for (int i = 0; i < 11; i++)
            {
                OceanStore.Add(0);
            }
        }

        float[,] PlanetTemperature;
        float[,] PlanetPrecipitation;
        
        public void ComputeBiomeNoise()
        {
            //test.Restart();
            
            Parallel.For(0, MainPlanet.PlanetSize.x, x =>
            {
                for (int y = 0; y < MainPlanet.PlanetSize.y; y++)
                {
                    ComputeTemperatureNoise(x, y);
                    ComputePrecipitationNoise(x, y);
                    ComputeLandNoise(x, y);
                    ComputeOceanNoise(x, y);
                }
            });

            test.Stop();

           // Debug.Log("ComputeBiomeNoise Took: " + test.ElapsedMilliseconds / 1000f + "s");

        }

        private void ComputeTemperatureNoise(int x, int y)
        {
           float temp = IntensityFromSunrayFocus(new Vector2Int(x, y));
            DistributionOfValues(2, temp);
            PlanetTemperature[x, y] = temp;
        }

        Stopwatch test = new Stopwatch();
        private void RandomizeTemp()
        {
            test.Restart();
            
            for (int x = 0; x < MainPlanet.PlanetSize.x; x++)
            {
                for (int y = 0; y < MainPlanet.PlanetSize.y; y++)
                {
                    float temp = PlanetTemperature[x, y] + UnityEngine.Random.Range(-.05f, .05f);

                    //clamp

                    temp = Mathf.Clamp(temp, minTemperature, 1);

                    PlanetTemperature[x, y] = temp;
                }
            }
        }

        private Random deviator = new Random();
        [Header("Temperature Noise")]

        [Range(0, 1)]
        [SerializeField] float minTemperature;
        
        [Range(0, 10)]
        [SerializeField] float TemperatureMultiplier;

        /// <summary>
        /// Returns the proximity to MainPlanet focus point in percent. Smaller percent mean the position is closer meanwhile larger numbers mean much further to focus point
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        float IntensityFromSunrayFocus(Vector2Int position)
        {
            // measure only Y axis, remember that the sun is thesame across Y axis
            // use a normalization formula
            // use sin and wrap the formula
            float percentDistance;
            
            if (VerticalSun)
            {
                percentDistance = (float) (position.y - MainPlanet.CurrentSunRayFocus.y) / MainPlanet.PlanetSize.y;

                percentDistance = Mathf.Abs(percentDistance);
            }
            else
            {
                //// the wrapped distance should be max 50%
                ///
                 percentDistance = HexFunctions.CalculateDistanceAsPercent((Vector3Int)position, (Vector3Int)MainPlanet.CurrentSunRayFocus, (Vector3Int)MainPlanet.PlanetSize, HexFunctions.Edges.Both, CircularSun);
            }
            
            float percentIntensity;

            // cos graph from [0 - 1] gives us a good smoothing
            // this is pretty much a clamp
            //percentDistance = percentDistance / 50 * (Mathf.PI / 2);

            //percentIntensity = Mathf.Cos(percentDistance);

            //percentIntensity *= TemperatureMultiplier;

            percentIntensity = Mathf.Abs(MathF.Cos(percentDistance * MathF.PI));

            percentIntensity = Mathf.Clamp(percentIntensity, minTemperature, 1);

           // percentIntensity = Mathf.Pow(percentIntensity, 3);
            
            percentIntensity *= TemperatureMultiplier;

            return percentIntensity;
        }

        public float GetTemperature(int x, int y)
        {
            return PlanetTemperature[x, y];
        }

        [Header("Precipitation Noise")]

        [Range(0, 5)]
        [SerializeField] float precipitationScale;
        
        [Range(0, 1)]
        [SerializeField] float minPrecipitation;

        [Range(0,10)]
        [SerializeField] float rainMultiplier;
        [SerializeField] Vector2Int pOffset;

        public void ComputePrecipitationNoise(int x, int y)
        {
            float tempNoise = 0;
            float divisor = 0;
            float adder = 0;
            float multiplier = 0;

           // planetFractal = Mathf.Clamp(planetFractal, minPrecipFractal, 25);

            adder = planetFractal * 2;

            multiplier = precipitationScale;

            for (int i = 1; i <= planetFractal; i++)
            {
                multiplier *= 2;

                adder /= 2;

                // so we can normalize the values later
                divisor += adder;

                tempNoise += adder * GetNoise(multiplier * x / MainPlanet.PlanetSize.x + pOffset.x, multiplier * y / MainPlanet.PlanetSize.y + pOffset.y);
            }

            tempNoise = tempNoise / divisor;

            tempNoise *= rainMultiplier;

            tempNoise = Mathf.Clamp(tempNoise, minPrecipitation, 1);

            DistributionOfValues(1, tempNoise / divisor);

            PlanetPrecipitation[x, y] = tempNoise / divisor;
        }

        float[,] PlanetSurface;
        float[,] PlanetOcean;
            
        private void ComputeLandNoise(int x, int y)
        {
            float tempNoise = 0;
            float tempNoise2 = 0;

            float divisor = 0;
            float adder = 0;
            float multiplier = 0;

            float position = Mathf.Sin(Mathf.PI * ((float)x / MainPlanet.PlanetSize.x));

            multiplier = landLevelScale;
            adder = planetFractal * 2;

            //tempNoise = landFNoise.GetNoise(multiplier * X / MainPlanet.PlanetSize.X + landOffset.X, multiplier * Y / MainPlanet.PlanetSize.Y + landOffset.Y);

            tempNoise = landFNoise.GetNoise(x + landOffset.x, y + landOffset.y);

            tempNoise = ExtensionMethods.Normalize(tempNoise, -1, 1, 0, 1);

            tempNoise *= landMultiplier;

            DistributionOfValues(0, tempNoise);
            PlanetSurface[x, y] = tempNoise;
        }

        private void ComputeOceanNoise(int x, int y)
        {
            float tempNoise = 0;
            
            tempNoise = OceanFNoise.GetNoise(x + oceanOffset.x, y + oceanOffset.y);

            tempNoise = ExtensionMethods.Normalize(tempNoise, -1, 1, 0, 1);

            tempNoise *= marineMultiplier;

            DistributionOfValues(3, tempNoise);
            PlanetOcean[x, y] = tempNoise;
        }
        public float GetPrecipitation(int x, int y)
        {
            return PlanetPrecipitation[x, y];
        }
        public float GetLand(int x, int y)
        {
            return PlanetSurface[x, y];
        }
        public float GetOcean(int x, int y)
        {
            return PlanetOcean[x, y];
        }
        private float GetNoise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }



        private void DistributionOfValues(int arrayType, float value)
        {
            // initialize 10 array values

            int index = Mathf.FloorToInt(Mathf.Abs(value) * 10);

            index = (index > 9) ? 10 : index;

            if (arrayType == 0)
            {
                LandStore[index]++;
            }
            else if (arrayType == 1)
            {
                PrecipitationStore[index]++;
            }
            else if (arrayType == 2)
            {
                TemperatureStore[index]++;
            }
            else if(arrayType == 3)
            {
                OceanStore[index]++;
            }
        }

        private void PrintDistrubutionPercent()
        {
            int total = 0;

            for (int i = 0; i < LandStore.Count; i++)
            {
                total += LandStore[i];
            }

            string printer = "Surface: ";
            
            for (int i = 0; i < LandStore.Count; i++)
            {
                printer += i + ": " + ((float)LandStore[i] / total) * 100 + " -- ";
            }

            Debug.Log(printer);

            total = 0;

            for (int i = 0; i < PrecipitationStore.Count && total > 0; i++)
            {
                total += PrecipitationStore[i];
            }

            printer = "Precipitation: ";

            for (int i = 0; i < PrecipitationStore.Count && total > 0; i++)
            {
                printer += i + ": " + ((float)PrecipitationStore[i] / total) * 100 + " -- ";
            }

            Debug.Log(printer);

            total = 0;

            for (int i = 0; i < TemperatureStore.Count; i++)
            {
                total += TemperatureStore[i];
            }

            printer = "Temperature: ";

            for (int i = 0; i < TemperatureStore.Count && total > 0; i++)
            {
                printer += i + ": " + ((float)TemperatureStore[i] / total) * 100 + " -- ";
            }

            Debug.Log(printer);
        }

        public void InsertData(MapData data)
        {
            planetFractal = data.planetFractal;

            // Land Values
            LandSeed = data.LandSeed;
            LandFrequemcy = data.LandFrequemcy;

            LFractal = data.LFractal;
            LNoise = data.LNoise;

            landMultiplier = data.landMultiplier;
            landLevelScale = data.landLevelScale;

            landOffset = data.landOffset;

            // Ocean Values
            OceanSeed = data.OceanSeed;

            OceanFrequemcy = data.OceanFrequemcy;

            OFractal = data.OFractal;
            ONoise = data.ONoise;

            marineMultiplier = data.marineMultiplier;
            marineLevelScale = data.marineLevelScale;

            marineLevel = data.marineLevel;
            oceanOffset = data.oceanOffset;

            // Precipitation Values
            minTemperature = data.minTemperature;
            TemperatureMultiplier = data.TemperatureMultiplier;

            // Precipitation Values
            precipitationScale = data.PrecipitationScale;
            minPrecipitation = data.minPrecipitation;
            rainMultiplier = data.RainMultiplier;
            pOffset = data.pOffset;
            
            
        }

        public BiomeProperties GetBiomeProperties(int x, int y)
        {
            float temp = GetTemperature(x, y);
            float precip = GetPrecipitation(x, y);
            
            float land = GetLand(x, y);
            float ocean = GetOcean(x, y);

            SurfaceType surfaceType;
            float surface = 0;
            
            if(ocean > land)
            {
                surfaceType = SurfaceType.Marine;
                surface = ocean;
            }
            else
            {
                surfaceType = SurfaceType.Terrestrial;
                surface = land;
            }

            GridValues gridValues = new GridValues(temp, precip, surface, surfaceType);

            return MainPlanet.GetBiomeProperties(gridValues);
        }

        public struct GridValues
        {
            public float Temperature;
            public float Precipitation;
            public float Surface;
            
            public SurfaceType SurfaceType;

            public GridValues(float temp, float precip, float surface, SurfaceType surfaceType)
            {
                Temperature = temp;
                Precipitation = precip;
                Surface = surface;
                SurfaceType = surfaceType;
            }
        }

        public struct MapData
        {
            public int planetFractal;

            // Land Values
            public int LandSeed;
            public float LandFrequemcy;

            public FractalType LFractal;
            public NoiseType LNoise;

            public float landMultiplier;
            public float landLevelScale;

            public Vector2Int landOffset;

            // Ocean Values
            public int OceanSeed;

            public float OceanFrequemcy;

            public FractalType OFractal;
            public NoiseType ONoise;

            public float marineMultiplier;
            public float marineLevelScale;

            public float marineLevel;
            public Vector2Int oceanOffset;

            // Temperature Values
            public float minTemperature;
            public float TemperatureMultiplier;

            // Precipitation Values
            public float PrecipitationScale;
            public float minPrecipitation;
            public float RainMultiplier;
            public Vector2Int pOffset;

            public MapData(PlanetGenerator planet)
            {
                planetFractal = planet.planetFractal;

                // Land Values
                LandSeed = planet.LandSeed;
                LandFrequemcy = planet.LandFrequemcy;

                LFractal = planet.LFractal;
                LNoise = planet.LNoise;

                landMultiplier = planet.landMultiplier;
                landLevelScale = planet.landLevelScale;

                landOffset = planet.landOffset;

                // Ocean Values
                OceanSeed = planet.OceanSeed;

                OceanFrequemcy = planet.OceanFrequemcy;

                OFractal = planet.OFractal;
                ONoise = planet.ONoise;

                marineMultiplier = planet.marineMultiplier;
                marineLevelScale = planet.marineLevelScale;

                marineLevel = planet.marineLevel;
                oceanOffset = planet.oceanOffset;

                // Temperature Values

                minTemperature = planet.minTemperature;
                TemperatureMultiplier = planet.TemperatureMultiplier;

                // Precipitation Values
                PrecipitationScale = planet.precipitationScale;
                minPrecipitation = planet.minPrecipitation;
                RainMultiplier = planet.rainMultiplier;
                pOffset = planet.pOffset;
            }

        }

#if UNITY_EDITOR

        // create custom editor class

        [CustomEditor(typeof(PlanetGenerator))]

        public class PlanetGridEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                PlanetGenerator myScript = (PlanetGenerator)target;

                if (GUILayout.Button("Save Planet Data"))
                {
                    MapData data = new MapData(myScript);
                    
                    string planetData = JsonUtility.ToJson(data, true);

                    File.WriteAllText(UniversalData.MapDataSavePath, planetData);
                }

                if (GUILayout.Button("Insert Saved Data"))
                {

                    if (File.Exists(UniversalData.MapDataSavePath) == false)
                    {
                        Debug.LogError("No Map Data File Found");
                        return;
                    }
                    
                    string planetData = File.ReadAllText(UniversalData.MapDataSavePath);

                    MapData data = JsonUtility.FromJson<MapData>(planetData);

                    myScript.InsertData(data);
                }
            }

            
        }

#endif
    }
}
