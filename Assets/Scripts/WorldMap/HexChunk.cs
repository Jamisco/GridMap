using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.GridManager;
using static Assets.Scripts.WorldMap.HexTile;
using static UnityEditor.FilePathAttribute;
using Debug = UnityEngine.Debug;

namespace Assets.Scripts.WorldMap
{
    public class HexChunk
    {
        public List<HexTile> hexes;
        private ConcurrentBag<HexTile> conHexes;
        private List<SimplifiedHex> simpleHex;

        public Dictionary<int, List<HexTile>> hexRows
            = new Dictionary<int, List<HexTile>>();

        // since the hexes positions are set regardless of the position of the chunk, we simply spawn the chunk at 0,0
        public static Matrix4x4 SpawnPosition = Matrix4x4.Translate(Vector3.zero);

        public Mesh mesh;

        public Vector2Int StartPosition;

        public BoundsInt ChunkBounds;

        public static Material MainMaterial;
        public static Material InstanceMaterial;
        
        public static BiomeVisual BiomeVisual;
        public HexChunk(BoundsInt aBounds)
        {
            ChunkBounds = aBounds;
            ChunkBounds.ClampToBounds(aBounds);

            mesh = new Mesh();
            hexes = new List<HexTile>(aBounds.size.x * aBounds.size.y);
            
            // the reason we use a concurrent bag is because it is thread safe
            // thus you can add to it from multiple from threads
            conHexes = new ConcurrentBag<HexTile>();
            simpleHex = new List<SimplifiedHex>();
        }

        public bool IsInChunk(HexTile hex)
        {
            if (ChunkBounds.Contains((Vector3Int)hex.GridCoordinates))
            {
                return true;
            }

            return false;
        }

        public bool IsInChunk(int x, int y)
        {
            // its wrong for some reason, idk why but you must use bounds
            Bounds hey = new Bounds(ChunkBounds.center, ChunkBounds.size);

            if (hey.Contains(new Vector3Int(x, y, 0)))
            {
                return true;
            }

            return false;
        }
        public void AddHex(HexTile hex)
        {
            // the reason we use a concurrent bag is because it is thread safe
            // thus you can add to it from multiple from threads

            conHexes.Add(hex);
        }

        Dictionary<BiomeProperties, List<CombineInstance>> subMeshes = new Dictionary<BiomeProperties, List<CombineInstance>>();
        public void CombinesMeshes()
        {
            subMeshes.Clear();
            hexes = conHexes.ToList();
            Simplify();

            //hexes.Sort((a, b) =>
            //{
            //    int compareY = a.Y.CompareTo(b.Y);
            //    return compareY == 0 ? a.X.CompareTo(b.X) : compareY;
            //});

            //hexes = hexes.OrderBy(o => o.AxialCoordinates).ToList();
        }

        public void DrawImmediate()
        {
            subMeshes.Clear();
            hexes = conHexes.ToList();
            DrawMesh();
        }

        List<CombineInstance> instances = new List<CombineInstance>();
        private void DrawMesh()
        {
            Material newMat;
            RenderParams rp;
            MaterialPropertyBlock block;
            
            mesh = new Mesh();
         
            CombineInstance instance;

            for (int i = 0; i < hexes.Count; i++)
            {
                instance = new CombineInstance();
                
                instance.mesh = hexes[i].DrawMesh();
                instance.transform = Matrix4x4.Translate(hexes[i].Position);
                instances.Add(instance);
                
                newMat = new Material(MainMaterial);
                Color color = hexes[i].HexBiomeProperties.BiomeColor;

                block = new MaterialPropertyBlock();

                block.SetFloat("_UseColor", 1);
                block.SetColor("_Color", color);

                blocks.Add(block);
                materials.Add(newMat);
            }

            mesh.CombineMeshes(instances.ToArray(), false, true);
            mesh.MarkDynamic();
        }

        public void UpdateMaterialBlock(ref Renderer renderer)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                renderer.GetPropertyBlock(blocks[i]);
                
                blocks[i].SetFloat("_UseColor", 1);
                blocks[i].SetColor("_Color", hexes[i].HexBiomeProperties.BiomeColor);
                
