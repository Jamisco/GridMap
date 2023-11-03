using Assets.Scripts.Miscellaneous;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.Miscellaneous.HexFunctions;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.GridManager;
using static Assets.Scripts.WorldMap.Planet;
using Random = System.Random;

namespace Assets.Scripts.WorldMap
{
    public class HexTile
    {
        public static HexSettings hexSettings;

        /// <summary>
        /// The index is the respective i
        /// Will return a tuple of 3 ints,
        /// int 1 = x direction, int 2 = y direction, int 3 = x offset
        /// If you are on a even x axis, that is an offset, so add the offset number to the x
        /// </summary>
        public static (int, int, int)[] StepVertexModifier =
        {
             new (0, 1, 1),
             new (1, 0, 0),
             new (0, -1, 1),
             new (-1, -1, 1),
             new (-1, 0, 0),
             new (-1, 1, 1 ),
        };

        private Vector3 InnerVertexPosition(int i)
        {
            return hexSettings.VertexCorners[i];
        }

        private Vector3 OuterVertexPosition(int i)
        {
            return hexSettings.VertexCorners[i] * hexSettings.outerHexMultiplier;
        }

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
            Vector3 mod = new Vector3(temp.Item1, 0f, temp.Item2) * ((hexSettings.stepDistance / 2));

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
        /// A coordinate system for hexagonal Grids that uses three axes, X, Y, and Z. The X-axis points southeast, the Y-axis points south, and the Z-axis points southwest. The sum of the three axial coordinates should always be 0. Used to overcome the offset of the hexagonal Grid.
        /// </summary>
        public struct Axial : IEquatable<Axial>, IComparable<Axial>
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
            /// <param name="pos">Non Axial Coordinate to convert. The Y placeholds for the Z </param>
            /// <returns>returns a new Axial Class</returns>
            public static Axial ToAxial(Vector2Int pos)
            {
                Axial a = new Axial();
                a.X = pos.x - (pos.y - (pos.y & 1)) / 2;
                a.Y = -a.X - pos.y;
                a.Z = pos.y;
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

            public int CompareTo(Axial other)
            {
                int compareY = X.CompareTo(other.X);
                return compareY == 0 ? Z.CompareTo(other.Z) : compareY;
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
        /// Will return the position of the hex1 tile on the Grid map at the given coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 GetPosition(int x, float y, int z)
        {
            Vector3 position;
            // it looks thsame but i assure u, its differnt
            position.x = (x + (z * 0.5f) - (z / 2)) * (hexSettings.innerRadius * 2f) + (x * hexSettings.stepDistance);
            position.y = y;
            position.z = z * (hexSettings.outerRadius * 1.5f) + (z * hexSettings.stepDistance);

            return position;
        }

        public static Vector3 GetPosition(Vector3Int pos)
        {
            return GetPosition(pos.x, pos.y, pos.z);
        }


        private static float GetPositionZ(int z)
        {
            return z * (hexSettings.outerRadius * 1.5f) + (z * hexSettings.stepDistance);
        }

        /// <summary>
        /// Be aware that this function is only accurate 80% of the time. Due to the nature of the hex grid, there are some cases where the function will return the wrong value because of offsets. Thus is it recommended to also get the surrounding tiles and check if the tile is actually the closest one.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public static Vector2Int GetGridCoordinate(Vector3 worldPosition)
        {
            Vector3Int gridCoordinate = Vector3Int.zero;

            float z = worldPosition.z / (hexSettings.outerRadius * 1.5f);

            int minZ = Mathf.FloorToInt(z);
            int maxZ = Mathf.CeilToInt(z);

            int clozerZ = CloserZ();

            float x = worldPosition.x / (hexSettings.innerRadius * 2f);

            int minX = Mathf.FloorToInt(x);
            int maxX = Mathf.CeilToInt(x);

            int clozerX = CloserX();

            return new Vector2Int(clozerX, clozerZ);

            // we get the position of the 2 possible coordinates, then we compare the distance between the world position and the 2 coordinates
            // the one with the smallest distance is the closest coordinate to the world position
            int CloserZ()
            {
                float position = worldPosition.z;
                float val1Pos = GetPositionZ(minZ);
                float val2Pos = GetPositionZ(maxZ);

                float val1Distance = Mathf.Abs(val1Pos - position);
                float val2Distance = Mathf.Abs(val2Pos - position);

                if (val1Distance < val2Distance)
                {
                    return minZ;
                }
                else
                {
                    return maxZ;
                }
            }

            // we get the position of the 2 possible coordinates, then we compare the distance between the world position and the 2 coordinates
            // the one with the smallest distance is the closest coordinate to the world position
            // Since X position is dependent on Z position, we need to get the closest Z position first
            int CloserX()
            {
                float position = worldPosition.x;
                float val1Pos = GetPosition(minX, 0, clozerZ).x;
                float val2Pos = GetPosition(maxX, 0, clozerZ).x;

                float val1Distance = Mathf.Abs(val1Pos - position);
                float val2Distance = Mathf.Abs(val2Pos - position);

                if (val1Distance < val2Distance)
                {
                    return minX;
                }
                else
                {
                    return maxX;
                }
            }
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

        public Axial AxialCoordinates;
        public Vector2Int GridCoordinates;
        public Vector3 Position { get; set; }

        public List<Vector3> Vertices = new List<Vector3>(6);
        public List<int> Triangles = new List<int>(12);

        List<Vector3> SlopeVertices = new List<Vector3>(4);
        List<int> SlopeTriangles = new List<int>(6);

        public List<Vector2> SlopeUV = new List<Vector2>(28);

        Mesh mesh;
        
        public Vector2[] BaseUV;

        private BiomeData _hexBiomeData;
        public BiomeData HexBiomeData 
        {   
            get { return _hexBiomeData; }
            set { _hexBiomeData = value; }
        }

        public BiomeData DefaultBiomeData
        {
            get { return Planet.GetBiomeProperties(X, Y); }
            
        }

        /// <summary>
        /// Quick access to the X coordinates of the tile
        /// </summary>
        public int X { get { return GridCoordinates.x; } }

        /// <summary>
        /// Quick access to the Y coordinates of the tile
        /// </summary>
        public int Y { get { return GridCoordinates.y; } }


        public static GridManager Grid { get; set; }
        public static PlanetGenerator Planet;

        // We should pass the planet instead

        static Random random = new Random();
        public HexTile(int x, int z)
        {
            AxialCoordinates = Axial.ToAxial(x, z);
            GridCoordinates = new Vector2Int(x, z);

            float y = random.NextFloat(0, hexSettings.maxHeight);

            Position = GetPosition(x, y, z);

            CreateBaseMesh();
            //CreateOuterHexMesh();

            //InitiateDrawProtocol();

            SetBounds();

            try
            {
                _hexBiomeData = DefaultBiomeData;
            }
            catch (Exception)
            {
                // this exception is here just in case the planet arrays have not been computed yet
            }
        }

        public static void CreateSlopes(Dictionary<Axial, HexTile> hexes)
        {
            Parallel.ForEach(hexes.Values, (hexTile) =>
            {
                hexTile.CreateSlopeMesh();
            });

        }

        public static Dictionary<Axial, HexTile> CreatesHexes(Vector2Int MapSize, ref List<HexChunk> hexChunks)
        {
            Dictionary<Axial, HexTile> hexTiles = new Dictionary<Axial, HexTile>(MapSize.x * MapSize.y + 10);


            #region Parrallel forEach
            //Parallel.ForEach(hexChunks, chunk =>
            //{
            //    int chunkBoundsXMin = chunk.ChunkBounds.xMin;
            //    int chunkBoundsXMax = chunk.ChunkBounds.xMax;
            //    int chunkBoundsYMin = chunk.ChunkBounds.yMin;
            //    int chunkBoundsYMax = chunk.ChunkBounds.yMax;

            //    for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
            //    {
            //        for (int z = chunkBoundsYMin; z < chunkBoundsYMax; z++)
            //        {
            //            HexTile hc = new HexTile(x, z);

            //            hexTiles[hc.AxialCoordinates] = hc;

            //            chunk.AddHex(hc);
            //        }
            //    }
            //});

            #endregion

            #region Parrellel For

            //List<HexChunk> chunky = hexChunks;

            //Parallel.For(0, hexChunks.Count, chunkIndex =>
            //{
            //    HexChunk chunk = chunky[chunkIndex];
            //    int chunkBoundsXMin = chunk.ChunkBounds.xMin;
            //    int chunkBoundsXMax = chunk.ChunkBounds.xMax;
            //    int chunkBoundsYMin = chunk.ChunkBounds.yMin;
            //    int chunkBoundsYMax = chunk.ChunkBounds.yMax;

            //    for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
            //    {
            //        for (int z = chunkBoundsYMin; z < chunkBoundsYMax; z++)
            //        {
            //            HexTile hc = new HexTile(x, z);

            //            lock (hexTiles) // Ensure safe access to the dictionary
            //            {
            //                hexTiles[hc.AxialCoordinates] = hc;
            //            }

            //            chunk.AddHex(hc);
            //        }
            //    }
            //});

            #endregion


            // It seems using a standard loop is about 30% - 50% faster than using parrallel.
            // For a 1000 x 1000 grid
            // For loop: 7 seconds
            // Parrellel foreach/for 15 - 16 seconds

            for (int chunkIndex = 0; chunkIndex < hexChunks.Count; chunkIndex++)
            {
                HexChunk chunk = hexChunks[chunkIndex];
                int chunkBoundsXMin = chunk.ChunkBounds.xMin;
                int chunkBoundsXMax = chunk.ChunkBounds.xMax;
                int chunkBoundsYMin = chunk.ChunkBounds.yMin;
                int chunkBoundsYMax = chunk.ChunkBounds.yMax;

                for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
                {
                    for (int z = chunkBoundsYMin; z < chunkBoundsYMax; z++)
                    {
                        HexTile hc = new HexTile(x, z);

                        hexTiles[hc.AxialCoordinates] = hc;

                        chunk.AddHex(hc);
                    }
                }
            }

            return hexTiles;
        }

        public void CreateBaseMesh()
        {
            Triangles = hexSettings.BaseTrianges();

            Vertices = hexSettings.VertexCorners;

            BaseUV = hexSettings.BaseHexUV;
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
        }

        // To Do update this to use Y position
        public void CreateSlopeMesh()
        {
            if (hexSettings.stepDistance == 0 && hexSettings.maxHeight == 0)
            {
                // there are no slopes
                return;
            }

            SlopeVertices.Clear();
            SlopeTriangles.Clear();

            List<HexTile> surroundingHex = GetSurroundingHexes(this, Grid);

            for (int i = 0; i < 6; i++)
            {
                HexTile hex1 = surroundingHex[i];

                if (hex1 == null)
                {
                    continue;
                }

                // p1 and p2 denote the 2 points on the current i side in clockwise order

                int p1 = i;
                // so the last one loops back to the first one
                int p2 = (i + 1) % 6;

                Vector3 pos1 = Vertices[p1];
                Vector3 pos2 = Vertices[p2];

                // the current hex1's i vertices
                Vector3 pos3 = StepOuterVertexPosition(i) + Vertices[p1];
                Vector3 pos4 = StepOuterVertexPosition(i) + Vertices[p2];

                pos3.y -= (Position.y - hex1.Position.y) / 2;
                pos4.y -= (Position.y - hex1.Position.y) / 2;

                int sv = SlopeVertices.Count + Vertices.Count;

                SlopeVertices.Add(pos1); // sv + 0
                SlopeVertices.Add(pos2); // sv + 1

                SlopeVertices.Add(pos3); // sv + 2
                SlopeVertices.Add(pos4); // sv + 3

                SlopeTriangles.AddRange(new int[6] {
                        sv + 0, sv + 2, sv + 3,
                        sv + 0, sv + 3, sv + 1
                    });

                float heightDiff = Mathf.Abs(hex1.Position.y - Position.y);

                SlopeUV.AddRange(hexSettings.GetSlopeUV(heightDiff));

                // Check for next surrounding hex1
                // if it exist, add triangle
                // we do this bcuz there is a little gap between the 2 slope of the hex

                // next surrounding hex1
                int nextSide = (i + 1) % 6;

                HexTile hex2 = surroundingHex[nextSide];

                Vector3 center;
                Vector3 IPos3;

                if (hex2 != null && true)
                {
                    // hex2 connecting vertex
                    IPos3 = StepOuterVertexPosition(nextSide) + Vertices[p2];
                    IPos3.y -= (Position.y - hex2.Position.y) / 2;

                    Vector3 hex1Corner, hex2Corner;

                    // the corners of the surrounding hex, so we can get their centers
                    hex1Corner = StepOuterVertexPosition(i) * 2 + pos2;
                    hex2Corner = StepOuterVertexPosition(nextSide) * 2 + pos2;

                    hex1Corner.y -= (Position.y - hex1.Position.y);
                    hex2Corner.y -= (Position.y - hex2.Position.y);

                    center = GetTriangleCenter(pos2, hex1Corner, hex2Corner);

                    // sv + 1 = pos2 (current hex 2 vertex for slope)
                    // sv + 3 = pos4 (current hex last slope vertex)

                    SlopeVertices.Add(IPos3); // sv + 4
                    SlopeVertices.Add(center); // sv + 5

                    // SlopeUV.Add(new Vector2()

                    SlopeTriangles.AddRange(new int[6] {
                        sv + 1, sv + 3, sv + 4,
                        sv + 3, sv + 5, sv + 4
                    });

                    SlopeUV.AddRange(hexSettings.GetMidTriangleSlopeUV(heightDiff));
                }

                Vector3 GetTriangleCenter(Vector3 pos1, Vector3 pos2, Vector3 pos3)
                {
                    return (pos1 + pos2 + pos3) / 3f;
                }
            }
        }
        public static List<HexTile> GetSurroundingHexes(HexTile hex, GridManager grid)
        {
            List<HexTile> surroundingHexs = new List<HexTile>();

            for (int i = 0; i < 6; i++)
            {
                Axial sPos = hex.AxialCoordinates + SurroundingHexes[i];

                // will include null
                surroundingHexs.Add(grid.GetHexTile(sPos));
            }

            return surroundingHexs;
        }
        public Mesh DrawMesh()
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                return mesh;
            }

            //mesh.vertices = CombineVertices().ToArray();
            //mesh.triangles = CombineTriangles().ToArray();

            mesh.vertices = CombineVertices().ToArray();
            mesh.triangles = CombineTriangles().ToArray();
            mesh.uv = CombineUV().ToArray();

            return mesh;
        }

        public Color[] MeshColors()
        {
            Color[] colors = new Color[Vertices.Count];

            for (int i = 0; i < colors.Length; i++)
            {

                colors[i] = Planet.GetBiomeProperties(X, Y).BiomeColor;
            }

            return colors;
        }

        public void SetMaterialProperty()
        {
            if (mesh == null)
            {
                return;
            }
        }
    

        /// higher values the smaller the mapping wll be
        private float mul = 3;
        private Vector2 GetHexUV(Vector3 vertexPosition)
        {
            // Assuming hexagons are uniformly spaced in a Grid along the Z-axis
            // Calculate the relative position of the vertex within the hexagon
            float relativeX = vertexPosition.x / hexSettings.innerRadius;
            float relativeY = vertexPosition.y / (Position.y + hexSettings.outerRadius - hexSettings.innerRadius);
            float relativeZ = vertexPosition.z / (hexSettings.outerRadius);

            // Calculate UV coordinates based on the relative position
            // Using a custom range for UV coordinates based on the hexagon dimensions
            float uvX = (mul * relativeX);
            float uvY = (mul * relativeY);
            float uvZ = (mul * relativeZ);

            // Return the UV coordinate
            return new Vector2(uvX, uvZ);
        }
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
        List<Vector2> CombineUV()
        {
            List<Vector2> combinedUV = new List<Vector2>(hexSettings.BaseHexUV);
            combinedUV.AddRange(SlopeUV);

            return combinedUV;
        }
        public void ClearMesh()
        {
            Vertices.Clear();
            Triangles.Clear();
            SlopeVertices.Clear();
            SlopeTriangles.Clear();
        }
        public Vector3 GetWorldVertexPosition(int index)
        {
            return Vertices[index] + Position;
        }
        public int VertexCount()
        {
            return CombineVertices().Count;
        }

        double minx = 0;
        double maxx = 0;
        double miny = 0;
        double maxy = 0;
        private void SetBounds()
        {
            minx = Vertices[5].x;
            maxx = Vertices[2].x;
            miny = Vertices[0].y;
            maxy = Vertices[3].y;
        }
        public bool IsPointInPolygon(Vector3 p)
        {
            if (p.x < minx || p.x > maxx || p.y < miny || p.y > maxy)
            {
                return false;
            }

            // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
            
            bool inside = false;
            for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count; j = i++)
            {
                if ((Vertices[i].y > p.y) != (Vertices[j].y > p.y) &&
                     p.x < (Vertices[j].x - Vertices[i].x) * (p.y - Vertices[i].y) / (Vertices[j].y - Vertices[i].y) + Vertices[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GridCoordinates, AxialCoordinates, Position, mesh);
        }
    }
}
