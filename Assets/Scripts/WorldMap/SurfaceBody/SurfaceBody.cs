using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.WorldMap.Planet;
using UnityEngine;
using static Assets.Scripts.WorldMap.PlanetGenerator;
using Unity.VisualScripting;

namespace Assets.Scripts.WorldMap.Biosphere
{
    [System.Serializable]
    public abstract class SurfaceBody
    {
        protected static Dictionary<Biomes, BiomeData> BiomeDataList { get; private set; }

        public static void SetBiomeData(Dictionary<Biomes, BiomeData> data)
        {
            BiomeDataList = data;
        }

        // Since different surface types will use different gridvalues, those specific classes should be the one to implement this method
        public abstract BiomeData GetBiomeData(GridValues grid);
        
        public virtual BiomeData GetBiomeData(Biomes biome)
        {
            try
            {
                return BiomeDataList[biome];
            }
            catch (Exception)
            {

                return new BiomeData(biome);
            }
            
        }
       
        // the order and numbers of these biomes matter, do not change
        public enum Biomes
        {
            Tundra = 0, // 0
            BorealForest = 1, // 1
            TemperateRainforest = 2, // 2
            TropicalRainforest = 3, // 3
            TemperateGrassland = 4, // cold deserts --- 4
            Woodland = 5, // AKA shrublands ---- 5
            TemperateSeasonalForest = 6, // 6
            SubtropicalDesert = 7, // 7
            TropicalSeasonalForest = 8, // 8
            Polar = 9, // 9
            PolarDesert = 10, // 10
            
            Ocean,
            Sea,
            Lake,
        };

        [Serializable]
        public struct BiomeData
        {
            [SerializeField] private Biomes _biome;
            [SerializeField] private Color _biomeColor;
            [SerializeField] private Texture2D _seasonTexture;
            [SerializeField] private Texture2D _weatherTexture;
            public Biomes Biome { get => _biome;}
            public Color BiomeColor { get => _biomeColor;  }
            public Texture2D SeasonTexture { get => _seasonTexture; }
            public Texture2D WeatherTexture { get => _weatherTexture;}

            public BiomeData(Biomes aBiome)
            {
                _biome = aBiome;
                _biomeColor = Color.white;
                _seasonTexture = null;
                _weatherTexture = null;
            }
            public BiomeData(Biomes biome, Color color,
                Texture2D seasonTexture, Texture2D weatherTexture)
            {
                _biome = biome;
                _biomeColor = color;
                _seasonTexture = seasonTexture;
                _weatherTexture = weatherTexture;
            }

            public void SetBiomeColor(Color color)
            {
                _biomeColor = color;
            }

            public void SetSeasonTexture(Texture2D texture)
            {
                _seasonTexture = texture;
            }

            public void SetWeatherTexture(Texture2D texture)
            {
                _weatherTexture = texture;
            }

            public void SetBiome(Biomes biome)
            {
                _biome = biome;
            }
        }


    }
}
