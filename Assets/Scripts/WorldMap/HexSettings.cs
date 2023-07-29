using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.WorldMap
{
    [CreateAssetMenu(fileName = "HexSettings", menuName = "Hex/Settings", order = 1)]
    public class HexSettings : ScriptableObject
    {
        public float outerRadius = 10f;
        public float innerRadius;

        public float outerHexMultiplier = 1f;

        public float stepDistance;
        public float outerHexSize;
        public float maxHeight = 0f;

        public Color InnerHighlightColor;
        public Color OuterHighlightColor;

        /// <summary>
        /// The corners of the hex tile. Starting from the top center corner and going clockwise
        /// </summary>
        [NonSerialized] public Vector3[] VertexCorners;
        
        private void OnValidate()
        {
            innerRadius = outerRadius * 0.866025404f;

            VertexCorners = new Vector3[]
            {
                new Vector3(0f, 0f, outerRadius),
                new Vector3(innerRadius, 0f, 0.5f * outerRadius),
                new Vector3(innerRadius, 0f, -0.5f * outerRadius),
                new Vector3(0f, 0f, -outerRadius),
                new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
                new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
                new Vector3(0f, 0f, outerRadius)
            };
        }

        [MenuItem("Assets/Hex Settings")]
        public static void CreateMyAsset()
        {
            HexSettings asset = CreateInstance<HexSettings>();

            AssetDatabase.CreateAsset(asset, "Assets/Hex Settings.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
    }
}
