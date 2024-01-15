using Assets.Scripts.Miscellaneous;
using Assets.Scripts.WorldMap;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.GridManager;
using static Assets.Scripts.WorldMap.HexTile;
using static Assets.Scripts.WorldMap.HexTile.HexVisualData;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(GridManager))]
public class GridSpawner : MonoBehaviour
{
    public GridManager manager;
    public PlanetGenerator planet;
    public GridData gridData;

    public GameObject spriteParent;
    public Sprite spriteToSpawn;

    Vector2Int mapSize;

    public bool useColor = true;
    public bool disableUnseen = true;
    private void Awake()
    {
        
    }
    void Start()
    {
        Begin();
    }

    private void Update()
    {
        OnMouseClick();
        OnMouseClick2();
        //OnMouseClick2();
        
        if (disableUnseen)
        {
            DisableUnseenChunks();
        }
    }

    private void OnValidate()
    {
        if (!disableUnseen)
        {
            if(manager != null)
            {
                manager.SetAllChunksStatus(true);
            }
        }

        layer = SortingLayer.NameToID(layerName);

        if(layer == 0)
        {
            layer = 1111111;
        }
    }

    void Begin()
    {
        ClearMap();
        manager = GetComponent<GridManager>();
        planet = GetComponent<PlanetGenerator>();

        List<HexVisualData> visualData;

        planet.MainPlanet.PlanetSize = gridData.GridSize;
        planet.GenerateData();
        
        mapSize = gridData.GridSize;  
        visualData = ConvertToHexVisual(planet.GetAllBiomes());
        manager.InitializeGrid(gridData, visualData);
        manager.InitiateDrawProtocol();

    }

    public float generateTime = 0f;

    public void CanvasGenerate(Vector2Int size)
    {
        manager = GetComponent<GridManager>();
        planet = GetComponent<PlanetGenerator>();

        List<HexVisualData> visualData;

        gridData.GridSize = size;
        planet.MainPlanet.PlanetSize = gridData.GridSize;
        planet.GenerateData();

        mapSize = gridData.GridSize;
        visualData = ConvertToHexVisual(planet.GetAllBiomes());
        manager.InitializeGrid(gridData, visualData);
        manager.ImmediateDrawGrid();

        generateTime = manager.time;
    }
    void SetHexVisualData()
    {
        planet.GenerateData();

        List<BiomeData> datas = planet.GetAllBiomes();

        for(int x = 0; x < mapSize.x; x++)
        {
            for(int y = 0; y < mapSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                BiomeData data = planet.GetBiomeData(pos);

                HexVisualData hData = ConvertToHexVisual(data);

                manager.SetVisualData(pos, hData);
            }
        }
    }

    private List<HexVisualData> ConvertToHexVisual(List<BiomeData> datas)
    {
        List<HexVisualData> hexVisuals = new List<HexVisualData>();

        foreach (BiomeData data in datas)
        {
            hexVisuals.Add(ConvertToHexVisual(data));
        }

        return hexVisuals;
    }

    private HexVisualData ConvertToHexVisual(BiomeData data)
    {
        HexVisualData hData = new HexVisualData(data.BiomeColor,
                              data.SeasonTexture, null, 0f);
        
        if (useColor)
        {
            hData.SetVisualOption(HexVisualOption.Color);
        }
        else
        {
            hData.SetVisualOption(HexVisualOption.BaseTextures);
        }

        return hData;
    }


    Dictionary<int, HexData> HighlightedHexes = new Dictionary<int, HexData>();
    Dictionary<int, HexData> ActivatedBorderHexes = new Dictionary<int, HexData>();


    public int layer;
    public string layerName;
    private void OnMouseClick()
    {
        HexData newData;
        Vector3 mousePos;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            mousePos = GetMousePosition();

            if (!manager.PositionHasDrawnHex(mousePos))
            {
                return;
            }
            
            newData = 
                manager.GetHexData(mousePos);

            if (newData.IsNullOrEmpty())
            {
                return;
            }

            newData.Test();

            Debug.Log("Hex at position: "
                + newData.GridCoordinates.ToString());
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            mousePos = GetMousePosition();

            if (!manager.PositionHasDrawnHex(mousePos))
            {
                return;
            }
            
            newData =
               manager.GetHexData(mousePos);

            if (!newData.IsNullOrEmpty())
            {
                newData.AddLayer(layer);
            }

            //newData.ActivateAllBorders(Color.yellow);

            // check if hex exists before highligthing, actibing border et
        }
    }

    int side = 0;
    private void OnMouseClick2()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mousePos = GetMousePosition();

            if (!manager.PositionHasDrawnHex(mousePos))
            {
                return;
            }

            HexData data = manager.GetHexData(mousePos);

            //Color[] colors = new Color[6] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };

            //data.ActivateAllBorders(colors);

            //data.Test();
        }
        
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector3 mousePos = GetMousePosition();

            if (!manager.PositionHasDrawnHex(mousePos))
            {
                return;
            }

            HexData data = manager.GetHexData(mousePos);

            data.DeactivateAllBorders();
        }
    }

    private void DisableUnseenChunks()
    {
        Bounds bounds = Camera.main.OrthographicBounds3D();

        manager.SetChunkStatusIfInBoundsOtherwise(bounds, true);
    }
    public void HighlightHex(HexData hex)
    {
        if (hex.IsNullOrEmpty())
        {
            return;
        }

        if (!HighlightedHexes.ContainsKey(hex.Hash))
        {
            HighlightedHexes.Add(hex.Hash, hex);
            // hex.Highlight();
            hex.ChangeColor(Color.black);
        }
    }
    public void UnHighlightHex(HexData hex)
    {
        if (hex.IsNullOrEmpty())
        {
            return;
        }

        if (HighlightedHexes.ContainsKey(hex.Hash))
        {
            HighlightedHexes.Remove(hex.Hash);
            // hex.UnHighlight();
            //hex.UnHighlight();

            Color ogColor = Color.red;

            hex.ChangeColor(ogColor);
        }
    }

    private Vector3 GetMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // The ray hit something, return the point in world space
            return hit.point;
        }
        else
        {
            // The ray didn't hit anything, you might return a default position or handle it as needed
            return Vector3.negativeInfinity;
        }
    }

    public void ClearMap()
    {
        if (manager != null)
        {
            manager.ClearMap();
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(GridSpawner))]
    public class ClassButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridSpawner exampleScript = (GridSpawner)target;

            if (GUILayout.Button("Generate Grid"))
            {
                exampleScript.ClearMap();
                exampleScript.Begin();
            }
        }
    }
#endif
}
