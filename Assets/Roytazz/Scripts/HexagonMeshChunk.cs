using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Roytazz.HexMesh
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class HexagonMeshChunk : MonoBehaviour
    {
        public Vector2 ChunkCoords;

        public void SetMesh(Mesh mesh) {
            Material material = Resources.Load("VertexColorShader", typeof(Material)) as Material;
            GetComponent<MeshRenderer>().sharedMaterial = material;
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}