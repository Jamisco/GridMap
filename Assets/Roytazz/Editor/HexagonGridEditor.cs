using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Roytazz.HexMesh
{
    [CustomEditor(typeof(HexagonGrid))]
    public class HexagonGridEditor : Editor
    {
        public override void OnInspectorGUI() {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Terrain"))
                (target as HexagonGrid).GenerateGrid();
            if (GUILayout.Button("Clear Terrain"))
                (target as HexagonGrid).ClearGrid();
            GUILayout.EndHorizontal();

            DrawDefaultInspector();
        }
    }
}