using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Assets.Scripts.Miscellaneous.HexFunctions;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Assets.Scripts.WorldMap
{
    public class HexChunk
    {
        public List<HexTile> hexes;
        private List<SimplifiedHex> simpleHex;

        public Dictionary<int, List<HexTile>> hexRows
            = new Dictionary<int, List<HexTile>>();

        // since the hexes positions are set regardless of the position of the chunk, we simply spawn the chunk at 0,0
        public static Matrix4x4 SpawnPosition = Matrix4x4.Translate(Vector3.zero);

        public Mesh mesh;
        public HexChunk()
        {
            mesh = new Mesh();
            hexes = new List<HexTile>();
            simpleHex = new List<SimplifiedHex>();
        }
        public void AddHex(HexTile hex)
        {
            hexes.Add(hex);

            if (!hexRows.ContainsKey(hex.GridCoordinates.y))
            {
                hexRows.Add(hex.GridCoordinates.y, new List<HexTile>());
                hexRows[hex.GridCoordinates.y].Add(hex);
            }
            else
            {
                hexRows[hex.GridCoordinates.y].Add(hex);
            }
        }
        public void CombinesMeshes()
        {
            Simplify();
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

        public void Simplify()
        {
            mesh = new Mesh();

            CombineInstance[] combine = new CombineInstance[hexes.Count];

            for (int i = 0; i < hexes.Count; i++)
            {
                combine[i].mesh = hexes[i].DrawMesh();

                // set first 6 uv index 

                Vector2[] tempUv = combine[i].mesh.uv;

                //for (int j = 0; j < 6; j++)
                //{
                //    tempUv[j] = HexUV[j];
                //}

                //combine[i].mesh.uv = tempUv;

                combine[i].transform = Matrix4x4.Translate(hexes[i].Position);
            }

            mesh.CombineMeshes(combine);
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
