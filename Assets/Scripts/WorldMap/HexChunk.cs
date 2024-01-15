using Assets.Scripts.Miscellaneous;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.HexTile;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using static Assets.Scripts.WorldMap.FusedMesh;
using Unity.VisualScripting;
using static Assets.Scripts.WorldMap.GridManager;
using System;

namespace Assets.Scripts.WorldMap
{
    // Due to the nature of this class, it will have to work hand in hand with the Gridmanager is order to properly share data. It is recommended that you refrain from doing specific functions that would limit the gridmanager from being able to controlt them. For example, a chunk should not be the one to highlight itself, but rather the gridmanager should be the one to do it. A chunk should not store the data, data about itself that isnt nessacary for it to display its meshes. We should force the Gridmanager to store that.

    // Max hex count per chunk is about 1600. See Gridmanager.CreateHexChunks() for details
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class HexChunk : MonoBehaviour
    {
        // you must be aware that technically speaking, all these chunks are at gridPosition (0,0). It is their meshes/hexes that are place appriopriately
        
        public GridManager MainGrid;

        private Material MainMaterial;
        private Material InstanceMaterial;

        private HexSettings hexSettings;

        private List<HexTile> hexes;

        private Dictionary<Vector2Int, HexTile> hexDictionary = new Dictionary<Vector2Int, HexTile>();

        private Dictionary<HexVisualData, List<HexTile>> biomeTiles = new Dictionary<HexVisualData, List<HexTile>>();

        private Dictionary<HexVisualData, FusedMesh> BaseFusedMeshes = new Dictionary<HexVisualData, FusedMesh>();

        private List<Material> materials = new List<Material>();
        private List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();
        
        // the highlight layer has to be above the base layer.
        // how high above do u want it to be
        private static readonly Vector3 HighlightLayerOffset = new Vector3(0, .001f, 0);
        private static readonly Vector3 BorderLayerOffset = new Vector3(0, .002f, 0);

        public BoundsInt _chunkBounds;
       /// <summary>
            /// DO NOT USE THIS TO CHECK IF A SPECIFIC HEX/COORDINATES IS IN THE CHUNK.
            /// Does not work for an unknown reason
            /// Bounds are not Inclusive. Thus the last row and column contain no hexes
         /// </summary>
        public BoundsInt ChunkBounds
        {
            get
            {
                return _chunkBounds;
            }           
            set
            {
                _chunkBounds = value;
                hexes.Capacity = _chunkBounds.size.x * _chunkBounds.size.y;
                SetWorldBounds();
            }
        }

        /// <summary>
        /// Use this to check if a grid position is in the chunk
        /// </summary>
        public Bounds GridBounds
        {
            get
            {
                return new Bounds(ChunkBounds.center, ChunkBounds.size);
            }
        }

        private Bounds _worldBounds;
       /// <summary>
        /// The world bounds of the chunkbounds. Use this to check if a world position in a chunk regardless of whether the chunk mesh is drawn or not
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                // account for object transform
                Bounds respectiveBounds = _worldBounds;

                respectiveBounds.center += transform.position;

                return respectiveBounds;
            }
        }

        /// <summary>
        /// The world bounds of the chunk mesh. Use this to check if a world position in a chunk and within the drawn mesh.
        /// </summary>
        public Bounds MeshWorldBounds
        {
            get
            {
                return meshRenderer.bounds;
            }
        }
        
        RenderParams renderParams;
        RenderParams instanceParam;

        MeshRenderer meshRenderer;

        LayeredMesh HighlightLayer;
        LayeredMesh BorderLayer;

        LayeredMesh TestLayer;

        public bool IsEmpty { get { return hexes.Count == 0; } }

        /// <summary>
        /// Start grid position of the chunk bounds
        /// </summary>
        public Vector2Int StartPosition { get; private set; }

        /// <summary>
        /// End grid position of the chunk bounds
        /// </summary>
        public Vector2Int EndPosition { get; private set; }

        public void AddMeshLayer(string name, int layerId)
        {
            TestLayer = new LayeredMesh(this, name, layerId, MainGrid.Data.HighlightShader);

            Debug.Log("Added Layer: " + name);
        }

        public void test()
        {
            FusedMesh tMesh = TestLayer.LayerFusedMesh;
            
            Mesh hMesh = hexSettings.GetInnerHighlighter();

            hMesh.SetFullColor(Color.red);


            HexTile ranHex = hexes[0];
            
            tMesh.InsertMesh(hMesh,
                ranHex.GetHashCode(), ranHex.LocalPosition);

            TestLayer.UpdateMesh();
        }
        public void Initialize(GridManager grid, GridData gridData, 
            BoundsInt gridBounds)
        {
            hexSettings = gridData.HexSettings;
            
            MainMaterial = gridData.MainMaterial;
            InstanceMaterial = gridData.InstanceMaterial;

            renderParams = new RenderParams(MainMaterial);
            instanceParam = new RenderParams(InstanceMaterial);

            meshRenderer = GetComponent<MeshRenderer>();

            MainGrid = grid;

            _chunkBounds = gridBounds;

            StartPosition = new Vector2Int(_chunkBounds.min.x, _chunkBounds.min.z);

            EndPosition = new Vector2Int(_chunkBounds.max.x, _chunkBounds.max.z);

            SetWorldBounds();

            hexes = new List<HexTile>(gridBounds.size.x * gridBounds.size.y);

            int sortId = meshRenderer.sortingLayerID;

            HighlightLayer = new LayeredMesh(this, "Highlight Layer", sortId, gridData.HighlightShader);

            BorderLayer = new LayeredMesh(this, "Border Layer", sortId, gridData.HighlightShader);
        }

        private void SetWorldBounds()
        {
            _worldBounds = GetWorldBounds(ChunkBounds, hexSettings);
        }
        /// <summary>
        /// Quickly adds a hex to the chunk. Is thread safe, but does not sort the hexes into visual groups and thus hex cannot be drawn
        /// </summary>
        /// <param name="hex"></param>
        public void QuickAddHex(HexTile hex)
        {
            if (!IsInChunk(hex.GridPosition))
            {
                Debug.Log("Adding a Hex that isnt within Chunk Bounds");
                return;
            }
           
            // the reason we use a concurrent bag is because it is thread safe
            // thus you can add to it from multiple from threads
            bool ss = hexDictionary.TryAdd(hex.GridPosition, hex);

            if (ss == false)
            {
                Debug.Log("Failed to Add Hex: " + hex.GridPosition.ToString());
            }
        }
       /// <summary>
        /// Will add a hex and immediately sort it.This is not thread safe. Do not use to draw multiple hexes at once. Instead, add the hexes with draw set to false, then manually call the draw method
        /// </summary>
        /// <param name="hex"></param>
        public void AddHex(HexTile hex, bool draw = false)
        {
            if (!IsInChunk(hex.GridPosition))
            {
                Debug.Log("Adding a Hex that isnt within Chunk Bounds");
                return;
            }
            
            bool success = hexDictionary.TryAdd(hex.GridPosition, hex);
            
            if(success)
            {
                hexes.Add(hex);

                AddBiomeTile(hex);

                AddVisualData(hex);

                if(draw)
                {
                    DrawFusedMeshes();
                }
            }
        }

        /// <summary>
        /// Will add all the hexes that are within the chunk bounds
        /// </summary>
        /// <param name="gridSize">Pass in the Grid size of the map so that the chunk size is constraint, to the grid</param>
        /// <param name="draw"></param>
        public void AddAllHexes(Vector2Int gridSize, bool draw = false)
        {
            for (int x = _chunkBounds.min.x; 
                     x < _chunkBounds.max.x && x < gridSize.x; x++)
            {
                for (int z = _chunkBounds.min.z; 
                         z < _chunkBounds.max.z && z < gridSize.y; z++)
                {
                    Vector2Int pos = new Vector2Int(x, z);

                    if (hexDictionary.ContainsKey(pos))
                    {
                        continue;
                    }

                    HexTile hex = new HexTile(x, z, hexSettings);

                    if (hex == null)
                    {
                        continue;
                    }

                    AddHex(hex, false);
                }
            }

            if (draw)
            {
                DrawFusedMeshes();
            }
        }
        /// <summary>
        /// Will RemoveHex a hex from the chunk. Hex will also be subsequently removed from highlighted and border meshes etc.
        /// </summary>
        /// <param name="hex"></param>
        /// <param name="draw"></param>
        public void RemoveHex(HexTile hex, bool draw = false)
        {
            ThrowIfNotInChunk(hex);

            RemoveHexFromLists(hex);

            HexVisualData props = hex.VisualData;

            bool success = BaseFusedMeshes[props].RemoveMesh(hex.GetHashCode());

            UnHighlightHex(hex);

            DeactivateAllHexBorders(hex);

            if (draw)
            {
                DrawFusedMeshes();
            }
        }

        // This splits all hexes into thesame visual groups, so they can be rendered together
        private void SplitDictionary()
        {
            if (hexes.Count != hexDictionary.Count)
            {
                hexes = hexDictionary.Values.ToList();
            }

            biomeTiles.Clear();
            
            foreach (HexTile hex in hexes)
            {
                AddBiomeTile(hex);
            }
        }

        private void AddBiomeTile(HexTile hex)
        {
            HexVisualData props = hex.VisualData;

            if (biomeTiles.ContainsKey(props))
            {
                biomeTiles[props].Add(hex);
            }
            else
            {
                biomeTiles.TryAdd(props, new List<HexTile>() { hex });
            }
        }
        /// <summary>
        /// This will split the hexes into their respective visual groups and draw them. Note this is a very expensive operation and should only be done when trying to Initially Draw the chunk or REDRAWING the whole chunk
        /// </summary>
        public void InitiateDrawProtocol()
        {    
            SplitDictionary();
            FuseMeshes();
        }
        private void FuseMeshes()
        {
            List<MeshData> meshes = new List<MeshData>();
            List<int> hashes = new List<int>();
            List<Vector3> offsets = new List<Vector3>();
            
            // Essentially, this tells us this
            // for each index, when inserting triangles at a specific index, you must offset the vertex count by the givin variable
            // and you must offset the insert gridPosition using the triStartIndex
            List<(int vertCount, int triStartIndex)> vertTriIndex = new List<(int, int)>();
            (int totalVerts, int totalTri) totals = (0, 0);
            // the below loop accounts for 90% of the time taken for this method 
            // this entire method alone accounts for 60% of the time taken to draw the entire map

            // drawing the hexes individuall is horrendouesly ineffective
            // a 100 x 100 map with 1000 hexes per chunk averages 5 - 10 fps

            foreach (KeyValuePair<HexVisualData, List<HexTile>> biomes in biomeTiles)
            {
                ExtractData(biomes.Value);

                // this will fuse all the meshes together. The fuse constructor using multithreading inorder to increase the speed
                BaseFusedMeshes.Add(biomes.Key,
                    new FusedMesh(meshes, hashes, offsets, vertTriIndex, totals));
            }

            DrawFusedMeshes();

            void ExtractData(List<HexTile> hexes)
            {
                meshes.Clear();
                hashes.Clear();
                offsets.Clear();
                vertTriIndex.Clear();

                int vert = 0;
                int tri = 0;

                foreach (HexTile hex in hexes)
                {
                    meshes.Add(new MeshData(hex.GetMesh()));
                    hashes.Add(hex.GetHashCode());
                    offsets.Add(hex.LocalPosition);
                      
                    vertTriIndex.Add((vert, tri));
                    
                    vert += hex.GetMesh().vertexCount;
                    tri += hex.GetMesh().triangles.Count();
                    
                }

                totals = (vert, tri);
            }
        }
        public void DrawFusedMeshes()
        {
            // The downside of this is that every time you change the mesh of any fused mesh you have to recombine ALL the other meshes
            // thankfully, it is not that expensive to do so, accounts for at most 10% of the time taken to draw the entire map.

            // This can be further optimized if, instead of combining all the mesh individually again, we remove only the updated mesh, and recombine it
            Mesh mainMeshes =
                FusedMesh.CombineToSubmesh(BaseFusedMeshes.Values.ToList());

            mainMeshes.RecalculateBounds();

            SetMaterialProperties();

            SetMaterialPropertyBlocks();

            GetComponent<MeshFilter>().mesh = mainMeshes;
            GetComponent<MeshCollider>().sharedMesh = mainMeshes;

            // everytime we redraw the mesh, we must make sure the sorting order of the highlighters are themselves updated;
            UpdateSortOrders();

        }
        
        private void UpdateSortOrders()
        {
            int order = meshRenderer.sortingOrder + 1;

            HighlightLayer.OrderInLayer = order;
            BorderLayer.OrderInLayer = order;
        }

        /// <summary>
        /// Since we combined all of the individual meshes into one, there exist only one collider. THus we need to find the hex that was clicked on base on the gridPosition. 
        /// We do this by getting all the possible grid positions within the vicinity of the mouse click and then we measure between the positions of said grid positions and the mouse click gridPosition. The grid gridPosition with the smallest distance is the one that was clicked on.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public HexTile GetClosestHex(Vector3 position)
        {
            // average time to find hex is 0.002 seconds
            HexTile hex;
            List<Vector2Int> possibleGridCoords = new List<Vector2Int>();

            Vector2Int gridPos = GetGridCoordinate(position, hexSettings);

            if (IsInChunk(gridPos) == false)
            {
                throw new HexException(position, HexException.ErrorType.NotInGrid);
            }

            possibleGridCoords.Add(gridPos);

            Vector2Int gridPos2 = new Vector2Int(gridPos.x + 1, gridPos.y);
            Vector2Int gridPos3 = new Vector2Int(gridPos.x - 1, gridPos.y);
            Vector2Int gridPos4 = new Vector2Int(gridPos.x, gridPos.y + 1);
            Vector2Int gridPos5 = new Vector2Int(gridPos.x, gridPos.y - 1);

            possibleGridCoords.Add(gridPos2);
            possibleGridCoords.Add(gridPos3);
            possibleGridCoords.Add(gridPos4);
            possibleGridCoords.Add(gridPos5);

            hex = GetClosestHex(possibleGridCoords, position);
            //Debug.Log("Hex: " + hex.GridPosition.ToString() + " Hit");
            return hex;

            HexTile GetClosestHex(List<Vector2Int> coords, Vector3 pos)
            {
                HexTile closestHex = null;

                float shortestDistance = float.MaxValue;

                HexTile posHex = null;

                foreach (Vector2Int item in coords)
                {
                    posHex = null;

                    hexDictionary.TryGetValue(item, out posHex);

                    if (posHex == null)
                    {
                        continue;
                    }

                    if (posHex != null)
                    {
                        float currDistance = Vector3.Distance(posHex.LocalPosition, pos);

                        if (currDistance < shortestDistance)
                        {
                            shortestDistance = currDistance;
                            closestHex = posHex;
                        }
                    }
                }

                return closestHex;
            }
        }

        private void RemoveHexFromLists(HexTile hex)
        {
            bool go2od = hexDictionary.Remove(hex.GridPosition, out HexTile hexTile);

            bool good2 =  hexes.Remove(hex);

            HexVisualData props = hex.VisualData;

            bool good = biomeTiles[props].Remove(hex);
        }

        Dictionary<HexTile, Color> changedColor = new Dictionary<HexTile, Color>();

        // It must be understood that the bounds for chunks has no Y axis, and thus, any bound check against a position must be done with the Y axis set to 0

        /// <summary>
        /// Checks if a grid position is within the grid bounds of a chunk
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsInChunk(int x, int y)
        {
            // For some reason BoundsInt.Contains is not working...idk why
            Bounds chunkBounds = new Bounds(ChunkBounds.center,  ChunkBounds.size);

            if (x == ChunkBounds.max.x || y == ChunkBounds.max.z)
            {
                return false;
            }

            if (chunkBounds.Contains(new Vector3Int(x, 0, y)))
            {
                return true;
            }

            return false;
        }
        public bool IsInChunk(HexTile hex)
        {
            return IsInChunk(hex.X, hex.Y);
        }

        public bool IsInChunk(Vector2Int gridPosition)
        {
            return IsInChunk(gridPosition.x, gridPosition.y);
        }

        /// <summary>
        /// Use to check if a world position in inside the chunk. Regardless of whether or not the hexes are drawn
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public bool IsInChunk(Vector3 worldPosition)
        {
            if(WorldBounds.Contains(worldPosition))
            {
                return true;
            }

            return false;
        }

        public bool HexIsDrawn(Vector2Int gridPosition)
        {
            HexTile hex = null;

            // first we check if the hex exists in the dictionary
            hexDictionary.TryGetValue(gridPosition, out hex);

            if (hex == null)
            {
                return false;
            }

            FusedMesh fused = null;

            // then we check if the hex visualdata has a fused mesh
            BaseFusedMeshes.TryGetValue(hex.VisualData, out fused);

            if (fused == null)
            {
                return false;
            }
            else
            {
                // finally we check if the hex mesh is in the fused mesh
                return BaseFusedMeshes[hex.VisualData].
                                                    HasMesh(hex.GetHashCode());
            }
        }

        public bool IsInsideBounds(Bounds bounds)
        {
            if (bounds.Intersects(WorldBounds))
            {
                return true;
            }

            return false;
        }

        private void AddVisualData(HexTile hex)
        {
            ThrowIfNotInChunk(hex);

            FusedMesh fused = null;

            BaseFusedMeshes.TryGetValue(hex.VisualData, out fused);

            // if the hex visual data is not already in the dictionary, create a new fused mesh for the new visual data and add it to the dictionary
            // if it is, add the hex mesh to the fused mesh
            
            if (fused == null)
            {
                fused = new FusedMesh();
                fused.InsertMesh(hex.GetMesh(), hex.GetHashCode(), hex.LocalPosition);

                BaseFusedMeshes.Add(hex.VisualData, fused);
            }
            else
            {
                BaseFusedMeshes[hex.VisualData].InsertMesh(hex.GetMesh(), hex.GetHashCode(), hex.LocalPosition);
            }
        }
        private void RemoveVisualData(HexTile hex)
        {
            ThrowIfNotInChunk(hex);

            FusedMesh fused = null;

            BaseFusedMeshes.TryGetValue(hex.VisualData, out fused);

            if (fused != null)
            {
                fused.RemoveMesh(hex.GetHashCode());
            }
        }  

        public void UpdateVisualData(HexTile hex)
        {
            ThrowIfNotInChunk(hex);

            RemoveVisualData(hex);

            AddVisualData(hex);
        }

        /// <summary>
        /// This will remove all the hexes that are currently in this chunk in a list provided
        /// </summary>
        /// <param name="hexList"></param>
        public void RemoveChunkHexesFromExternalList(Dictionary<Vector2Int, HexTile> hexList)
        {
            foreach (HexTile item in hexes)
            {
                hexList.Remove(item.GridPosition);
            }
        }

        public void ChangeColor(HexTile hex, Color newColor)
        {
            ThrowIfNotInChunk(hex);

            // first we remove the hex mesh
            RemoveVisualData(hex);

            // we then change its visual data
            hex.VisualData.SetColor(newColor);

            // then we add it back to the fused mesh
            AddVisualData(hex);
            
            // draw the change
            DrawFusedMeshes();
        }
        public void HighlightHex(HexTile hex, Color color)
        {
            ThrowIfNotInChunk(hex);

            Mesh hMesh = hexSettings.GetInnerHighlighter();

            hMesh.SetFullColor(color);

            HighlightLayer.LayerFusedMesh.InsertMesh(hMesh,
                hex.GetHashCode(), hex.LocalPosition);

            HighlightLayer.UpdateMesh();

        }
        public void UnHighlightHex(HexTile hex)
        {
            ThrowIfNotInChunk(hex);

            bool removed = HighlightLayer.LayerFusedMesh.
                                        RemoveMesh(hex.GetHashCode());

            if (removed)
            {
                 HighlightLayer.UpdateMesh();
            }
        }

        public void ActivateHexBorder(HexTile hex, int side, Color color)
        {
            ThrowIfNotInChunk(hex);

            if (side < 0 || side > 5)
            {
                throw new ArgumentOutOfRangeException("sides must be between 0 and 5");
            }

            // create array of sides and colors
            int[] sides = new int[1] { side };
            Color[] colors = new Color[1] { color };

            // call other method

            ActivateHexBorders(hex, sides, colors);
        }

        public void DeactivateHexBorder(HexTile hex, int side)
        {
            ThrowIfNotInChunk(hex);

            if (side < 0 || side > 5)
            {
                throw new ArgumentOutOfRangeException("sides must be between 0 and 5");
            }

            // create array of sides and colors
            int[] sides = new int[1] { side };

            DeactivateHexBorders(hex, sides);
        }

        public void ActivateHexBorders(HexTile hex, int[] sides, Color[] colors)
        {
            ThrowIfNotInChunk(hex);

            Mesh hMesh;

            int offsetMultiplier = 1;

            FusedMesh bMesh = BorderLayer.LayerFusedMesh;

            if (bMesh.HasMesh(hex.GetHashCode()))
            {
                // since we dont actually store which sides of the hex are activated, we must get the mesh and modify the sides respectively
                hMesh = bMesh.GetMesh(hex.GetHashCode());
                hexSettings.AddOuterHighlighter(hMesh, sides, colors);

                // the reason we do this is because, since we are getting the mesh from the fused mesh, the position or vertices are already offset, thus, we do not need to offset the mesh again when resetting
                offsetMultiplier = 0;
            }
            else
            {
                hMesh = hexSettings.GetBaseOuterHighlighter();
                hexSettings.AddOuterHighlighter(hMesh, sides, colors);
            }

            bMesh.InsertMesh(hMesh,
                                     hex.GetHashCode(), 
                                     hex.LocalPosition * offsetMultiplier);

            BorderLayer.UpdateMesh();
        }
        public void DeactivateHexBorders(HexTile hex, int[] sides)
        {
            ThrowIfNotInChunk(hex);

            FusedMesh bMesh = BorderLayer.LayerFusedMesh;

            Mesh hMesh;

            if (bMesh.HasMesh(hex.GetHashCode()))
            {
                hMesh = bMesh.GetMesh(hex.GetHashCode());
            }
            else
            {
                return;
            }

            hexSettings.RemoveOuterHighlighter(hMesh, sides);

            if(hMesh.triangles.Length > 0)
            {
                // since we are removing a mesh that has already been fused, said mesh vertices are already offset
                bMesh.InsertMesh(hMesh,
                                     hex.GetHashCode(), Vector3.zero);
            }
            else
            {
                // if the mesh has no triangles, remove it from the border entirely since it will be drawing nothing
                bMesh.RemoveMesh(hex.GetHashCode());
            }

            BorderLayer.UpdateMesh();
        }
        public void ActivateAllHexBorders(HexTile hex, Color[] colors)
        {
            if(colors.Length != 6)
            {
                throw new ArgumentException("Your Color array must be of size 6. 1 color for each side");
            }

            ThrowIfNotInChunk(hex);

            FusedMesh bMesh = BorderLayer.LayerFusedMesh;

            if (bMesh.HasMesh(hex.GetHashCode()))
            {
                // if the hex already has a mesh, we remove it and add it the one we want
                bMesh.RemoveMesh(hex.GetHashCode());
            }

            Mesh hMesh = hexSettings.GetBaseOuterHighlighter();

            hexSettings.AddOuterHighlighter(hMesh, 
                new int[] { 0, 1, 2, 3, 4, 5 }, colors);

            bMesh.InsertMesh(hMesh,
                                     hex.GetHashCode(), hex.LocalPosition);

            BorderLayer.UpdateMesh();
        }
        public void ActivateAllHexBorders(HexTile hex, Color color)
        {
            ThrowIfNotInChunk(hex);

            Color[] colors = Enumerable.Repeat(color, 6).ToArray();

            ActivateAllHexBorders(hex, colors);

        }
        public void DeactivateAllHexBorders(HexTile hex)
        {
            ThrowIfNotInChunk(hex);

            FusedMesh bMesh = BorderLayer.LayerFusedMesh;

            if (bMesh.HasMesh(hex.GetHashCode()))
            {
                // if the hex already has a mesh, we remove it and add it the one we want
                bMesh.RemoveMesh(hex.GetHashCode());

                bool removed = bMesh.RemoveMesh(hex.GetHashCode());

                if (removed)
                {
                    BorderLayer.UpdateMesh();
                }
            }
        }

        private void ThrowIfNotInChunk(HexTile hex)
        {
            if (!hexDictionary.ContainsKey(hex.GridPosition))
            {
                throw new HexException(hex.GridPosition, HexException.ErrorType.NotInChunk);
            }
        }
        private void SetMaterialProperties()
        {
            blocks.Clear();
            materials.Clear();

            for (int i = 0; i < BaseFusedMeshes.Count; i++)
            {
                HexVisualData data = BaseFusedMeshes.Keys.ElementAt(i);

                switch (data.VisualOption)
                {
                    case HexVisualData.HexVisualOption.Color:
                        AddMaterial_Color(data);
                        break;
                    case HexVisualData.HexVisualOption.BaseTextures:
                    case HexVisualData.HexVisualOption.AllTextures:
                        AddMaterial_Texture(data);
                        break;
                    default:
                        break;
                }
            }
        }
        private void AddMaterial_Color(HexVisualData data)
        {
            Renderer renderer = GetComponent<Renderer>();
            Material newMat = new Material(MainMaterial);

            Color color = data.HexColor;
            float lerp = data.WeatherLerp;

            MaterialPropertyBlock block = new MaterialPropertyBlock();

            block.SetFloat("_UseColor", 1);
            block.SetColor("_Color", color);
            newMat.SetFloat("_Text2Lerp", lerp);

            blocks.Add(block);

            materials.Add(newMat);
        }
        private void AddMaterial_Texture(HexVisualData data)
        {
            Material newMat = new Material(MainMaterial);

            Texture2D texture = data.BaseTexture;

            Texture texture2 = data.OverlayTexture1;

            float lerp = data.WeatherLerp;

            newMat.SetTexture("_MainTex", texture);

            if (texture2 != null)
            {
                newMat.SetTexture("_Texture1", texture2);
                newMat.SetFloat("_Text2Lerp", lerp);
            }

            // we must maintain a 1 to 1 relation between the materials and block arrays because blocks are assigned to materials via index
            blocks.Add(new MaterialPropertyBlock());
            // texture.lerp
            materials.Add(newMat);
        }
        private void SetMaterialPropertyBlocks()
        {
            Renderer renderer = GetComponent<Renderer>();

            renderer.materials = materials.ToArray();

            // for the time being, this will only adjust the HexColor of the material
            for (int i = 0; i < blocks.Count; i++)
            {
                // do not apply empty blocks, can lead to visual bugs
                if (!blocks[i].isEmpty)
                {
                    renderer.SetPropertyBlock(blocks[i], i);
                }

            }
        }

        public class ChunkException : Exception
        {
            public Vector2Int GridPosition { get; private set; }

            public Vector2 WorldPosition { get; private set; }

            public ChunkException(Vector2Int gridPosition) :
                        base(GetMessage(gridPosition.ToString(), false))
            {
                GridPosition = gridPosition;

                WorldPosition = Vector2.one * -1;
            }

            public ChunkException(Vector2 worldPosition) :
                     base(GetMessage(worldPosition.ToString(), true))
            {
                WorldPosition = worldPosition;

                GridPosition = Vector2Int.one * -1;
            }

            public void LogMessage()
            {
                Debug.LogError(Message);
            }

            private static string GetMessage(string position, bool isWorldPos)
            {
                string WorldOrGrid = "";

                if (isWorldPos)
                {
                    WorldOrGrid = "World";
                }
                else
                {
                    WorldOrGrid = "Grid";
                }

                string message = $"Chunk at {WorldOrGrid} Position ({position}) Not Found In Grid";

                return message;
            }
        }

        #region The Below is for Gpu Mesh generation, Dont touch unless you know what
        // the limit for graphic instances is 1000
        private static int maxLimit = 500;
        
        List<List<MyInstanceData>> data2 = new List<List<MyInstanceData>>();
        List<List<Vector4>> color2 = new List<List<Vector4>>();
        MyInstanceData[] data;
        public struct MyInstanceData
        {
            public Matrix4x4 objectToWorld; // We must specify object-to-world transformation for each instance
            public uint renderingLayerMask; // In addition we also like to specify rendering layer mask per instence.

            public int hexIndex;
        };

        private void SetData()
        {
            // Data
            data = new MyInstanceData[hexes.Count];
            data2.Clear();

            Vector3 transformOffset = transform.position;

            Parallel.For(0, hexes.Count, x =>
                {
                    MyInstanceData d = new MyInstanceData();
                    d.objectToWorld = Matrix4x4.Translate(hexes[x].LocalPosition + transformOffset);
                    d.renderingLayerMask = 0;

                    d.hexIndex = x;
                    data[x] = d;
                });

            while (data.Any())
            {
                data2.Add(data.Take(maxLimit).ToList());

                data = data.Skip(maxLimit).ToArray();
            }
        }

        private void SetColor()
        {
            color2.Clear();
            Vector4[] aColor;

            foreach (List<MyInstanceData> item in data2)
            {
                aColor = new Vector4[item.Count];

                Parallel.For(0, item.Count, x =>
                {
                    Color col =
                    hexes[item[x].hexIndex].VisualData.HexColor;;

                    aColor[x] = col;
                });

                color2.Add(aColor.ToList());
            }
        }

        Mesh instanceMesh;
        MaterialPropertyBlock instanceBlock;
        public void  DrawInstanced()
        {
            if (data2.Count == 0)
            {
                SetData();
                instanceMesh = hexes[0].GetMesh();
            }

            SetColor();

            int i = 0;

            foreach (List<MyInstanceData> item in data2)
            {
                instanceBlock = new MaterialPropertyBlock();

                Vector4[] v = color2.ElementAt(i).ToArray();

                instanceBlock.SetVectorArray("_MeshColors", v);
                instanceParam.matProps = instanceBlock;

                Graphics.RenderMeshInstanced(instanceParam, instanceMesh, 0, item);

                i++;
            }
        }

        #endregion

    }


    public class LayeredMesh
    {
        public GameObject LayerGameObject { get; set; }
        public FusedMesh LayerFusedMesh { get; set; }
        public int OrderInLayer
        {
            get
            {
                return meshRenderer.sortingOrder;
            }
            set
            {
                meshRenderer.sortingOrder = value;
            }
        }

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        

        HexChunk layerChunk;
        public LayeredMesh(HexChunk chunk, string layerName, int layer,                         Material material, int order = 0)
        {
            LayerGameObject = new GameObject(layerName);
            LayerGameObject.transform.SetParent(chunk.transform);
            LayerGameObject.transform.position = chunk.transform.position;
           
            layerChunk = chunk;

            meshRenderer = LayerGameObject.AddComponent<MeshRenderer>();
            meshFilter = LayerGameObject.AddComponent<MeshFilter>();

            LayerFusedMesh = new FusedMesh();

            meshRenderer.material = material;

            meshRenderer.sortingLayerID = layer;
            meshRenderer.sortingOrder = order;
        }

        public void UpdateMesh()
        {
            if (LayerGameObject != null)
            {
                if (LayerFusedMesh != null)
                {
                    meshFilter.mesh = LayerFusedMesh.Mesh;
                }
            }
        }      
    }

}
