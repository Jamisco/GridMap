using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
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

        [Tooltip("The size of the edges when highlighting." +
            " This is as a percentage of space to occupy ")]
        [Range(.05f, .5f)]
        public float innerHexSize;

        [Tooltip("The size of the border when highlighting." +
                 " This is as a percentage of space to occupy ")]
        [Range(.01f, .1f)]
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

            InnerHighlighter = null;
            HexSize = new Vector2(outerRadius * 2f + stepDistance, innerRadius * 2f + stepDistance);
        }

        public void ResetVariables()
        {
            OnValidate();
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

        private Mesh InnerHighlighter;
        public Mesh GetInnerHighlighter()
        {
            if (InnerHighlighter != null)
            {
                return InnerHighlighter;
            }

            // we need to create 2 hexes, 1 hex is going to be the default hex and another is going to be an inner hex... we link the vertex of the sides respectively and draw triangles

            List<Vector3> outerVerts = new List<Vector3>();
            List<Vector3> innerVerts = new List<Vector3>();
            List<int> edgeTriangles = new List<int>();

            outerVerts = VertexCorners.ToList();

            for (int i = 0; i < 6; i++)
            {
                innerVerts.Add(outerVerts[i] * (1 - innerHexSize));
            }

            // now we for each side on both vertex, draw triangles linking them

            for (int i = 0; i < 6; i++)
            {
                edgeTriangles.AddRange(SetInnerTriangles(i));
            }

            InnerHighlighter = new Mesh();

            outerVerts.AddRange(innerVerts);
            InnerHighlighter.vertices = outerVerts.ToArray();
            InnerHighlighter.triangles = edgeTriangles.ToArray();

            SetMeshColors(ref InnerHighlighter, InnerHighlightColor);

            return InnerHighlighter;

            int[] SetInnerTriangles(int vertexIndex) => new int[6]
            {
                vertexIndex % 6, (vertexIndex + 1) % 6, vertexIndex == 5 ? 6 : vertexIndex + 7,
                vertexIndex + 6, vertexIndex, vertexIndex == 5 ? 6 : vertexIndex + 7
            };
        }

        private Mesh BaseOuterHighlighter;
        public Mesh GetOuterHighlighter(int[] sides)
        {
            Mesh baseOuter = GetBaseOuterHighlighter();

            List<int> triangles = new List<int>();

            sides = sides.Distinct().ToArray();

            foreach (int side in sides)
            {
                // a hex only has 6 sides, indexed 0 - 5
                if (side > 5)
                {
                    continue;
                }
                triangles.AddRange(SetInnerTriangles(side));
            }

            baseOuter.triangles = triangles.ToArray();

            SetMeshColors(ref baseOuter, OuterHighlightColor);

            return baseOuter;

            Mesh GetBaseOuterHighlighter()
            {
                // this will create 2 hexes, an inner and outer hex,
                // you then have to manually draw in the sides want
                if (BaseOuterHighlighter != null)
                {
                    return BaseOuterHighlighter.CloneMesh();
                }

                List<Vector3> outerVerts = new List<Vector3>();
                List<Vector3> innerVerts = new List<Vector3>();

                outerVerts = VertexCorners.ToList();

                for (int i = 0; i < 6; i++)
                {
                    innerVerts.Add(outerVerts[i] * (1 - outerHexSize));
                }

                BaseOuterHighlighter = new Mesh();

                outerVerts.AddRange(innerVerts);

                BaseOuterHighlighter.vertices = outerVerts.ToArray();

                SetMeshColors(ref BaseOuterHighlighter, OuterHighlightColor);

                return BaseOuterHighlighter;

            }

            int[] SetInnerTriangles(int vertexIndex) => new int[6]
            {
                vertexIndex % 6, (vertexIndex + 1) % 6, vertexIndex == 5 ? 6 : vertexIndex + 7,
                vertexIndex + 6, vertexIndex, vertexIndex == 5 ? 6 : vertexIndex + 7
            };
        }

        private void SetMeshColors(ref Mesh aMesh, Color color)
        {
            Color[] colors = new Color[aMesh.vertexCount];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            aMesh.colors = colors.ToArray();
        }

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
