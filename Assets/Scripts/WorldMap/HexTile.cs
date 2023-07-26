using System;
using System.Collections;
using System.Collections.Generic;
using Roytazz.HexMesh;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Assets.Scripts.WorldMap
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexTile : MonoBehaviour
    {
        public static float outerRadius = 10f;
        public static float innerRadius = outerRadius * 0.866025404f;

        public static float outerHexMultiplier = 1f;
        public static float step = 1f;

        public float stepDistance;
        public float outerHexSize;

        /// <summary>
        /// The corners of the hex1 tile. Starting from the top center corner and going clockwise
        /// </summary>
        public static Vector3[] VertexCorners =
        {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(0f, 0f, outerRadius)
        };

        /// <summary>
        /// The index is the respective i
        /// Will return a tuple of 3 ints,
        /// int 1 = x direction, int 2 = y direction, int 3 = x offset
        /// If you are on a even x axis, that is an offset, so add the offset number to the x
        /// </summary>
        public static (int, int, int)[] StepVertexModifier = 
        {
            //
             new (0, 1, 1),
             new (1, 0, 0),
             new (0, -1, 1),
             new (-1, -1, 1),
             new (-1, 0, 0),
             new (-1, 1, 1 ),
        };


        public float height = 0f;

        private Vector3 InnerVertexPosition(int i)
        {
            return VertexCorners[i];
        }

        private Vector3 OuterVertexPosition(int i)
        {
            return VertexCorners[i] * outerHexMultiplier;
        }

        //private Vector3 StepInnerVertexPosition(int i)
        //{
        //    //return StepVertexModifier[i] * 3 + Vertices[i]; ;
        //}

        private Vector3 StepOuterVertexPosition(int i)
        {
            (int, int, int) temp = StepVertexModifier[i];

            if (IsOffset)
            {
                temp.Item1 += temp.Item3;
            }

            // we multiply step by 2 because, the step applies to both hexes
            // hex1 1 is shifted 3 units away, and hex1 is also shifted 3 units away
            // totaling 6
            Vector3 mod = new Vector3(temp.Item1, 0f, temp.Item2) * ((step/2) * outerHexMultiplier);

            return mod;
        }


        public bool IsOffset
        {
            get
            {
                return GridCoordinates.y % 2 == 0 ? false : true; 
            }
        }

        public static Axial[] SurroundingHexes =
        {
            new Axial(0, -1, 1),
            new Axial(1,-1, 0),
            new Axial(1, 0, -1),
            new Axial(0, 1, -1),
            new Axial(-1, 1, 0),
            new Axial(-1, 0, 1)
        };

        // will return the index of the corners that are required to form triangles for the slopes. Indexes are in clockwise order of the hex1 whose surrounding your using
        public static (int, int, int)[] OppositeCorners =
        {
            (4, 3, 5),
            (5, 4, 0),
            (0, 5, 1),
            (1, 0, 2),
            (2, 1, 3),
            (3, 2, 4)
        };

        /// <summary>
        /// A coordinate system for hexagonal grids that uses three axes, X, Y, and Z. The X-axis points southeast, the Y-axis points south, and the Z-axis points southwest. The sum of the three axial coordinates should always be 0. Used to overcome the offset of the hexagonal grid.
        /// </summary>
        public struct Axial : IEquatable<Axial>
        {
            public int X;
            public int Y;
            public int Z;

            public Axial(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            /// <summary>
            /// Converts a position (X, Y) to axial coordinates. Returns Axial Struct
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns>A new Axial Class</returns>
            public static Axial ToAxial(int x, int z)
            {
                Axial a = new Axial();

                a.X = x - (z / 2);
                a.Y = -a.X - z;
                a.Z = z;

                return a;
            }
            /// <summary>
            /// Converts a position to axial coordinates. Returns Axial Struct
            /// </summary>
            /// <param name="pos">Non Axial Coordinate to convert</param>
            /// <returns>returns a new Axial Class</returns>
            public static Axial ToAxial(Vector3Int pos)
            {
                Axial a = new Axial();
                a.X = pos.x - (pos.y - (pos.y & 1)) / 2;
                a.Y = pos.y;
                a.Z = -a.X - a.Y;
                return a;
            }

            /// <summary>
            /// Converts an Axial position to a Non Axial position
            /// </summary>
            /// <param name="axial"></param>
            /// <returns></returns>
            public static Vector3Int FromAxial(Axial axial)
            {
                int x = axial.X + ((axial.Y - (axial.Y & 1)) / 2);

                return new Vector3Int(x, axial.Y, 0);
            }

            public static bool operator ==(Axial coord1, Axial coord2)
            {
                return (coord1.X, coord1.Y) == (coord2.X, coord2.Y);
            }

            public static bool operator !=(Axial coord1, Axial coord2)
            {
                return (coord1.X, coord1.Y) != (coord2.X, coord2.Y);
            }
            bool IEquatable<Axial>.Equals(Axial other)
            {
                if (other.Coordinates == Coordinates)
                {
                    return true;
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                if (obj != null)
                {
                    Axial axe = (Axial)obj;

                    if (axe != null)
                    {
                        if (axe.Coordinates == Coordinates)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            public override int GetHashCode()
            {
                return Coordinates.GetHashCode();
            }

            public override string ToString()
            {
                return Coordinates.ToString();
            }

            public static Axial operator +(Axial coord1, Axial coord2)
            {
                return (Axial)(coord1.Coordinates + coord2.Coordinates);
            }

            public static Axial operator -(Axial coord1, Axial coord2)
            {
                return (Axial)(coord1.Coordinates - coord2.Coordinates);
            }

            public static explicit operator Axial(Vector3Int v)
            {
                return new Axial(v.x, v.y, v.z);
            }

            /// <summary>
            /// Return the axial coordinates as a Vector3Int
            /// </summary>
            public Vector3Int Coordinates { get { return new Vector3Int(X, Y, Z); } }
        }

        /// <summary>
        /// Will return the position of the hex1 tile on the grid map at the given coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 GetPosition(int x, int z)
        {
            Vector3 position;
            position.x = (x + (z * 0.5f) - (z / 2)) * (HexMetrics.innerRadius * 2f) + (x * step);
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f) + (z * step);

            return position;
        }

        /// <summary>
        /// Creates the four triangles that fill up a hexagon.
        /// Used for mesh creation
        /// </summary>
        /// <param name="vertexCount"> This is the current number of vertices you have </param>
        /// <returns></returns>
        public static int[] SetInnerTriangles(int vertexCount)
            => new int[12] {
            4 + vertexCount, 5 + vertexCount, 0 + vertexCount,
            4 + vertexCount, 0 + vertexCount, 1 + vertexCount,
            4 + vertexCount, 1 + vertexCount, 2 + vertexCount,
            4 + vertexCount, 2 + vertexCount, 3 + vertexCount
        };

        /// <summary>
        /// Will return the outer triangles for each i of the outer hexagon
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex in the vertices array</param>
        /// <returns></returns>
        public static int[] SetOuterTriangles(int vertexIndex)
            => new int[6] {
            vertexIndex % 6, vertexIndex, vertexIndex == 11 ? 6 : vertexIndex + 1,
            vertexIndex % 6, vertexIndex == 11 ? 6 : vertexIndex + 1, (vertexIndex + 1) % 6
        };

        //public static int[] SetInnerTriangles(int vertexCount) =>
        //    new int[12] {
        //0 + vertexCount, 5 + vertexCount, 4 + vertexCount,
        //1 + vertexCount, 0 + vertexCount, 4 + vertexCount,
        //2 + vertexCount, 1 + vertexCount, 4 + vertexCount,
        //3 + vertexCount, 2 + vertexCount, 4 + vertexCount
        //};

        public Axial AxialCoordinates;
        public Vector2Int GridCoordinates;
        public Vector3 Position { get; set; }

        public Color InnerHexColor;
        public Color OuterHexColor;
        public Color SlopeColor;
        public Color HighlightColor;

        Mesh mesh;
        MeshCollider meshCollider;

        List<Vector3> Vertices;
        List<int> Triangles;
        List<Color> colors;

        List<Vector3> SlopeVertices;
        List<int> SlopeTriangles;
        List<Color> SlopeColors;

        public GridManager Grid { get; set; }

        public float elevation = 0f;

        public static void SetStaticVariables(float stepDistance, float outerHexSize)
        {
            step = stepDistance;
            outerHexMultiplier = outerHexSize;
        }

        private void Awake()
        {
            Vertices = new List<Vector3>(6);
            Triangles = new List<int>(12);
            colors = new List<Color>(6);

            step = stepDistance;
            outerHexMultiplier = outerHexSize;

            SlopeVertices = new List<Vector3>(4);
            SlopeTriangles = new List<int>(6);
            SlopeColors = new List<Color>(4);

            mesh = GetComponent<MeshFilter>().mesh;
            meshCollider = GetComponent<MeshCollider>();
        }

        public void Initialize(GridManager grid, int x, int z)
        {
            Grid = grid;
            
            AxialCoordinates = Axial.ToAxial(x, z);
            GridCoordinates = new Vector2Int(x, z);

            float y = UnityEngine.Random.Range(0, 00);
            height = y;

            Position = GetPosition(x, z) * outerHexMultiplier + new Vector3(0,y,0);

            transform.localPosition = Position;
        }

        public void CreateMesh()
        {
            mesh.Clear();
            Vertices.Clear();
            Triangles.Clear();
            colors.Clear();

            Triangles.AddRange(SetInnerTriangles(Vertices.Count));

            // Add the vertices
            for (int i = 0; i < 6; i++)
            {
                Vertices.Add(InnerVertexPosition(i));
            }

            AddColors(InnerHexColor);

            CreateOuterHexMesh();
        }

        /// <summary>
        /// Will create an outer hex1 surrounding the inner hex1. This should be called immediatel after creating the inner hex1
        /// </summary>
        private void CreateOuterHexMesh()
        {
            for (int i = 0; i < 6; i++)
            {
                Vertices.Add(OuterVertexPosition(i));
            }

            for (int i = 0; i < 6; i++)
            {
                Triangles.AddRange(SetOuterTriangles(i + 6));
            }

            AddColors(OuterHexColor);
        }
        
        private Vector3 InnerHexDiff()
        {
            float length = Mathf.Abs( Vector3.Distance(Vertices[0], Vertices[6]));

            float shift = length / Mathf.Sqrt(2);

            return new Vector3(shift, 0, shift);
        }

        public void CreateSlopeMesh()
        {
            // slope mesh will start from outer hex1 vertices and slope towards inner hex1 vertice
            int[] sideLink = { 5, 0, 1 };
            
            SlopeVertices.Clear();
            SlopeTriangles.Clear();
            SlopeColors.Clear();

            List<HexTile> surroundingHex = GetSurroundingHexes();

            for (int i = 0; i < 6; i++)
            {
                HexTile hex1 = surroundingHex[i];

                if (hex1 == null)
                {
                    continue;
                }

                (int, int, int) data = OppositeCorners[i];

                // p1 and p2 denote the 2 points on the current i side in clockwise order

                int p1 = i;
                // so the last one loops back to the first one
                int p2 = (i + 1) % 6;

                // this is the index of the outer hex1's in vertices array
                p1 += 6;
                p2 += 6;

                // the current hex1's i vertices
                Vector3 pos1 = StepOuterVertexPosition(i) + Vertices[p1];
                Vector3 pos2 = StepOuterVertexPosition(i) + Vertices[p2];

                pos1.y -= (Position.y - hex1.Position.y) / 2;
                pos2.y -= (Position.y - hex1.Position.y) / 2;

                int sv = SlopeVertices.Count + Vertices.Count;

                SlopeVertices.Add(pos1);
                SlopeVertices.Add(pos2);

                SlopeTriangles.AddRange(new int[6] {
                        p1, sv + 0, sv + 1,
                        p1, sv + 1, p2
                    });

                SlopeColors.Add(SlopeColor);
                SlopeColors.Add(SlopeColor);

                // Check for next surrounding hex1
                // if it exist, add triangle
                // we do this bcuz there is a little gap between the slope and the next hex1

                // next surrounding hex1
                int nextSide = (i + 1) % 6;

                HexTile hex2 = surroundingHex[nextSide];

                Vector3 IPos3 = Vector3.positiveInfinity;

                if (hex2 != null)
                {
                    // we multiply by 2 to get the exact position and not just half position
                    // this gets the outerhex position of surround hex1 1
                    IPos3 = StepOuterVertexPosition(nextSide) + Vertices[p2];

                    SlopeVertices.Add(IPos3);

                    SlopeTriangles.AddRange(new int[3] {
                                p2, sv + 1, sv + 2,
                            });

                    SlopeColors.Add(SlopeColor);
                }
            }
        }

        /// <summary>
        /// Checks if the given hex1 is the highest of all its surrounding hexes
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public bool IsHighest()
        {
            List<HexTile> surroundingHex = GetSurroundingHexes();

            foreach (HexTile hex in surroundingHex)
            {
                if (hex == null)
                {
                    continue;
                }

                if (Position.y < hex.Position.y)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddSlopeColors()
        {

        }

        public void RefreshMesh()
        {
            DrawMesh();
        }

        public List<HexTile> GetSurroundingHexes()
        {
            List<HexTile> surroundingHexs = new List<HexTile>();

            for (int i = 0; i < 6; i++)
            {
                Axial sPos = AxialCoordinates + SurroundingHexes[i];

                // will include null
                surroundingHexs.Add(Grid.GetHexTile(sPos));
            }

            return surroundingHexs;
        }
        
        /// <summary>
        /// Will return the position of a corner in the inner hex1
        /// </summary>
        /// <param name="index">Input a range from 0 - 5 denoting the i </param>
        /// <returns></returns>
        public Vector3 GetInnerVertexPosition(int index)
        {
            return Vertices[index];
        }

        /// <summary>
        /// Will return the position of a corner in the outer hex1
        /// </summary>
        /// <param name="index">Input a range from 0 - 5 denoting the i </param>
        /// <returns></returns>
        public Vector3 GetOuterCornerPosition(int index)
        {
            return Vertices[index + 6];
        }



        public void DrawMesh(Vector3 position = default)
        {
            mesh.vertices = CombineVertices().ToArray();
            mesh.triangles = CombineTriangles().ToArray();
            meshCollider.sharedMesh = mesh;
            mesh.colors = CombineColors().ToArray();
            SetHexMeshUVs();
            mesh.RecalculateNormals();

        }

        public void SetHexMeshUVs()
        {
            List<Vector3> vertices = CombineVertices();

            Vector2[] uv = new Vector2[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                // Calculate UV coordinates based on the position of the vertex
                uv[i] = GetHexUV(vertices[i]);
            }

            mesh.uv = uv;
        }

        private Vector2 GetHexUV(Vector3 vertexPosition)
        {
            // Assuming hexagons are uniformly spaced in a grid along the Z-axis
            // Calculate UV coordinates based on the Z-position
            float uvX = vertexPosition.x; // Use X position as UV X coordinate
            float uvY = vertexPosition.z; // Use Z position as UV Y coordinate

            // Normalize the UV coordinates to [0, 1]
            uvX /= (2f * Mathf.PI); // Assuming the hexagons have a radius of 1
            uvY /= (2f * Mathf.PI); // Assuming the hexagons have a radius of 1

            // Return the UV coordinate
            return new Vector2(uvX, uvY);
        }

        private static readonly Vector2[] hexUVs = new Vector2[]
        {
            new Vector2(0.5f, 1f),   // Top vertex
            new Vector2(0f, 0.75f),  // Bottom-left vertex
            new Vector2(0f, 0.25f),  // Top-left vertex
            new Vector2(0.5f, 0f),   // Bottom vertex
            new Vector2(1f, 0.25f),  // Bottom-right vertex
            new Vector2(1f, 0.75f),  // Top-right vertex
        };

        List<Vector3> CombineVertices()
        {
            List<Vector3> combinedVertices = new List<Vector3>(Vertices);
            combinedVertices.AddRange(SlopeVertices);

            return combinedVertices;
        }

        List<int> CombineTriangles()
        {
            List<int> combinedTriangles = new List<int>(Triangles);
            combinedTriangles.AddRange(SlopeTriangles);

            return combinedTriangles;
        }

        List<Color> CombineColors()
        {
            List<Color> combinedColors = new List<Color>(colors);
            combinedColors.AddRange(SlopeColors);

            return combinedColors;
        }

        public void DeleteHex()
        {
            Destroy(gameObject);
        }

        public void HighlightHex()
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                colors[i] = HighlightColor;
            }

            mesh.colors = CombineColors().ToArray();
            mesh.RecalculateNormals();
        }

        public void ResetColor()
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                colors[i] = InnerHexColor;
            }

            mesh.colors = CombineColors().ToArray();
            mesh.RecalculateNormals();
        }

        private void AddColors(Color color)
        {
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }
    }
}
