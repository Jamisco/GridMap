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

        public Vector2 HexSize { get; private set; }
        /// <summary>
        /// The corners of the hex tile. Starting from the top center corner and going clockwise
        /// </summary>
        [NonSerialized] public List<Vector3> VertexCorners;
        
        private void Awake()
        {
            OnValidate();
        }
        private void OnValidate()
        {
            innerRadius = outerRadius * 0.866025404f;

            VertexCorners = new List<Vector3>
            {
                new Vector3(0f, 0f, outerRadius),
                new Vector3(innerRadius, 0f, 0.5f * outerRadius),
                new Vector3(innerRadius, 0f, -0.5f * outerRadius),
                new Vector3(0f, 0f, -outerRadius),
                new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
                new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
               // new Vector3(0f, 0f, outerRadius)
            };

            HexSize = new Vector2(outerRadius * 2f + stepDistance, innerRadius * 2f + stepDistance);
        }

        public Vector2[] BaseHexUV
        {
            get
            {
                return new Vector2[]
                {
                    new Vector2(0.5f, 1),
                    new Vector2(1, 0.75f),
                    new Vector2(1, 0.25f),
                    new Vector2(0.5f, 0),
                    new Vector2(0, 0.25f),
                    new Vector2(0, 0.75f)
                };
            }
        }
        public Vector2[] GetSlopeUV(float height)
        {
            // this can be improved... by adding to the base UV
            Vector2[] slopes = SlopeHexUV;

            for (int i = 0; i < slopes.Length; i++)
            {
                slopes[i].y
                    *= CalculateHypotenuse(height / 2, stepDistance / 2) / innerRadius;
            }

            return slopes;
        }
        public float CalculateHypotenuse(float sideA, float sideB)
        {
            // Calculate the length of the hypotenuse using the Pythagorean theorem
            float hypotenuse = Mathf.Sqrt(sideA * sideA + sideB * sideB);
            return hypotenuse;
        }
        public Vector2[] GetMidTriangleSlopeUV(float height)
        {
            Vector2[] slopes = GetSlopeUV(height);

            // index 1 = bottom right
            // index 3 = top right

            /// This works by getting the UV of the slopes,
            /// calculating the sizes and adding them.... i recommend u comment out code to see how they work.
            /// 

            List<Vector2> newSlopes = new List<Vector2>();

            float increment = Mathf.Abs(slopes[1].y - slopes[3].y);

            Vector2 slopeTop = slopes[3];
            
            Vector2 slopeRight = new Vector2(slopes[1].x + increment, slopes[1].y);
            Vector2 slopeRightTop = new Vector2(slopeRight.x, slopeRight.y + increment);

            Vector2 midSlope = (slopeTop + slopeRightTop + slopeRight) / 3f;

            newSlopes.Add(slopeRight);
            newSlopes.Add(midSlope);

            //for (int i = 0; i < slopes.Length; i++)
            //{
            //    slopes[i].y
            //        *= CalculateHypotenuse(height / 2, stepDistance / 2) / outerRadius;
            //}

            return newSlopes.ToArray();
        }
        public Vector2[] SlopeHexUV
        {
            get
            {
                // this is from top left to top right
                // to bottom left to right
                return new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                };
            }
        }
       
        public List<int> BaseTrianges() => new List<int>
        {
            4, 5, 0,
            4, 0, 1,
            4, 1, 2,
            4, 2, 3 
        };

#if UNITY_EDITOR

        [MenuItem("Assets/Hex Settings")]
        public static void CreateMyAsset()
        {
            HexSettings asset = CreateInstance<HexSettings>();

            AssetDatabase.CreateAsset(asset, "Assets/Hex Settings.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
#endif
    }

}
