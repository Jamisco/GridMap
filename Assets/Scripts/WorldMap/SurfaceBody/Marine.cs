using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.PlanetGenerator;

namespace Assets.Scripts.WorldMap.Biosphere
{
    [System.Serializable]
    public class Marine : SurfaceBody
    {
        public enum Biomes
        {
            Ocean,
            Sea,
            Lake,
        }
        public Marine()
        {
           
        }

        public override BiomeProperties GetBiomeProperties(GridValues grid)
        {
            return biomeProperties.First().Value;
        }

        public void Init()
        {
            ConvertData();
        }

        [SerializeField] private List<BiomeData> biomeData = new List<BiomeData>();
        private void ConvertData()
        {
            biomeProperties.Clear();

            foreach (BiomeData item in biomeData)
            {
                int hash = GetHash(item.biome);

                Color _color = item.color;
                Texture2D _texture = item.texture;

                biomeProperties.TryAdd(hash, new BiomeProperties(hash, _color, _texture));
            }
        }

        private int GetHash(Biomes biome)
        {
            string name = nameof(Terrestrial) + biome.ToString();

            return name.GetHashCode();
        }

        [System.Serializable]
        private struct BiomeData
        {
            public Biomes biome;
            public Color color;
            public Texture2D texture;
            public BiomeData(Biomes biome, Color color, Texture2D texture)
            {
                this.biome = biome;
                this.color = color;
                this.texture = texture;
            }
        }
    }
}
