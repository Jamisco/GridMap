using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Assets.Scripts.WorldMap
{
    public class HexTile
    {
        public HexSettings hexSettings;

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
        /// 
        public static Vector3 GetPosition(int x, float y, int z, HexSettings hexSettings)
        {
            Vector3 position;
            // it looks thsame but i assure u, its differnt
            position.x = (x + (z * 0.5f) - (z / 2)) * (hexSettings.innerRadius * 2f);
            position.y = y;
            position.z = z * (hexSettings.outerRadius * 1.5f);

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
            return z * (hexSettings.outerRadius * 1.5f);
        }

        /// <summary>
        /// This will give you the min and max positions of the hexes in the given area aswell as the dimensions(width and height) of the area
        /// </summary>
        /// <param name="minPos"></param>
        /// <param name="maxPos"></param>
        /// <param name="hexSettings"></param>
        /// <returns></returns>
        public static (Vector3 min, Vector3 max, Vector3 size) 
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

        /// <summary>
        /// Given the min and max positions of the hexes in the given area, this will return the bounds of the area
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="hexSettings"></param>
        /// <returns></returns>
        public static Bounds GetWorldBounds(BoundsInt bounds,
                                    HexSettings hexSettings)
        {
            Vector2Int min = new Vector2Int(bounds.min.x, bounds.min.z);

            Vector2Int max = new Vector2Int(bounds.max.x, bounds.max.z);

            (Vector3 min, Vector3 max, Vector3 size) vb = HexTile.GetVectorBounds(min, max, hexSettings);

            return new Bounds((vb.min + vb.max) / 2, vb.max - vb.min);
        }

        public static Vector3 GetSize(Vector2Int minPos, Vector2Int maxPos, HexSettings hexSettings)
        {
            return GetVectorBounds(minPos, maxPos, hexSettings).size;
        }
        /// <summary>
        /// Will return Vector2Int.Left if nothing is found
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public static Vector2Int GetGridCoordinate(Vector3 localPosition, HexSettings hexSettings)
        {
            //ExtensionMethods.ClearLog();
            
            Vector3Int gridCoordinate = Vector3Int.zero;

            localPosition.y = 0;

            float x = localPosition.x / (hexSettings.innerRadius * 2f);
            float z = localPosition.z / (hexSettings.outerRadius * 1.5f);

            int x1 = Mathf.CeilToInt(x);
            int z1 = Mathf.CeilToInt(z);

            return GetClosestGrid(x1, z1);

            Vector2Int GetClosestGrid(int maxX, int maxZ)
            {
                Vector2Int closest = Vector2Int.left;
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

        private Axial _axeCoord = new Axial(-1, -1, -1);
        public Axial AxialCoordinates
        {
            get
            {
                if (_axeCoord.X == -1)
                {
                    _axeCoord = Axial.ToAxial(X, Y);
                }

                return _axeCoord;
            }
        }
        
        public Vector2Int GridPosition;
        public Vector3 LocalPosition { get; private set; }

        public List<Vector3> Vertices = new List<Vector3>(6);
        public List<int> Triangles = new List<int>(12);
        
        Vector2[] BaseUV;

        public HexVisualData VisualData { get; set; } = new HexVisualData(Color.white);

        /// <summary>
        /// Quick access to the X coordinates of the tile
        /// </summary>
        public int X { get { return GridPosition.x; } }

        /// <summary>
        /// Quick access to the Y coordinates of the tile
        /// </summary>
        public int Y { get { return GridPosition.y; } }

        public GridManager Grid { get; private set; }

        public static Dictionary<Vector2Int, HexTile> CreatesHexes(GridManager grid, Vector2Int MapSize, List<HexChunk> hexChunks)
        {
            // we define the size of the dictionary to avoid resizing it, which slows things down
            Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>(MapSize.x * MapSize.y + 10);

            #region Parrallel forEach
            //Parallel.ForEach(hexChunks, chunk =>
            //{
            //    int chunkBoundsXMin = chunk.ChunkBounds.xMin;
            //    int chunkBoundsXMax = chunk.ChunkBounds.xMax;
            //    int chunkBoundsZMin = chunk.ChunkBounds.yMin;
            //    int chunkBoundsZMax = chunk.ChunkBounds.yMax;

            //    for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
            //    {
            //        for (int z = chunkBoundsZMin; z < chunkBoundsZMax; z++)
            //        {
            //            HexTile hc = new HexTile(x, z);

            //            hexTiles[hc.AxialCoordinates] = hc;

            //            chunk.QuickAddHex(hc);
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
            //    int chunkBoundsZMin = chunk.ChunkBounds.yMin;
            //    int chunkBoundsZMax = chunk.ChunkBounds.yMax;

            //    for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
            //    {
            //        for (int z = chunkBoundsZMin; z < chunkBoundsZMax; z++)
            //        {
            //            HexTile hc = new HexTile(x, z);

            //            lock (hexTiles) // Ensure safe access to the dictionary
            //            {
            //                hexTiles[hc.AxialCoordinates] = hc;
            //            }

            //            chunk.QuickAddHex(hc);
            //        }
            //    }
            //});

            #endregion

            // It seems using a standard loop is about 30% - 50% faster than using parrallel.
            // For a 1000 x 1000 grid
            // For loop: 7 seconds
            // Parrellel foreach/for 15 - 16 seconds

            HexSettings settings = grid.HexSettings;

            for (int chunkIndex = 0; chunkIndex < hexChunks.Count; chunkIndex++)
            {
                HexChunk chunk = hexChunks[chunkIndex];
                int chunkBoundsXMin = chunk.ChunkBounds.xMin;
                int chunkBoundsXMax = chunk.ChunkBounds.xMax;
                int chunkBoundsZMin = chunk.ChunkBounds.zMin;
                int chunkBoundsZMax = chunk.ChunkBounds.zMax;

                // It must be understood that the chunk bounds are not the same as the map size. The entirity of the bounds must not necessarily be within the map size or used
                chunkBoundsXMax = Mathf.Clamp(chunkBoundsXMax, 0, MapSize.x);
                chunkBoundsZMax = Mathf.Clamp(chunkBoundsZMax, 0, MapSize.y);

                for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
                {
                    for (int z = chunkBoundsZMin; z < chunkBoundsZMax; z++)
                    {
                        HexTile hc = new HexTile(x, z, settings);

                        hexTiles[hc.GridPosition] = hc;

                        chunk.QuickAddHex(hc);
                    }
                }
            }

            return hexTiles;
        }

        public static Dictionary<Vector2Int, HexTile> CreatesHexes(GridManager grid, Vector2Int MapSize, List<HexChunk> hexChunks, List<HexVisualData> data)
        {
            // we define the size of the dictionary to avoid resizing it, which slows things down
            Dictionary<Vector2Int, HexTile> hexTiles = new Dictionary<Vector2Int, HexTile>(MapSize.x * MapSize.y + 10);

            HexSettings hexSettings = grid.HexSettings;

            #region Parrallel forEach
            //Parallel.ForEach(hexChunks, chunk =>
            //{
            //    int chunkBoundsXMin = chunk.ChunkBounds.xMin;
            //    int chunkBoundsXMax = chunk.ChunkBounds.xMax;
            //    int chunkBoundsZMin = chunk.ChunkBounds.yMin;
            //    int chunkBoundsZMax = chunk.ChunkBounds.yMax;

            //    for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
            //    {
            //        for (int z = chunkBoundsZMin; z < chunkBoundsZMax; z++)
            //        {
            //            HexTile hc = new HexTile(x, z);

            //            hexTiles[hc.AxialCoordinates] = hc;

            //            chunk.QuickAddHex(hc);
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
            //    int chunkBoundsZMin = chunk.ChunkBounds.zMin;
            //    int chunkBoundsZMax = chunk.ChunkBounds.zMax;

            //    for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
            //    {
            //        for (int z = chunkBoundsZMin; z < chunkBoundsZMax; z++)
            //        {
            //            HexTile hc = new HexTile(x, z, hexSettings);

            //            hexTiles[hc.GridPosition] = hc;

            //            chunk.QuickAddHex(hc);
            //        }
            //    }
            //});

            #endregion

            // It seems using a standard loop is about 30% - 50% faster than using parrallel.
            // For a 1000 x 1000 grid
            // For loop: 7 seconds
            // Parrellel foreach/for 15 - 16 seconds
            HexTile hc;
            for (int chunkIndex = 0; chunkIndex < hexChunks.Count; chunkIndex++)
            {
                HexChunk chunk = hexChunks[chunkIndex];
                int chunkBoundsXMin = chunk.ChunkBounds.xMin;
                int chunkBoundsXMax = chunk.ChunkBounds.xMax;
                int chunkBoundsZMin = chunk.ChunkBounds.zMin;
                int chunkBoundsZMax = chunk.ChunkBounds.zMax;

                // It must be understood that the chunk bounds are not the same as the map size. The entirity of the bounds must not necessarily be within the map size or used
                chunkBoundsXMax = Mathf.Clamp(chunkBoundsXMax, 0, MapSize.x);
                chunkBoundsZMax = Mathf.Clamp(chunkBoundsZMax, 0, MapSize.y);

                for (int x = chunkBoundsXMin; x < chunkBoundsXMax; x++)
                {
                    for (int z = chunkBoundsZMin; z < chunkBoundsZMax; z++)
                    {
                        hc = new HexTile(x, z, hexSettings);

                        hexTiles.Add(hc.GridPosition, hc);

                        int index = (z * MapSize.x) + x;

                        hc.VisualData = data.ElementAtOrDefault(index);

                        chunk.QuickAddHex(hc);
                    }
                }
            }

            return hexTiles;
        }

        public float Elevation { get; set; }

        public HexTile(int x, int z, HexSettings hexSettings, float elevation = 0)
        {
            GridPosition = new Vector2Int(x, z);

            this.hexSettings = hexSettings;

            Elevation = elevation;

            LocalPosition = GetPosition(x, 0, z, hexSettings);

            CreateBaseMesh();
        }
        public void CreateBaseMesh()
        {
            Triangles = hexSettings.BaseTrianges();

            Vertices = hexSettings.VertexCorners;

            BaseUV = hexSettings.BaseHexUV;
        }

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

            return combinedVertices;
        }
        List<int> CombineTriangles()
        {
            List<int> combinedTriangles = new List<int>(Triangles);

            return combinedTriangles;
        }
        List<Vector2> CombineUV()
        {
            List<Vector2> combinedUV = new List<Vector2>(hexSettings.BaseHexUV);

            return combinedUV;
        }
        public void ClearMesh()
        {
            Vertices.Clear();
            Triangles.Clear();
        }
        public Vector3 GetWorldVertexPosition(int index)
        {
            return Vertices[index] + LocalPosition;
        }
        public int VertexCount()
        {
            return CombineVertices().Count;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(GridPosition, AxialCoordinates, LocalPosition, mesh);
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
            public enum ErrorType { NotInGrid, NotInChunk, AlreadyInGrid}

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
                string WorldOrGrid = "";
                string message = "";
                
                if (isWorldPos)
                {
                    WorldOrGrid = "World";
                }
                else
                {
                    WorldOrGrid = "Grid";
                }

                message = $"Hex at {WorldOrGrid} Position ({position}) {error} ";

                return message;
            }
        }

    }
}
