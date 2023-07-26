using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Roytazz.HexMesh
{
    [ExecuteInEditMode]
    public class HexagonGrid : MonoBehaviour
    {
        [Tooltip("Automatically generate new terrain when a value has been changed")]
        public bool AutoUpdateTerrain = true;
        public Vector2 GridSize = new Vector2(100, 100);
        public Vector2 ChunkSize = new Vector2(50, 50);
        public float TileWidth = 5f;

        [Space(15f)]
        public HeightMode HeightMode = HeightMode.Smooth;

        [Header("Following 3 values only used in 'Step' height mode")]
        [Tooltip("The amount every step level is higher than its previous level")]
        public float StepIncrease = 1;
        [Tooltip("Determines the amount of noise applied to each tile. Creates a combination of smooth and step terrain")]
        public float StepNoiseScale = 1;
        [Tooltip("Color of the side faces")]
        public Color StepSideFaceColor = Color.white;

        [Space(15f)]
        public Gradient TerrainGradient;

        [Space(15f)]
        public bool NoiseEnabled = true;
        [Range(0, 500)]
        public float NoiseScale = 10;
        public Noise NoiseSettings;

        [HideInInspector]
        public HexagonMapper GridMapper
        {
            get
            {
                if (_gridMapper == null)
                    _gridMapper = new HexagonMapper(HexagonalGridType.FlatOdd, TileWidth);
                return _gridMapper;
            }
        }
        private HexagonMapper _gridMapper;

        [HideInInspector]
        public List<HexagonMeshChunk> Chunks
        {
            get
            {
                if (_chunks == null)
                    _chunks = GetComponentsInChildren<HexagonMeshChunk>().ToList();
                return _chunks;
            }
        }
        private List<HexagonMeshChunk> _chunks;

        private TileInfo[,] _tileInfo;
        private bool _generateGrid = false;

        /// <summary>
        /// Called when a value in the inspector is changed. Reloads some variables to include the changed values, and redraws the mesh
        /// </summary>
        public void OnValidate()
        {
            if (AutoUpdateTerrain)
            {
                _generateGrid = true;
                _gridMapper = null;
                NoiseSettings?.ReInitialize();
            }
        }

        public void LateUpdate()
        {
            if (_generateGrid)
            {
                GenerateGrid();
                _generateGrid = false;
            }
        }

        /// <summary>
        /// Creates the terrain and chunks. Also clears the current terrain.
        /// </summary>
        public void GenerateGrid()
        {
            ClearGrid();
            _tileInfo = new TileInfo[(int)GridSize.x, (int)GridSize.y];

            //Create mesh for each chunk
            for (int chunkX = 0; chunkX < GridSize.x / ChunkSize.x; chunkX++)
            {
                for (int chunkY = 0; chunkY < GridSize.y / ChunkSize.y; chunkY++)
                {
                    HexagonMeshChunk chunk = new GameObject($"Chunk {chunkX}, {chunkY}").AddComponent<HexagonMeshChunk>();
                    chunk.transform.SetParent(this.transform, false);
                    chunk.ChunkCoords = new Vector2(chunkX, chunkY);
                    chunk.SetMesh(GenerateMeshChunk((int)(chunkX * ChunkSize.x), (int)(chunkY * ChunkSize.y)));
                    Chunks.Add(chunk);
                }
            }

            //Clear the tileinfo array as we dont need it anymore
            _tileInfo = null;
        }

        /// <summary>
        /// Clears the terrain/deletes all chunks
        /// </summary>
        public void ClearGrid()
        {
            Chunks.Clear();
            while (this.transform.childCount != 0)
            {
                DestroyImmediate(this.transform.GetChild(0).gameObject);
            }
        }

        #region Positional & Chunk Mapping

        /// <summary>
        /// Returns the Chunk with the given offset coordinate
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public HexagonMeshChunk ToChunk(Offset offset)
        {
            Vector2 chunkCoords = ToChunkCoords(offset);
            return Chunks.Where(chunk => chunk.ChunkCoords == chunkCoords).FirstOrDefault();
        }

        /// <summary>
        /// Returns the Chunk with the given position in world space
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public HexagonMeshChunk ToChunk(Vector2 worldPos) =>
            ToChunk(ToOffset(worldPos));

        /// <summary>
        /// Returns the chunk coordinates (X,Y) where the given offset coordinate is in
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public Vector2 ToChunkCoords(Offset offset) =>
            new Vector2(
                Mathf.Floor(offset.X / ChunkSize.x),
                Mathf.Floor(offset.Y / ChunkSize.y));

        /// <summary>
        /// Returns the local chunk coordinates from the given Offset coordinates
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public Vector2 ToLocalChunkCoords(Offset coords) =>
            new Vector2(((coords.X % ChunkSize.x) + ChunkSize.x) % ChunkSize.x,
                        ((coords.Y % ChunkSize.y) + ChunkSize.y) % ChunkSize.y);

        /// <summary>
        /// Returns the local chunk coordinates from the given position in world space
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Vector2 ToLocalChunkCoords(Vector2 worldPos) =>
            ToLocalChunkCoords(GridMapper.ToOffset(worldPos.x, worldPos.y));

        /// <summary>
        /// Returns the position in world space from an Offset coordinate
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Offset ToOffset(Vector2 worldPos) =>
            GridMapper.ToOffset(worldPos.x, worldPos.y);

        /// <summary>
        /// Returns an Offset coordinate from a chunk and it's local chunk coords
        /// </summary>
        /// <param name="localChunkCoords"></param>
        /// <param name="chunkCoords"></param>
        /// <returns></returns>
        public Offset ToOffset(Vector2 localChunkCoords, Vector2 chunkCoords) =>
            new Offset(localChunkCoords.x + (chunkCoords.x * ChunkSize.x),
                       localChunkCoords.y + (chunkCoords.y * ChunkSize.y));

        /// <summary>
        /// Returns a world space position from an offset coordinate 
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public Vector3 ToWorld(Offset offset)
        {
            Vector2 pos = GridMapper.ToPoint2v(offset);
            return new Vector3(pos.x, NoiseSettings.GetNoise(pos) * NoiseScale, pos.y);
        }

        /// <summary>
        /// Returns a world space position from a chunk and it's local chunk coords
        /// </summary>
        /// <param name="localChunkCoords"></param>
        /// <param name="chunkCoords"></param>
        /// <returns></returns>
        public Vector3 ToWorld(Vector2 localChunkCoords, Vector2 chunkCoords) =>
            ToWorld(ToOffset(localChunkCoords, chunkCoords));

        #endregion

        #region Mesh Generation

        /// <summary>
        /// Returns a hexagon mesh terrain with vertex colors set. Uses an X and Y offset if the chunk doesnt start at 0,0 in world space
        /// </summary>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <returns></returns>
        private Mesh GenerateMeshChunk(int xOffset = 0, int yOffset = 0)
        {
            //For each hexagon add the vertices of the 6 corners and create four triangles to fill in the shape
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            List<Vector3> vertices = new List<Vector3>();

            for (int x = 0; x < ChunkSize.x; x++)
            {
                for (int y = 0; y < ChunkSize.y; y++)
                {
                    if (x + xOffset >= GridSize.x || y + yOffset >= GridSize.y)
                        continue;

                    Offset coords = new Offset(x + xOffset, y + yOffset);
                    /// Retrieve all corner positions. Corner indexes are as followed:
                    ///   2   1
                    /// 3       0
                    ///   4   5
                    Vector3[] cornerPos = new Vector3[6];
                    float noise = 0f;
                    if (HeightMode == HeightMode.Step)
                        noise = NoiseSettings.GetNoise(GridMapper.ToPoint2v(coords));

                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 pos = GridMapper.GetCornerPointv(coords, i);
                        if (HeightMode == HeightMode.Step)
                            cornerPos[i] = new Vector3(pos.x, GetHeightStep(noise) + NoiseSettings.GetNoise(pos) * StepNoiseScale, pos.y);
                        else
                        {
                            float cornerNoise = NoiseSettings.GetNoise(pos);
                            noise += cornerNoise;
                            cornerPos[i] = new Vector3(pos.x, cornerNoise * NoiseScale, pos.y);
                        }
                    }

                    if (HeightMode != HeightMode.Step)
                        noise /= 6f;

                    //Create an array of indexes that point to the vertices points of all corners
                    //An offset is used of 'vertices.Count' to point at the correct hexagon corner in the full array
                    triangles.AddRange(HexagonMapper.HexagonTriIndexes(vertices.Count));
                    vertices.AddRange(cornerPos);

                    //Finally we take the noise (which maps to the height) 
                    //and set the vertex color of all 6 corners to color the whole tile in the same color
                    colors.AddRange(GetTileColors(noise));

                    //Store all necessary tile info to be used later
                    _tileInfo[coords.X, coords.Y] = new TileInfo
                    {
                        Corners = cornerPos
                    };

                    #region Borders

                    //Keep the Borders region folded please. Its a behemoth. It could prob be extrated into seperate methods,
                    //but it will prob just make it super confusing, if it isnt enough already

                    if (HeightMode == HeightMode.Step)
                    {
                        //Create the borders/cliffside between two hexagon tiles. Done on only 3 sides on every tile
                        //Bottom Edge
                        Offset bottomNeighbourCoords = new Offset(coords.X, coords.Y - 1);
                        if (IsCoordsValid(bottomNeighbourCoords))
                        {
                            TileInfo bottomNeighbour = _tileInfo[bottomNeighbourCoords.X, bottomNeighbourCoords.Y];
                            if (bottomNeighbour != null)
                            {
                                Vector3[] border = new Vector3[4]
                                {
                                    cornerPos[4], cornerPos[5],
                                    bottomNeighbour.Corners[1], bottomNeighbour.Corners[2]
                                };
                                //We use the 4 corners in the bottomBorder array that make up the bottom border and get
                                //an array of indexes that point to the correct vertices
                                triangles.AddRange(
                                    HexagonBorderTriIndexes(bottomNeighbour.Height / NoiseScale > noise, vertices.Count));
                                vertices.AddRange(border);
                                for (int i = 0; i < 4; i++)
                                    colors.Add(StepSideFaceColor);
                            }
                        }

                        //Lower left Edge
                        Axial lowerLeftAxial = GridMapper.ToAxial(coords);
                        Offset lowerLeftNeighbourCoords = GridMapper.ToOffset(new Axial(lowerLeftAxial.Q - 1, lowerLeftAxial.R));
                        if (IsCoordsValid(lowerLeftNeighbourCoords))
                        {
                            TileInfo lowerLeftNeighbour = _tileInfo[lowerLeftNeighbourCoords.X, lowerLeftNeighbourCoords.Y];
                            if (lowerLeftNeighbour != null)
                            {
                                Vector3[] border = new Vector3[4] {
                                    cornerPos[3], cornerPos[4],
                                    lowerLeftNeighbour.Corners[0], lowerLeftNeighbour.Corners[1]
                                };
                                //We use the 4 corners in the bottomBorder array that make up the bottom border and get
                                //an array of indexes that point to the correct vertices
                                triangles.AddRange(HexagonBorderTriIndexes(lowerLeftNeighbour.Height / NoiseScale > noise, vertices.Count));
                                vertices.AddRange(border);
                                for (int i = 0; i < 4; i++)
                                    colors.Add(StepSideFaceColor);
                            }
                        }

                        //Upper left Edge
                        Axial upperLeftAxial = GridMapper.ToAxial(coords);
                        Offset upperLeftNeighbourCoords = GridMapper.ToOffset(new Axial(upperLeftAxial.Q - 1, upperLeftAxial.R + 1));
                        if (IsCoordsValid(upperLeftNeighbourCoords))
                        {
                            TileInfo upperLeftNeighbour = _tileInfo[upperLeftNeighbourCoords.X, upperLeftNeighbourCoords.Y];
                            Vector3[] border = new Vector3[0];

                            //If we dont have an upperLeftNeighbour, that doesnt mean we are 
                            //at the end of the map, it could have not been generated yet (generation goes from bottom to top)
                            //Then we dont have any tile data, so we fetch it ourselves
                            if (upperLeftNeighbour == null)
                            {
                                Vector2 upperLeftPos = GridMapper.ToPoint2v(upperLeftNeighbourCoords);

                                Vector2 upperLeftCorner1 = GridMapper.GetCornerPointv(upperLeftNeighbourCoords, 5);
                                Vector2 upperLeftCorner2 = GridMapper.GetCornerPointv(upperLeftNeighbourCoords, 0);

                                float upperLeftTileNoise = NoiseSettings.GetNoise(upperLeftPos);
                                border = new Vector3[4] {
                                    cornerPos[2], cornerPos[3],
                                    new Vector3(upperLeftCorner1.x, GetHeightStep(upperLeftTileNoise) + NoiseSettings.GetNoise(upperLeftCorner1) * StepNoiseScale, upperLeftCorner1.y),
                                    new Vector3(upperLeftCorner2.x, GetHeightStep(upperLeftTileNoise) + NoiseSettings.GetNoise(upperLeftCorner2) * StepNoiseScale, upperLeftCorner2.y)
                                };
                                //We use the 4 corners in the bottomBorder array that make up the bottom border and get
                                //an array of indexes that point to the correct vertices
                                triangles.AddRange(HexagonBorderTriIndexes((GetHeightStep(noise) + NoiseSettings.GetNoise(upperLeftCorner1) * StepNoiseScale) > noise, vertices.Count));
                            }
                            else
                            {
                                border = new Vector3[4] {
                                    cornerPos[2], cornerPos[3],
                                    upperLeftNeighbour.Corners[5], upperLeftNeighbour.Corners[0]
                                };
                                //We use the 4 corners in the bottomBorder array that make up the bottom border and get
                                //an array of indexes that point to the correct vertices
                                triangles.AddRange(HexagonBorderTriIndexes(upperLeftNeighbour.Height / NoiseScale > noise, vertices.Count));
                            }
                            vertices.AddRange(border);
                            for (int i = 0; i < 4; i++)
                                colors.Add(StepSideFaceColor);
                        }
                    }

                    #endregion Borders
                }
            }

            Mesh mesh = new Mesh() { name = "HexagonMeshChunk" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// Retrieves the color from the terrain gradients from the given noise
        /// </summary>
        /// <param name="noise"></param>
        /// <returns></returns>
        private Color[] GetTileColors(float noise)
        {
            if (TerrainGradient == null)
                return new Color[0];

            Color tileColor = TerrainGradient.Evaluate(noise);
            Color[] result = new Color[6];
            for (int i = 0; i < 6; i++)
            {
                result[i] = tileColor;
            }
            return result;
        }

        /// <summary>
        /// Retrieves the height for the given tile based on the Noise.
        /// Used when HeightMode is set to Step mode
        /// </summary>
        /// <param name="noise"></param>
        /// <returns></returns>
        private float GetHeightStep(float noise)
        {
            Color color = TerrainGradient.Evaluate(noise);
            for (int i = 0; i < TerrainGradient.colorKeys.Length; i++)
            {
                if (color == TerrainGradient.colorKeys[i].color)
                    return i * StepIncrease;
            }
            return 0f;
        }

        /// <summary>
        /// Checks wether the given offset coordinate is within our gridsize
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        private bool IsCoordsValid(Offset coords) => coords.X >= 0 && coords.Y >= 0 && GridSize.x > coords.X && GridSize.y > coords.Y;

        /// <summary>
        /// Returns the tri indexes of a border/cliff between two hex tiles.
        /// </summary>
        /// <param name="neighbourIsHigher">If the neighbour tile higher than the current tile</param>
        /// <param name="indexOffset">The amount the tri index needs to be offset</param>
        /// <returns></returns>
        private int[] HexagonBorderTriIndexes(bool neighbourIsHigher, int indexOffset)
        {
            if (neighbourIsHigher)
            {
                return new int[6] {
                    1 + indexOffset, 2 + indexOffset, 3 + indexOffset,
                    3 + indexOffset, 0 + indexOffset, 1 + indexOffset
                };
            }
            else
            {
                return new int[6] {
                    2 + indexOffset, 3 + indexOffset, 1 + indexOffset,
                    3 + indexOffset, 0 + indexOffset, 1 + indexOffset
                };
            }
        }

        #endregion

        /// <summary>
        /// Describes all information about a hexagon tile
        /// </summary>
        private class TileInfo
        {
            //Instead of averaging the normal of each tri of our hex, we take the 3 corners or a imaginary 'middle' tri
            public Vector3 Normal => Vector3.Cross(Corners[2] - Corners[0], Corners[4] - Corners[0]).normalized;
            public float Height => Corners.Average(corner => corner.y);

            /// <summary>
            /// Position of each corner in world space
            /// </summary>
            public Vector3[] Corners { get; set; }
        }
    }

    public enum HeightMode
    {
        Smooth,
        Step
    }
}