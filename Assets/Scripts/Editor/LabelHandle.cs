using UnityEngine;
using System.Collections;
using UnityEditor;
using Assets.Scripts.WorldMap;

// Create a 180 degrees wire arc with a ScaleValueHandle attached to the disc
// lets you visualize some info of the transform

[CustomEditor(typeof(HandleExample))]
class LabelHandle : Editor
{
    void OnSceneGUI()
    {
        HandleExample handleExample = (HandleExample)target;
        HexTile hex = handleExample.GetComponent<HexTile>(); ;

        if (handleExample == null)
        {
            return;
        }

        Handles.color = Color.red;

        Handles.Label(hex.Position + Vector3.up * 3,
                      "\nPosition: " + hex.Position.ToString() +
                      "\nCoordinate: " + hex.AxialCoordinates.ToString() +
            "\nShieldArea: " +
            handleExample.shieldArea.ToString());

        Handles.BeginGUI();
        if (GUILayout.Button("Reset Area", GUILayout.Width(100)))
        {
            handleExample.shieldArea = 5;
        }
        Handles.EndGUI();


        //Handles.DrawWireArc(hex.Position,
        //    handleExample.transform.up,
        //    -handleExample.transform.right,
        //    180,
        //    handleExample.shieldArea);
        //handleExample.shieldArea =
        //    Handles.ScaleValueHandle(handleExample.shieldArea,
        //        handleExample.transform.position + handleExample.transform.forward * handleExample.shieldArea,
        //        handleExample.transform.rotation,
        //        1,
        //        Handles.ConeHandleCap,
        //        1);
    }
}