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
        public struct BiomeVisualData : IEquatable<BiomeVisualData>
        {
            public int BiomeHash { get; private set; }

            // you can either display the BiomeColor or the texture
            public Color BiomeColor { get; set; }
            public Texture2D SeasonTexture { get; set; }
            public Texture2D WeatherTexture { get; set; }

            public float WeatherLerp { get; set; } 
            public bool UseColor { get; private set; }

            // These constructores are set up in such a way that it forces the use to use either a BiomeColor or a texture
            // Of course there is also the option of adding both a texture and a color
            public BiomeVisualData(Color color)
            {
                BiomeHash = 0;
                
                BiomeColor = color;
                UseColor = true;
                
                SeasonTexture = null;
                WeatherTexture = null;
                
                WeatherLerp = 0;

                SetHashCode();
            }
            public BiomeVisualData(Texture2D seasonTexture)
            {
                BiomeHash = 0;

                BiomeColor = Color.white;
                UseColor = false;

                SeasonTexture = seasonTexture;
                WeatherTexture = null;

                WeatherLerp = 0;

                SetHashCode();
            }
            public BiomeVisualData(Texture2D seasonTexture, Texture2D weatherTexture, float lerp)
            {
                BiomeHash = 0;

                BiomeColor = Color.white;
                UseColor = false;

                SeasonTexture = seasonTexture;
                WeatherTexture = weatherTexture;

                WeatherLerp = lerp;

                SetHashCode();
            }
            public BiomeVisualData(Color color, Texture2D seasonTexture, Texture2D weatherTexture, float lerp, bool useColor)
            {
                BiomeHash = 0;

                BiomeColor = color;
                UseColor = useColor;

                SeasonTexture = seasonTexture;
                WeatherTexture = weatherTexture;

                WeatherLerp = lerp;

                SetHashCode();
            }

            /// <summary>
            /// This is used to set the UseColor variable. If true the hash will be changed to use the color, if false the hash will be changed to use the texture
            /// </summary>
            /// <param name="useColor"></param>
            public void SetUseColor(bool useColor)
            {
                UseColor = useColor;

                SetHashCode();
            }

            // this is primarily used to make check if two objects can be rendered together
            // because objects withsame textures and colors will have thesame hashcode
            public bool Equals(BiomeVisualData other)
            {
                if(other.BiomeHash == BiomeHash)
                {
                    return true;
                }

                return false;
            }         
            private void SetHashCode()
            {
                int hash = 0;

                if(UseColor)
                {
                    hash = BiomeColor.GetHashCode();
                }
                else
                {
                    if(WeatherLerp == 0)
                    {
                        hash = SeasonTexture.GetHashCode();
                    }
                    else
                    {
                        hash = HashCode.Combine(SeasonTexture, WeatherTexture, WeatherLerp);
                    }
                    
                }

                // this is in case season texture and Biome texture have thesame hash code
                hash = HashCode.Combine(hash, typeof(SurfaceBody));

                BiomeHash = hash;
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

            public enum BiomeVisualOption { Color, Season, SeasonWeather }

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

            public BiomeVisualData GetBiomeProperties(BiomeVisualOption options, float lerp = 0)
            {
                BiomeVisualData props;

                switch (options)
                {
                    case BiomeVisualOption.Color:
                        props = new BiomeVisualData(BiomeColor);
                        break;
                    case BiomeVisualOption.Season:
                        props = new BiomeVisualData(SeasonTexture);
                        break;
                    case BiomeVisualOption.SeasonWeather:
                        props = new BiomeVisualData(SeasonTexture, WeatherTexture, lerp);
                        break;
                    default:
                        // by default we use the color only
                        props = new BiomeVisualData(BiomeColor);
                        break;
                }

                return props;
            }
        }


    }
}
