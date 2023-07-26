using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

namespace Assets.Scripts.WorldMap
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridMapMesh : MonoBehaviour
    {
        List<Vector3> Vertices;
        List<int> Triangles;
        List<Color> colors;

        Mesh mesh;
        MeshCollider meshCollider;
        private void Awake()
        {
            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            colors = new List<Color>();
            
            mesh = GetComponent<MeshFilter>().mesh;
            meshCollider = GetComponent<MeshCollider>();
        }

        public void RemoveHex(HexTile hex)
        {
            Vector3 initPos = hex.transform.position + HexTile.VertexCorners[0];

            int index = Vertices.IndexOf(initPos);

            if (index != -1)
            {
                Vertices.RemoveRange(index, 6);
                Triangles.RemoveRange(index * 2, 12);
                colors.RemoveRange(index, 6);
            }
            else
            {
                Debug.LogError("Hex does not exist in the list");
            }
        }
        
        private List<Vector3> GetHexVertices(HexTile hex)
        {
            List<Vector3> hexPos = new List<Vector3>();

            for (int i = 0; i < 6; i++)
            {
                hexPos.Add(hex.transform.position + HexTile.VertexCorners[i]);
            }

            return hexPos;
        }
        public void AddHex(HexTile hex)
        {
            List<Vector3> hexPos = GetHexVertices(hex);

            // This means that the hex already exists in the list
            if (HasHex(hexPos))
            {
                RemoveHex(hex);
            }

            Triangles.AddRange(HexTile.SetInnerTriangles(Vertices.Count));

            AddTriangleColor(hex.InnerHexColor);
            Vertices.AddRange(hexPos);
        }

        public void ReAddHex(HexTile hex)
        {
            List<Vector3> hexPos = GetHexVertices(hex);
            int index = Vertices.IndexOf(hexPos[0]);

            // This means that the hex already exists in the list
            if (HasHex(hexPos))
            {
                RemoveHex(hex);

                Triangles.InsertRange(index * 2, HexTile.SetInnerTriangles(Vertices.Count));

                Vertices.InsertRange(index, hexPos);

                AddTriangleColor(Color.green, index);
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Given a list of hexVertices, determine whether the hex is in the mesh
        /// </summary>
        /// <param name="hexVertices"></param>
        /// <returns></returns>
        private bool HasHex(List<Vector3> hexVertices)
        {
            int initIndex = Vertices.IndexOf(hexVertices[0]);

            if (initIndex != -1)
            {
                for (int i = initIndex; i < initIndex + 6; i++)
                {
                    // essentially,what we are looking for is if the next 6 hexVertices match each other. Every hex, will have thesame hexVertices in thesame index position in the array
                    if (Vertices[i] != hexVertices[i - initIndex])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public bool HaxHex(HexTile hex)
        {
            return HasHex(GetHexVertices(hex));
        }

        public void SetColor(HexTile hex)
        {
            RemoveHex(hex);

            DrawMesh();
        }

        public void DrawMesh()
        {

        }
       
        void AddTriangleColor(Color color, int index = -1)
        {
            if(index == -1)
            {
                for (int i = 0; i < 6; i++)
                {
                    colors.Add(color);
                }
            }
            else
            {
                for (int i = index; i < index + 6; i++)
                {
                    colors.Insert(i, color);
                }
            }

        }
    }
}