                renderer.SetPropertyBlock(blocks[i], i);
            }
        }
        private void Simplify()
        {
            mesh = new Mesh();
            subMeshes.Clear();
            
            List<CombineInstance> tempCombine = new List<CombineInstance>();
            CombineInstance instance;

            // split and store the meshes into submeshes based on their biome
            for (int i = 0; i < hexes.Count; i++)
            {
                HexTile hex = hexes[i];               

                subMeshes.TryGetValue(hex.HexBiomeProperties, out tempCombine);

                if(tempCombine == null)
                {
                    tempCombine = new List<CombineInstance>();
                    subMeshes.Add(hex.HexBiomeProperties, tempCombine);
                }

                instance = new CombineInstance();
                
                instance.mesh = hex.DrawMesh();

                List<Color> colors = new List<Color>();

                for (int c = 0; c < instance.mesh.vertexCount; c++)
                {
                    colors.Add(hex.HexBiomeProperties.BiomeColor);
                }

                instance.mesh.SetColors(colors);

                instance.transform = Matrix4x4.Translate(hexes[i].Position);
                tempCombine.Add(instance);

            }

            Mesh preMesh;

            List<CombineInstance> prelist = new List<CombineInstance>();

            // combine all the meshes of thesame biome into one
            foreach (List<CombineInstance> item in subMeshes.Values)
            {
                CombineInstance subInstance = new CombineInstance();
                preMesh = new Mesh();

                preMesh.CombineMeshes(item.ToArray());

                subInstance.mesh = preMesh;
                
                prelist.Add(subInstance);
            }

            mesh.CombineMeshes(prelist.ToArray(), false, false);
            SetMaterialProperties();
        }

        public List<Material> materials = new List<Material>();

        RenderParams renderParams = new RenderParams(MainMaterial);
        private void SetMaterialProperties()
        {
            if (BiomeVisual == BiomeVisual.Color)
            {
                SetMaterialColor();
            }
            else
            {
                SetMaterialTexture();
            }
        }

        List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();
        public void SetMaterialPropertyBlocks(ref Renderer renderer)
        {
            int count = renderer.materials.Length;

            if (blocks.Count != count)
            {
                renderer.materials = materials.ToArray();
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                renderer.SetPropertyBlock(blocks[i], i);
            }
        }

        Matrix4x4 zero = Matrix4x4.Translate(Vector3.zero);
        public struct MyInstanceData
        {
            public Matrix4x4 objectToWorld; // We must specify object-to-world transformation for each instance
            public uint renderingLayerMask; // In addition we also like to specify rendering layer mask per instence.

            public int hexIndex;
        };

        // the limit is 500
        int maxLimit = 500;

        List<List<MyInstanceData>> data2 = new List<List<MyInstanceData>>();
        MyInstanceData[] data;
        private void SetData()
        {
            // Data
            data = new MyInstanceData[hexes.Count];
            data2.Clear();

            Parallel.For(0, hexes.Count, x =>
            {
                MyInstanceData d = new MyInstanceData();
                d.objectToWorld = Matrix4x4.Translate(hexes[x].Position);
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

        List<List<Vector4>> color2 = new List<List<Vector4>>();
        private void SetColor()
        {
            color2.Clear();
            Vector4[] aColor;
            // Color
            foreach (List<MyInstanceData> item in data2)
            {
                aColor = new Vector4[item.Count];

                Parallel.For(0, item.Count, x =>
                {
                    aColor[x] = 
                    hexes[item[x].hexIndex].HexBiomeProperties.BiomeColor;
                });

                color2.Add(aColor.ToList());
            }
        }

        RenderParams instanceParam = new RenderParams(InstanceMaterial);
        Mesh instanceMesh;
        MaterialPropertyBlock instanceBlock;
        public void RenderMesh()
        {
            if (data2.Count == 0)
            {
                SetData();
                instanceMesh = hexes[0].DrawMesh();
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
        private void SetMaterialColor()
        {
            for (int i = 0; i < subMeshes.Count; i++)
            {
                Material newMat = new Material(MainMaterial);
                Color color = subMeshes.Keys.ElementAt(i).BiomeColor;

                MaterialPropertyBlock block = new MaterialPropertyBlock();

                block.SetFloat("_UseColor", 1);
                block.SetColor("_Color", color);

                blocks.Add(block);

                materials.Add(newMat);
            }
        }
        private void SetMaterialTexture()
        {
            for (int i = 0; i < subMeshes.Count; i++)
            {
                Material newMat = new Material(MainMaterial);

                Texture2D texture = subMeshes.Keys.ElementAt(i).BiomeTexture;
                
                newMat.SetTexture("_MainTex", texture);

                materials.Add(newMat);
            }
        }

    }


    public struct SimplifiedHex
    {
        public List<HexTile> hexRows;
        
        public List<Vector3> Vertices;
        public List<int> Triangles;
        List<Vector2> UV;

        public Vector3 Position;

        public Mesh mesh;
        public SimplifiedHex(List<HexTile> rows)
        {
            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            UV = new List<Vector2>();

            hexRows = rows;

            mesh = new Mesh();

            Position = rows[0].Position;

            Sort();

            Position = hexRows[0].Position;

            Simplify();
        }

        private void Sort()
        {
            // this list should already be sorted, this will be just in case
            hexRows.Sort((x, y) => x.GridCoordinates.x.CompareTo(y.GridCoordinates.x));

        }

        private static Vector2[] HexUV
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
    

        private void Simplify()
        {
            HexTile hex;

            Vector3 leftTop = Vector3.zero;
            Vector3 leftBot = Vector3.zero;
            Vector3 rightTop = Vector3.zero;
            Vector3 rightBot = Vector3.zero;

            int[] topIndex = { 5, 0, 1 };
            int[] botIndex = { 4, 2, 3 };

            for (int i = 0; i < hexRows.Count; i++)
            {
                // from here we will test it based on materials etc, for now just simplify
                hex = hexRows[i];

                // normally we would set the edges incrementally, becuase the hex might have different materials im between
                
                if (i == 0)
                {
                    leftTop = hex.GetWorldVertexPosition(5);
                    leftBot = hex.GetWorldVertexPosition(4);
                }

                if(i == hexRows.Count - 1)
                {
                    rightTop = hex.GetWorldVertexPosition(1);
                    rightBot = hex.GetWorldVertexPosition(2);
                }

                // for uv mapping, these top and bottom edges have uv which are independent of the row they are placed, so we can just add them here
                // add top and bottom part of hex
                foreach (int num in topIndex)
                {
                    Vertices.Add(hex.GetWorldVertexPosition(num));
                    UV.Add(HexUV[num]);
                    Triangles.Add(Vertices.Count - 1);
                }

                foreach (int num in botIndex)
                {
                    Vertices.Add(hex.GetWorldVertexPosition(num));
                    UV.Add(HexUV[num]);
                    Triangles.Add(Vertices.Count - 1);
                }
            }

            // add the 2 mid main triangles
            Vertices.Add(leftBot); // -4
            UV.Add(GetUV(4, 1));

            Vertices.Add(leftTop); // - 3
            UV.Add(GetUV(5, 1));

            Vertices.Add(rightBot); // - 2
            UV.Add(GetUV(2, hexRows.Count));
            
            Vertices.Add(rightTop); // -1
            UV.Add(GetUV(1, hexRows.Count));

            Triangles.Add(Vertices.Count - 4);
            Triangles.Add(Vertices.Count - 3);
            Triangles.Add(Vertices.Count - 1);

            Triangles.Add(Vertices.Count - 4);
            Triangles.Add(Vertices.Count - 1);
            Triangles.Add(Vertices.Count - 2);

            mesh.vertices = Vertices.ToArray();
            mesh.triangles = Triangles.ToArray();
            mesh.uv = UV.ToArray();

        }

        private Vector2 GetUV(int hexSide, int rowCount)
        {
            // the reason we multiply the x by the row count is because since the mesh in a collection of multiple rows, we want our texture mapping to repeat for each hex
            Vector2 uv = HexUV[hexSide];

            uv.x = uv.x * rowCount;

            return uv;
        }
    }
}
