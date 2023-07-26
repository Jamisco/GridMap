using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.Scripts.WorldMap
{
    [Serializable]
    public class Planet
    {
        public Vector2Int PlanetSize;

        /// <summary>
        /// The position at which the sun ray will be most intense
        /// </summary>
        public Vector2 InitialSunRayFocus;

        public Vector2Int SunlightVector;

        /// <summary>
        /// Max Sunlight in Fahrenheit divided by 100
        /// </summary>
        [Range(0, 13)]
        public float SunlightIntensity;

        public float SunlightArea;

        [Range(10, 365)]
        public int DaysInYear;

        public int currentDay;

        [SerializeField]
        public List<BiomeProperties> Biomes = new List<BiomeProperties>();

        public void RandomizeBiome()
        {
            Biomes.Clear();

            int biomeCount = UnityEngine.Random.Range(3, 6);

            foreach (Biome item in Enum.GetValues(typeof(Biome)))
            {
                BiomeProperties biome = new BiomeProperties();
                biome.Biome = item;
                biome.BiomeColor = UnityEngine.Random.ColorHSV(0, 1, 0, 1, 0, 1, 1, 1);


                Biomes.Add(biome);
            }
        }
        public Vector2Int CurrentSunRayFocus
        {
            get
            {
                int day = currentDay % DaysInYear;

                int xSpeed = Mathf.CeilToInt( (float)PlanetSize.x / DaysInYear);
                int ySpeed = Mathf.CeilToInt((float)PlanetSize.y / DaysInYear);

                Vector2Int speed = new Vector2Int(xSpeed, ySpeed);

                //  day = (day == 0) ? 1 : day;

                Vector2Int currPos = day * SunlightVector * speed;

                // wrap it around so its doesnt exceed the map size
                int x = (int)((currPos.x + InitialSunRayFocus.x) % PlanetSize.x);
                int y = (int)((currPos.y + InitialSunRayFocus.y) % PlanetSize.y);

                return new Vector2Int(x, y);
            }
        }

        // below 30% = nothing can grow

        public Biome GetBiome(float temperature, float precipitation)
        {
            // The Biome table is structured in a 2d graph format, where the X axis is the temperature and the Y axis is the precipitation
            // so we have to convert a 2d graph coordinate to array index
            
            // think of temp and precipitation as percentages

            // Example, if temp = 0.5f, then it means the temperature is at the Math.floor(.5 * 9) = 4. Where 9 in the max index of X axis of our array

            // Example, if precipitation = 0.3f,  then it means the precipitation is at the Math.floor(.3 * 9) = 2. Where 9 in the max index of Y axis of our array

            // so (.5, .3) = (.5 * 9, .3 * 9) = (4.5, 2.7) = (4, 3) = in 2D graph coords -  = (6, 4) in our array coords/index

            // (.99, 1) = (8.91, 9) = (9, 9) 2d coords = (0, 9) in our array coords/index

            // (.68, .85) = (6.12, 7.65) = (6, 8) 2d coords = (1, 6) in our array coords/index

            // This gives us the formula to get array index
            // X = 9 - Math.Round(precipitation * 9)
            // Y = Math.Round(temperature * 9)

            // 2d coor (X, Y) = array indexes (9 - Y, X)

            // First we have to convert these numbers from 2d coordinates to array coords

            int x = (int)(9 - Math.Round(precipitation * 9));
            int y = (int)Math.Round(temperature * 9);

            return (Biome)BiomeTable[x, y];
        }
                 

        public Color GetColor(Biome biome)
        {
            return Biomes.First(x => x.Biome == biome).BiomeColor;
        }

        private static readonly int[,] BiomeTable = new int[10, 10]
        {
            #region Explanation
            // X axis = temperature from cold to hot
            // Y axis = precipitation from dry to wet


            // each number represents the int conversion of the Biome

            // min temperature = -40f
            // max temperature = 140f

            // equation to get temperature = -40 + 18x, with X being the index

            // Rainfall is from 0 - 500cm 
            // equation to get rainfall = 50x, with X being the index


            // Temp:  -40, -22, -4, 14, 32, 50, 68, 86, 104, and 122. Fahrenheit

            // -40, -30, -20, -10, 0, 10, 20, 30, 40, 50, Celcius
            // Rain: 0, 50, 100, 150, 200, 250, 300, 350, 400, and 450.
#endregion

            // PLease make sure your coordinates are in Array Indexes and not a 2D graph

            {9, 9, 9, 0, 1, 2, 3, 3, 3, 3 }, // 450
            {9, 9, 9, 0, 1, 2, 3, 3, 3, 3 }, // 400
            {9, 9, 9, 0, 1, 2, 2, 3, 3, 3 }, // 350
            {9, 9, 9, 0, 1, 2, 2, 3, 3, 3 }, // 300
            {9, 9, 9, 0, 1, 2, 2, 8, 8, 3 }, // 250
            {9, 9, 9, 0, 6, 6, 6, 8, 8, 8 }, // 200
            {9, 9, 9, 0, 1, 6, 6, 8, 8, 8 }, // 150
            {9, 9, 10, 0, 1, 6, 5, 8, 8, 8 }, // 100
            {10, 10, 9, 0, 0, 5, 5, 5, 7, 7 }, // 50
            {10, 10, 10, 0, 4, 4, 7, 7, 7, 7 }, // 0
        };

        public enum Biome
        {
            Tundra, // 0
            BorealForest, // 1
            TemperateRainforest, // 2
            TropicalRainforest, // 3
            TemperateGrassland, // cold deserts --- 4
            Woodland, // AKA shrublands ---- 5
            TemperateSeasonalForest, // 6
            SubtropicalDesert, // 7
            TropicalSeasonalForest, // 8

            Polar, // 9
            PolarDesert, // 10

        };

        [System.Serializable]
        public struct BiomeProperties
        {
            public Biome Biome;
            public Color BiomeColor;            
        }

    }
}
