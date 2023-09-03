
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
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
        [SerializeField] float oceanMultiplier;
        [Range(0, 10)]
        [SerializeField] float oceanLevelScale;
        
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
                MainPlanet.RandomizeBiome();
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

            RandomizeTemp();       
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

            test.Stop();

            UnityEngine.Debug.Log("Randomize Took: " + test.Elapsed.TotalSeconds.ToString("0.00"));
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

            tempNoise *= oceanMultiplier;

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

        public Color GetBiomeColor(int x, int y)
        {
            float temp, precip;

            temp = PlanetTemperature[x, y];
            precip = PlanetPrecipitation[x, y];

            Biome biome = MainPlanet.GetBiome(temp, precip);

            return MainPlanet.GetColor(biome);
        }
        public Texture2D GetBiomeTexture(int x, int y)
        {
            float temp, precip;

            temp = PlanetTemperature[x, y];
            precip = PlanetPrecipitation[x, y];

            Biome biome = MainPlanet.GetBiome(temp, precip);

            return MainPlanet.GetTexture(biome);
        }

        public Texture2D GetBiomeTextureLand(int x, int y)
        {
            float land, ocean;

            land = PlanetSurface[x, y];
            ocean = PlanetOcean[x, y];

            return MainPlanet.GetLandTexture(land, ocean);
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

    }
}
