using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityMeshSimplifier;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Assets.Scripts.WorldMap
{
    public class HexChunk : MonoBehaviour
    {
        public List<HexTile> hexes;
        private List<SimplifiedHex> simpleHex;

        public Dictionary<int, List<HexTile>> hexRows
            = new Dictionary<int, List<HexTile>>();

        public Matrix4x4 SpawnPosition;

        private Mesh mesh;
        private void Awake()
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

        public void Simplify()
        {
            foreach (var row in hexRows)
            {
                simpleHex.Add(new SimplifiedHex(row.Value));
            }
        }

        public static bool simplify = true;

        public static float Triangles = 0;
        public static float Vertices = 0;

        public void CombinesMeshes()
        {
            if(simplify)
            {

                Simplify();

                // since the hexes positions are set regardless of the position of the chunk, we simply spawn the chunk at 0,0
                SpawnPosition = Matrix4x4.Translate(Vector3.zero);

                CombineInstance[] combine = new CombineInstance[simpleHex.Count];

                for (int i = 0; i < simpleHex.Count; i++)
                {
                    combine[i].mesh = simpleHex[i].mesh;

                    combine[i].transform = Matrix4x4.Translate(Vector3.zero);
                }

                mesh.CombineMeshes(combine);
            }
            else
            {
                SpawnPosition = Matrix4x4.Translate(Vector3.zero);
                CombineInstance[] combine = new CombineInstance[hexes.Count];

                for (int i = 0; i < hexes.Count; i++)
                {
                    combine[i].mesh = hexes[i].HexMesh;

                    combine[i].transform = Matrix4x4.Translate(hexes[i].Position);
                }

                mesh.CombineMeshes(combine);
            }

            Triangles += mesh.triangles.Length / (float) 3 / (float)1000;
            Vertices += mesh.vertices.Length / (float) 1000;

            //transform.position = SpawnPosition.GetColumn(3);
        }
        
        public static RenderParams rp;
        public void DrawChunk()
        {
            //foreach (SimplifiedHex ah in simpleHex)
            //{
            //    Graphics.RenderMesh(rp, ah.mesh, 0, Matrix4x4.Translate(Vector3.zero));
            //}

            Graphics.RenderMesh(rp, mesh, 0, SpawnPosition);
        }
    }


    public struct SimplifiedHex
    {
        public List<HexTile> hexRows;
        
        public List<Vector3> Vertices;
        public List<int> Triangles;

        public Vector3 Position;

        public Mesh mesh;
        public SimplifiedHex(List<HexTile> rows)
        {
            Vertices = new List<Vector3>();
            Triangles = new List<int>();

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

        private void Simplify()
        {
            HexTile hex;

            Vector2 size = HexTile.hexSettings.HexSize;

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

                // add top and bottom part of hex
                foreach (int num in topIndex)
                {
                    Vertices.Add(hex.GetWorldVertexPosition(num));
                    Triangles.Add(Vertices.Count - 1);
                }

                foreach (int num in botIndex)
                {
                    Vertices.Add(hex.GetWorldVertexPosition(num));
                    Triangles.Add(Vertices.Count - 1);
                }
            }

            // add the 2 mid main triangles
            Vertices.Add(leftBot); // -4
            Vertices.Add(leftTop); // - 3
            Vertices.Add(rightBot); // - 2
            Vertices.Add(rightTop); // -1

            Triangles.Add(Vertices.Count - 4);
            Triangles.Add(Vertices.Count - 3);
            Triangles.Add(Vertices.Count - 1);

            Triangles.Add(Vertices.Count - 4);
            Triangles.Add(Vertices.Count - 1);
            Triangles.Add(Vertices.Count - 2);

            mesh.vertices = Vertices.ToArray();
            mesh.triangles = Triangles.ToArray();

        }
        
    }
}
