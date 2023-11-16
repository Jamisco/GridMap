using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.WorldMap.PlanetGenerator;
using static FastNoiseLite;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.WorldMap
{
    internal class MaskTextureGenerator : MonoBehaviour
    {

        private static string TextureSavePath = "Assets/Textures/MaskTextures/";
        FastNoiseLite noiseMap = new FastNoiseLite();
        [SerializeField] private FractalType fractal = FractalType.None;
        [SerializeField] private NoiseType noiseType = NoiseType.Perlin;

        [Range(0, 1)]
        [SerializeField] private float whiteCutOff = .5f;

        public Vector2Int size;

        GridManager gridManager;

        

        public void GenerateColors()
        {
            

            noiseMap.SetNoiseType(noiseType);
            noiseMap.SetFractalType(fractal);

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {

                }
            }
        }

        
        public Texture2D GenerateMaskTexture()
        {
            Texture2D maskTexture = new Texture2D(size.x, size.y);

            noiseMap.SetNoiseType(noiseType);
            noiseMap.SetFractalType(fractal);

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {   

                }
            }

            maskTexture.Apply();

            int count = 0;

            count = System.IO.Directory.GetFiles(TextureSavePath).Length;

            string name = "MaskTexture" + count + ".png";

            SaveTextureAsPNG(maskTexture, TextureSavePath + name);

            return maskTexture;
        }
        private void SaveTextureAsPNG(Texture2D texture, string filePath)
        {
            byte[] pngBytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filePath, pngBytes);
            Debug.Log("Texture saved as PNG to: " + filePath);
        }
    }

#if UNITY_EDITOR

    // create custom editor class

    [CustomEditor(typeof(MaskTextureGenerator))]

    public class MaskTextureEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MaskTextureGenerator myScript = (MaskTextureGenerator)target;

            if (GUILayout.Button("Generate Mask"))
            {
                myScript.GenerateMaskTexture();
            }
        }
    }

#endif
}
