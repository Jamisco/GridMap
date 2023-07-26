using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Example : MonoBehaviour
{
    Mesh mesh;
    List<Vector3> vertices;
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices.ToList();
    }

    void Update()
    {
        if(vertices.Count < 200)
        {
            vertices.Add(vertices[vertices.Count - 1] + Vector3.up * 2);          
        }
        
        // assign the local vertices array into the vertices array of the Mesh.
        mesh.vertices = vertices.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}