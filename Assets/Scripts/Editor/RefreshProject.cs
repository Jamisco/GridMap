using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RefreshProject : MonoBehaviour
{
    [MenuItem("Tools/ReloadProject")]
    public static void OpenProject()
    {
        EditorSceneManager.SaveOpenScenes();
        EditorApplication.OpenProject(Directory.GetCurrentDirectory());
    }
}
