using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.WorldMap
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HexChunk : MonoBehaviour
    {
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        public List<HexTile> hexes;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshCollider = GetComponent<MeshCollider>();

            hexes = new List<HexTile>();
        }
        public void AddHex(HexTile hex)
        {
            hexes.Add(hex);
            hex.transform.SetParent(transform);
        }

        public void CombinesMeshes()
        {
            MeshFilter[] meshFilters = hexes.Select(h => h.GetComponent<MeshFilter>()).ToArray();
            
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
            }

            meshFilter.mesh = new Mesh();

            meshFilter.mesh.CombineMeshes(combine);
            meshCollider.sharedMesh = meshFilter.mesh;

        }

        public void DestroyAllChildren()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }


        public void DrawChunk()
        {
            CombinesMeshes();

            //transform.position = hexes[0].transform.localToWorldMatrix.GetPosition() * 10;
        }
    }
}
