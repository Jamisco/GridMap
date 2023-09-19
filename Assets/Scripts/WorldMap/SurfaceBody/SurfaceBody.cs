using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.WorldMap.Planet;
using UnityEngine;
using static Assets.Scripts.WorldMap.PlanetGenerator;

namespace Assets.Scripts.WorldMap.Biosphere
{
    [System.Serializable]
    public abstract class SurfaceBody
    {
        protected Dictionary<int, BiomeProperties> biomeProperties { get; }

        public SurfaceBody()
        {
            biomeProperties = new Dictionary<int, BiomeProperties>();
        }

        public abstract BiomeProperties GetBiomeProperties(GridValues grid);
        
        public struct BiomeProperties : IEquatable<BiomeProperties>
        {
            public int BiomeHash { get; private set; }
            public Color BiomeColor { get; private set; }
            public Texture2D BiomeTexture { get; private set; }

            public BiomeProperties(int hash, Color color, Texture2D texture)
            {
                BiomeHash = hash;
                BiomeColor = color;
                BiomeTexture = texture;
            }

            public bool Equals(BiomeProperties other)
            {
                if(other.BiomeHash == BiomeHash)
                {
                    return true;
                }

                return false;
            }
        }
        
        //public void RandomizeBiome()
        //{
        //    biomes.Clear();

        //    foreach (var name in Enum.GetValues(biomeType))
        //    {
        //        BiomeProperties biome = new BiomeProperties();
        //        biome.Biome = (Enum)name;
        //        biome.Color = UnityEngine.Random.ColorHSV(0, 1, 0, 1, 0, 1, 1, 1);

        //        biomes.Add(biome);
        //    }
        //}

    }
}
