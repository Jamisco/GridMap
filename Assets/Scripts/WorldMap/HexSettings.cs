using Assets.Scripts.Miscellaneous;
using Newtonsoft.Json.Linq;
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
        [Tooltip("The Distance from Center to Corner(straight line)  of the Hex")]
        public float outerRadius = 10f;

        [Tooltip("The Distance from Center to Edge(points/Vertexes) of the Hex")]
        public float innerRadius;

        public float outerHexMultiplier = 1f;

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
            HexSize = new Vector2(outerRadius * 2f, innerRadius * 2f);
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

            InnerHighlighter.SetFullColor(InnerHighlightColor);

            return InnerHighlighter;

            int[] SetInnerTriangles(int vertexIndex) => new int[6]
            {
                vertexIndex % 6, (vertexIndex + 1) % 6, vertexIndex == 5 ? 6 : vertexIndex + 7,
                vertexIndex + 6, vertexIndex, vertexIndex == 5 ? 6 : vertexIndex + 7
            };
        }

        /// <summary>
        /// The number of vertices per side of border. Use this to color in the triangles in groups
        /// </summary>
        public static int BorderSideVertexCount = 6;
        private Mesh BaseOuterHighlighter;

        /// <summary>
        /// Creates a schematic for a base border highlighter and returns a new mesh. You have to manually draw/activate the border sides you want
        /// </summary>
        /// <returns></returns>
        public Mesh GetBaseOuterHighlighter()
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

            // The base highlighter will have 2 vertices per position
            // this is because we want each side to be able to draw its own color.
            // Unity only allows 1 color per vertex, but since the sides/border share vertices, we need to create 2 vertices per side, and assign them unique colors
            outerVerts.AddRange(VertexCorners);
            outerVerts.AddRange(innerVerts);

            BaseOuterHighlighter.vertices = outerVerts.ToArray();
            BaseOuterHighlighter.SetFullColor(Color.white);

            return BaseOuterHighlighter.CloneMesh();
        }

        public void AddOuterHighlighter(Mesh outerMesh, int[] sides, Color[] colors)
        {
            if (sides == null || colors == null)
            {
                throw new ArgumentNullException("sides and colors arrays must not be null");
            }

            if (sides.Length != colors.Length)
            {
                throw new ArgumentException("sides and colors arrays must be the same length");
            }

            List<int> triangles = outerMesh.triangles.ToList();
            List<Color> meshColors = outerMesh.colors.ToList();


            int index = -1;
            int[] vertTriangles = new int[3];

            int count = -1;

            // get the vertices of the triangles that make up the sides
            // loop through each side and get the list of vertices that make up the triangles
            // we then find the index of said vertices in the original triangles array, to determine if the border mesh already exists
            // and remove  them from the triangles array of the outerMesh
            foreach (int side in sides)
            {
                count++;
                // a hex only has 6 sides, indexed 0 - 5
                if (side > 5 || side < 0)
                {
                    continue;
                }

                vertTriangles = GetBorderTriangles(side);

                index = triangles.FindIndex(vertTriangles);

                if (index == -1)
                {
                    Color sideColor = colors[count];
                    triangles.AddRange(vertTriangles);

                    // set the color of the vertices we just added
                    foreach (int vert in vertTriangles)
                    {
                        meshColors[vert] = sideColor;
                    }
                }
            }

            outerMesh.triangles = triangles.ToArray();
            outerMesh.colors = meshColors.ToArray();

        }

        public void RemoveOuterHighlighter(Mesh outerMesh, int[] sides)
        {
            List<int> triangles = outerMesh.triangles.ToList();
            List<Color> sideColors = outerMesh.colors.ToList();

            sides = sides.Distinct().ToArray();

            int index = -1;
            int[] vertTriangles = new int[3];

            Exception tempException = new Exception();

            // get the vertices of the triangles that make up the sides
            // loop through each side and get the list of vertices that make up the triangles
            // we then find the index of said vertices in the original triangles array, to determine if the border mesh already exists
            // and remove  them from the triangles array of the outerMesh
            foreach (int side in sides)
            {
                // a hex only has 6 sides, indexed 0 - 5
                if (side > 5 || side < 0)
                {
                    continue;
                }

                vertTriangles = GetBorderTriangles(side);

                index = triangles.FindIndex(vertTriangles);

                if (index == -1)
                {
                    continue;
                }

                // each side has 2 triangles, with 3 vertices per triangle, so we remove 6 elements
                triangles.TryRemoveElementsInRange(index,
                    vertTriangles.Length, out tempException);
            }

            outerMesh.triangles = triangles.ToArray();
        }

        /// <summary>
        /// Will either add or remove the triangles of border mesh
        /// </summary>
        /// <param name="outerMesh"></param>
        /// <param name="sides"></param>
        /// <param name="add"></param>
        private void ModifyOuterHighlighter(Mesh outerMesh, int[] sides,
                                                             bool add)
        {
            
        }





        public int[] GetBorderTriangles(int vertexIndex)
        {
            int[] baseTriangles = SetBorderTriangles(vertexIndex);

            // the below offsets were manually calculated
            int[] offset1 = { 0, 0, 0, 0, 0, 0 }; // applies when vIndex = 0
            int[] offset2 = { 12, 0, 0, 12, 12, 0 }; // applies when vindex = 1,2,3 4
            int[] offset3 = { 12, 12, 12, 12, 12, 12 }; // applies when vIndex = 5

            if (vertexIndex == 0)
            {
                AddArray(baseTriangles, offset1);
            }
            else if (vertexIndex < 5)
            {
                AddArray(baseTriangles, offset2);
            }
            else
            {
                // vertexIndex = 6
                AddArray(baseTriangles, offset3);
            }

            return baseTriangles;

            void AddArray(int[] baseArray, int[] offset)
            {
                for (int i = 0; i < baseArray.Length; i++)
                {
                    baseArray[i] = baseArray[i] + offset[i];
                }
            }

            int[] SetBorderTriangles(int vertexIndex) => new int[6]
            {
                vertexIndex % 6, (vertexIndex + 1) % 6, vertexIndex == 5 ? 6 : vertexIndex + 7,
                vertexIndex + 6, vertexIndex, vertexIndex == 5 ? 6 : vertexIndex + 7
            };
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
