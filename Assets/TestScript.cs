using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public MeshFilter Filter;

    public void Update()
    {
        if (!Input.GetKeyDown(KeyCode.A)) return;

        var mesh = Filter.mesh;
        var vert = new Vector3[mesh.vertices.Length];
        for (var index = 0; index < mesh.vertices.Length; index++)
        {
            vert[index] = Quaternion.Euler(45f, 0, 0) * mesh.vertices[index];
        }

        mesh.vertices = vert;
        Filter.mesh = mesh;
    }
}
