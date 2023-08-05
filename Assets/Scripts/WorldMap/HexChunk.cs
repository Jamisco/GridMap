using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Assets.Scripts.WorldMap
{
    public class HexChunk : MonoBehaviour
    {
        public List<HexTile> hexes;

        public Matrix4x4 SpawnPosition;

        private Mesh mesh;
        private void Awake()
        {
            mesh = new Mesh();
            hexes = new List<HexTile>();
        }
        public void AddHex(HexTile hex)
        {
            hexes.Add(hex);
        }

        public void CombinesMeshes()
        {
            SpawnPosition = Matrix4x4.Translate(hexes[0].Position);
            
            CombineInstance[] combine = new CombineInstance[hexes.Count];

            for (int i = 0; i < hexes.Count; i++)
            {
                combine[i].mesh = hexes[i].HexMesh;

                combine[i].transform = Matrix4x4.Translate(hexes[i].Position);
            }

            mesh.CombineMeshes(combine);
        }

        public static RenderParams rp;
        public void DrawChunk()
        {
            Graphics.RenderMesh(rp, mesh, 0, SpawnPosition);
        }
    }
}
