using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using static Assets.Scripts.WorldMap.GridManager;
using Random = System.Random;

namespace Assets.Scripts.WorldMap
{
    public class HexTile
    {
        public HexSettings hexSettings;

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

        /// <summary>
        /// Is true when the Y position of the coordinates is odd
        /// </summary>
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
            public static Vector2Int FromAxial(Axial axial)
            {
                int x = axial.X + ((axial.Y - (axial.Y & 1)) / 2);

                return new Vector2Int(x, axial.Y);
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
        public static Vector3 GetPosition(int x, float y, int z, HexSettings hexSettings)
        {
            Vector3 position;
            // it looks thsame but i assure u, its differnt
            position.x = (x + (z * 0.5f) - (z / 2)) * (hexSettings.innerRadius * 2f) + (x * hexSettings.stepDistance);
            position.y = y;
            position.z = z * (hexSettings.outerRadius * 1.5f) + (z * hexSettings.stepDistance);

            return position;
        }

        public static Vector3 GetPosition(Vector2Int pos, HexSettings hexSettings)
        {
            return GetPosition(pos.x, 0, pos.y, hexSettings);
        }

        public static Vector3 GetPosition(Vector3Int pos, HexSettings hexSettings)
        {
            return GetPosition(pos.x, pos.y, pos.z, hexSettings);
        }

        private static float GetPositionZ(int z, HexSettings hexSettings)
        {
            return z * (hexSettings.outerRadius * 1.5f) + (z * hexSettings.stepDistance);
        }

        /// <summary>
        /// This will give you the min and max positions of the hexes in the given area aswell as the dimensions(width and height) of the area
        /// </summary>
        /// <param name="minPos"></param>
        /// <param name="maxPos"></param>
        /// <param name="hexSettings"></param>
        /// <returns></returns>
        public static (Vector3 min, Vector3 max, Vector3 dimensions) 
            GetVectorBounds(Vector2Int minPos, Vector2Int maxPos, HexSettings hexSettings)
        {
            HexTile minHex = new HexTile(minPos.x, minPos.y, hexSettings);
            HexTile maxHex = new HexTile(maxPos.x, maxPos.y, hexSettings);

            float minX = minHex.GetWorldVertexPosition(4).x;
            float minY = minHex.GetWorldVertexPosition(3).z;

            Vector3 min = new Vector3(minX, 0, minY);

            float maxX = maxHex.GetWorldVertexPosition(1).x;
            float maxY = maxHex.GetWorldVertexPosition(0).z;

            Vector3 max = new Vector3(maxX, 0, maxY);

            return (min, max, max - min);
        }

        public static Vector3 GetDimensions(Vector2Int minPos, Vector2Int maxPos, HexSettings hexSettings)
        {
            return GetVectorBounds(minPos, maxPos, hexSettings).dimensions;
        }
        /// <summary>
        /// Be aware that this function might not be accurate in some rare occasions. Due to the nature of the hex grid, there are some cases where the function will return the wrong value because of tiny discrepancies in distances. Thus it is recommended to also get the surrounding tiles and check if the tile is actually the closest one.
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public static Vector2Int GetGridCoordinate(Vector3 localPosition, HexSettings hexSettings)
        {
            ExtensionMethods.ClearLog();
            
            Vector3Int gridCoordinate = Vector3Int.zero;

            localPosition.y = 0;

            float x = localPosition.x / (hexSettings.innerRadius * 2f);
            float z = localPosition.z / (hexSettings.outerRadius * 1.5f);

            int x1 = Mathf.CeilToInt(x);
            int z1 = Mathf.CeilToInt(z);

            return GetClosestGrid(x1, z1);

            Vector2Int GetClosestGrid(int maxX, int maxZ)
            {
                Vector2Int closest = Vector2Int.zero;
                float prevDistance = hexSettings.outerRadius * 2;
                float distance = -1;

                int count = 2;

                int xMin = Mathf.Max(0, maxX - count);
                int zMin = Mathf.Max(0, maxZ - count);

                for (int x = maxX; x >= xMin; x--)
                {
                    for (int z = maxZ; z >= zMin; z--)
                    {
                        Vector3 pos = GetPosition(x, 0, z, hexSettings);
                        distance = Vector3.Distance(pos, localPosition);

                        // if the point is inside the hex, then the distance from the      center of the hex to the point should be less than the         outer radius
                        if (distance <= hexSettings.outerRadius)
                        {
                            if(distance < prevDistance)
                            {
                                prevDistance = distance;
                                closest = new Vector2Int(x, z);
                            }
                        }
                    }
                }

                // this should never run
                return closest;
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
        List<Vector2> SlopeUV = new List<Vector2>(28);
        List<int> SlopeTriangles = new List<int>(6);
        Vector2[] BaseUV;

        public HexVisualData VisualData { get; set; }

        /// <summary>
        /// Quick access to the X coordinates of the tile
        /// </summary>
        public int X { get { return GridCoordinates.x; } }

        /// <summary>
        /// Quick access to the Y coordinates of the tile
        /// </summary>
        public int Y { get { return GridCoordinates.y; } }

        public GridManager Grid { get; private set; }

        // We should pass the planet instead

        Random random = new Random();
        ///public static void CreateSlopes(Dictionary<Axial, HexTile> hexes)
        //{
        //    Parallel.ForEach(hexes.Values, (hexTile) =>
        //    {
        //        hexTile.CreateSlopeMesh();
        //    });

        //}
        public static Dictionary<Vector2Int, HexTile> CreatesHexes(GridManager grid, Vector2Int MapSize, ref List<HexChunk> hexChunks, List<HexVisualData> data = null)
        {
            // we define the size of the dictionary to avoid resizing it, which slows things down
            Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>(MapSize.x * MapSize.y + 10);

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
                int chunkBoundsYMin = chunk.ChunkBounds.zMin;
                int chunkBoundsYMax = chunk.ChunkBounds.zMax;

                for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
                {
                    for (int z = chunkBoundsYMin; z < chunkBoundsYMax; z++)
                    {
                        HexTile hc = new HexTile(x, z, grid);

                        hexTiles[hc.GridCoordinates] = hc;

                        if(data != null)
                        {
                            // based on the grid position of the hex, we can calculate the index of the hex in the data list... assuming it is in order of (0, 0) (0, 1) (0, 2) ... (mapSize.x, mapSize.y)

                            int index = (z * MapSize.x) + x;
                            
                            hc.VisualData = data.ElementAtOrDefault(index);
                        }

                        chunk.AddHex(hc);
                    }
                }
            }

            return hexTiles;
        }

        /// <summary>
        /// This private constructor should only be used to measure the bounds of the hex. DO NOT USE IT FOR ANYTHING ELSE
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="hexSettings"></param>
        private HexTile(int x, int z, HexSettings hexSettings)
        {
            AxialCoordinates = Axial.ToAxial(x, z);
            GridCoordinates = new Vector2Int(x, z);

            this.hexSettings = hexSettings;

            Position = GetPosition(x, 0, z, hexSettings);

            CreateBaseMesh();

            SetBounds();
        }

        public HexTile(int x, int z, GridManager grid)
        {
            AxialCoordinates = Axial.ToAxial(x, z);
            GridCoordinates = new Vector2Int(x, z);

            hexSettings = grid.HexSettings;

            float y = random.NextFloat(0, hexSettings.maxHeight);

            Position = GetPosition(x, y, z, hexSettings);

            Grid = grid;

            // hexes will be created with a default color of white
            VisualData = new HexVisualData(Color.white);

            CreateBaseMesh();
            
            SetBounds();

            hexSettings = Grid.HexSettings;
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
        //public void CreateSlopeMesh()
        //{
        //    if (hexSettings.stepDistance == 0 && hexSettings.maxHeight == 0)
        //    {
        //        // there are no slopes
        //        return;
        //    }

        //    SlopeVertices.Clear();
        //    SlopeTriangles.Clear();

        //    List<HexTile> surroundingHex = GetSurroundingHexes();

        //    for (int i = 0; i < 6; i++)
        //    {
        //        HexTile hex1 = surroundingHex[i];

        //        if (hex1 == null)
        //        {
        //            continue;
        //        }

        //        // p1 and p2 denote the 2 points on the current i side in clockwise order

        //        int p1 = i;
        //        // so the last one loops back to the first one
        //        int p2 = (i + 1) % 6;

        //        Vector3 pos1 = Vertices[p1];
        //        Vector3 pos2 = Vertices[p2];

        //        // the current hex1's i vertices
        //        Vector3 pos3 = StepOuterVertexPosition(i) + Vertices[p1];
        //        Vector3 pos4 = StepOuterVertexPosition(i) + Vertices[p2];

        //        pos3.y -= (Position.y - hex1.Position.y) / 2;
        //        pos4.y -= (Position.y - hex1.Position.y) / 2;

        //        int sv = SlopeVertices.Count + Vertices.Count;

        //        SlopeVertices.Add(pos1); // sv + 0
        //        SlopeVertices.Add(pos2); // sv + 1

        //        SlopeVertices.Add(pos3); // sv + 2
        //        SlopeVertices.Add(pos4); // sv + 3

        //        SlopeTriangles.AddRange(new int[6] {
        //                sv + 0, sv + 2, sv + 3,
        //                sv + 0, sv + 3, sv + 1
        //            });

        //        float heightDiff = Mathf.Abs(hex1.Position.y - Position.y);

        //        SlopeUV.AddRange(hexSettings.GetSlopeUV(heightDiff));

        //        // Check for next surrounding hex1
        //        // if it exist, add triangle
        //        // we do this bcuz there is a little gap between the 2 slope of the hex

        //        // next surrounding hex1
        //        int nextSide = (i + 1) % 6;

        //        HexTile hex2 = surroundingHex[nextSide];

        //        Vector3 center;
        //        Vector3 IPos3;

        //        if (hex2 != null && true)
        //        {
        //            // hex2 connecting vertex
        //            IPos3 = StepOuterVertexPosition(nextSide) + Vertices[p2];
        //            IPos3.y -= (Position.y - hex2.Position.y) / 2;

        //            Vector3 hex1Corner, hex2Corner;

        //            // the corners of the surrounding hex, so we can get their centers
        //            hex1Corner = StepOuterVertexPosition(i) * 2 + pos2;
        //            hex2Corner = StepOuterVertexPosition(nextSide) * 2 + pos2;

        //            hex1Corner.y -= (Position.y - hex1.Position.y);
        //            hex2Corner.y -= (Position.y - hex2.Position.y);

        //            center = GetTriangleCenter(pos2, hex1Corner, hex2Corner);

        //            // sv + 1 = pos2 (current hex 2 vertex for slope)
        //            // sv + 3 = pos4 (current hex last slope vertex)

        //            SlopeVertices.Add(IPos3); // sv + 4
        //            SlopeVertices.Add(center); // sv + 5

        //            // SlopeUV.Add(new Vector2()

        //            SlopeTriangles.AddRange(new int[6] {
        //                sv + 1, sv + 3, sv + 4,
        //                sv + 3, sv + 5, sv + 4
        //            });

        //            SlopeUV.AddRange(hexSettings.GetMidTriangleSlopeUV(heightDiff));
        //        }

        //        Vector3 GetTriangleCenter(Vector3 pos1, Vector3 pos2, Vector3 pos3)
        //        {
        //            return (pos1 + pos2 + pos3) / 3f;
        //        }
        //    }
        //}
        //public List<HexTile> GetSurroundingHexes()
        //{
        //    List<HexTile> surroundingHexs = new List<HexTile>();

        //    for (int i = 0; i < 6; i++)
        //    {
        //        Axial sPos = AxialCoordinates + SurroundingHexes[i];

        //        will include null
        //        surroundingHexs.Add(Grid.GetHexTile(sPos));
        //    }

        //    return surroundingHexs;
        //}

        Mesh mesh;
        public Mesh GetMesh()
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


        public struct HexVisualData : IEquatable<HexVisualData>
        {
            public int VisualHash { get; private set; }

            // you can either display the HexColor or the texture
            public Color HexColor { get; private set; }
            public Texture2D BaseTexture { get; private set; }
            public Texture2D OverlayTexture1 { get; private set; }
            public float WeatherLerp { get; set; }

            public HexVisualOption VisualOption { get; set; }
            public enum HexVisualOption { Color, BaseTextures, AllTextures }

            // These constructores are set up in such a way that it forces the use to use either a HexColor or a texture
            // Of course there is also the option of adding both a texture and a color
            public HexVisualData(Color color, HexVisualOption visualOption = HexVisualOption.Color, float lerp = 0)
            {
                VisualHash = 0;

                HexColor = color;
                VisualOption = visualOption;

                BaseTexture = null;
                OverlayTexture1 = null;

                WeatherLerp = lerp;

                UpdateVisualHash();
            }
            public HexVisualData(Texture2D baseTexture,
                                HexVisualOption visualOption = HexVisualOption.BaseTextures)
            {
                VisualHash = 0;

                HexColor = Color.white;
                VisualOption = visualOption;

                BaseTexture = baseTexture;
                OverlayTexture1 = null;

                WeatherLerp = 0;

                UpdateVisualHash();
            }
            public HexVisualData(Texture2D baseTexture, Texture2D overlayTexture, float lerp, HexVisualOption visualOption = HexVisualOption.AllTextures)
            {
                VisualHash = 0;

                HexColor = Color.white;
                VisualOption = visualOption;

                BaseTexture = baseTexture;
                OverlayTexture1 = overlayTexture;

                WeatherLerp = lerp;

                UpdateVisualHash();
            }
            public HexVisualData(Color color, Texture2D baseTexture, Texture2D overlayTexture, float lerp, HexVisualOption visualOption = HexVisualOption.AllTextures)
            {
                VisualHash = 0;

                HexColor = color;
                VisualOption = visualOption;

                BaseTexture = baseTexture;
                OverlayTexture1 = overlayTexture;

                WeatherLerp = lerp;

                UpdateVisualHash();
            }

            /// <summary>
            /// This is used to set the UseColor variable. If true the hash will be changed to use the color, if false the hash will be changed to use the texture
            /// </summary>
            /// <param name="useColor"></param>
            public void SetVisualOption(HexVisualOption option)
            {
                VisualOption = option;
                UpdateVisualHash();
            }

            public void SetColor(Color color)
            {
                HexColor = color;
                UpdateVisualHash();
            }

            public void SetBaseTexture(Texture2D texture)
            {
                BaseTexture = texture;
                UpdateVisualHash();
            }

            public void SetOverlayTexture1(Texture2D texture)
            {
                OverlayTexture1 = texture;
                UpdateVisualHash();
            }

            /// <summary>
            /// Whenever the properties of the hex visuals are updated. You must update the visual hash.
            /// Each has a unique visual hash. This hash is then used to determine if two hexes look thesame and thus can be rendered together in one draw call.
            /// </summary>
            private void UpdateVisualHash()
            {
                int hash = 0;

                switch (VisualOption)
                {
                    case HexVisualOption.Color:
                        hash = HashCode.Combine(VisualOption, WeatherLerp);
                        break;
                    default:
                        
                        if (WeatherLerp == 0)
                        {
                            hash = BaseTexture.GetHashCode();
                        }
                        else
                        {
                            hash = HashCode.Combine(BaseTexture, OverlayTexture1, WeatherLerp);
                        }
                        break;
                }
                // Just a redundancy,
                hash = HashCode.Combine(hash, typeof(HexTile));

                VisualHash = hash;
            }

            // this is primarily used to make check if two objects can be rendered together
            // because objects withsame textures and colors will have thesame hashcode
            public bool Equals(HexVisualData other)
            {
                if (other.VisualHash == VisualHash)
                {
                    return true;
                }

                return false;
            }

        }

        public class HexException : Exception
        {
            public enum ErrorType { NotInGrid, NotInChunk}

            private ErrorType errorType;

            public Vector2Int GridPosition { get; private set; }

            public Vector2 WorldPosition { get; private set; }

            public HexException(Vector2Int gridPosition, ErrorType error) :
                                base(GetMessage(gridPosition.ToString(), false, error))
            {
                errorType = error;
                GridPosition = gridPosition;

                WorldPosition = Vector2.one * -1;
            }

            public HexException(Vector2 worldPosition, ErrorType error) : 
                                base(GetMessage(worldPosition.ToString(), true,  error))
            {
                errorType = error;
                WorldPosition = worldPosition;

                GridPosition = Vector2Int.one * -1;
            }

            public void LogMessage()
            {
                Debug.LogError(Message);
            }

            private static string GetMessage(string position, bool isWorldPos, ErrorType error)
            {
                string GridOrChunk = "";
                string WorldOrGrid = "";

                switch (error)
                {
                    case ErrorType.NotInGrid:
                        GridOrChunk = "Grid";
                        break;
                    case ErrorType.NotInChunk:
                        GridOrChunk = "Chunk";
                        break;
                    default:
                        GridOrChunk = "Grid";
                        break;
                }

                if (isWorldPos)
                {
                    WorldOrGrid = "World";
                }
                else
                {
                    WorldOrGrid = "Grid";
                }

                string message = $"Hex at {WorldOrGrid} Position ({position}) Not Found In {GridOrChunk}";

                return message;
            }
        }

    }
}
