using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.WorldMap.Biosphere
{
    [CreateAssetMenu(fileName = "BiomeDataStorage", menuName = "BiomeDataStorage", order = 1)] //
    public class BiomeDataStorage : ScriptableObject
    {
        [SerializeField] List<BiomeData> biomeData = new List<BiomeData>();

        private static readonly string SavePath = "Assets/Materials/BiomeDataStorage.asset";

        public Dictionary<Biomes, BiomeData> GetData()
        {
            Dictionary<Biomes, BiomeData> data = new Dictionary<Biomes, BiomeData>();

            foreach (BiomeData item in biomeData)
            {
                data.Add(item.Biome, item);
            }

            return data;
        }

        private void ExpandAllBiomes()
        {
            foreach (Biomes biome in Enum.GetValues(typeof(Biomes)))
            {
                if (!biomeData.Exists(x => x.Biome == biome))
                {
                    biomeData.Add(new BiomeData(biome));
                }
            }
        }

        private void UseTextureColors()
        {
            for (int i = 0; i < biomeData.Count; i++)
            {
                BiomeData data = biomeData[i];
                
                if (data.SeasonTexture != null)
                {
                    Color avgColor = AverageColorFromTexture(data.SeasonTexture);

                    BiomeData newData = new BiomeData(data.Biome, avgColor, data.SeasonTexture, data.WeatherTexture);

                    biomeData[i] = newData;
                }
            }
        }

        private Color AverageColorFromTexture(Texture2D tex)
        {
            Color[] texColors = tex.GetPixels();

            int total = texColors.Length;

            float r = 0;
            float g = 0;
            float b = 0;

            for (int i = 0; i < total; i++)
            {
                r += texColors[i].r;

                g += texColors[i].g;

                b += texColors[i].b;
            }

            return new Color((r / total), (g / total), (b / total), 1);

        }


#if UNITY_EDITOR

        [CustomEditor(typeof(BiomeDataStorage))]

        public class BiomeDataStorageEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                BiomeDataStorage dataStorage = (BiomeDataStorage)target;

                if (GUILayout.Button("Expand All Biomes"))
                {
                    dataStorage.ExpandAllBiomes();
                }

                if (GUILayout.Button("Use Season Color"))
                {
                    dataStorage.UseTextureColors();
                }
            }
        }
#endif
        
    }
}
