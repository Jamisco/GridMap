using Assets.Scripts.WorldMap;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.WorldMap.Biosphere.SurfaceBody;
using static Assets.Scripts.WorldMap.GridManager;
using static Assets.Scripts.WorldMap.HexTile;
using static Assets.Scripts.WorldMap.HexTile.HexVisualData;
using static UnityEngine.GraphicsBuffer;

public class GridSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    public GridManager manager;
    public PlanetGenerator planet;
    public GridData gridData;

    Vector2Int mapSize;

    public bool useColor = true;
    private void Awake()
    {
        
    }
    void Start()
    {
        Begin();
    }

    void Begin()
    {
        manager = GetComponent<GridManager>();
        planet = GetComponent<PlanetGenerator>();

        List<HexVisualData> visualData;

        mapSize = gridData.MapSize;
        
        planet.MainPlanet.PlanetSize = gridData.MapSize;
        planet.GenerateData();

        visualData = ConvertToHexVisual(planet.GetAllBiomes());

        manager.InitializeGrid(gridData, visualData);
        
        manager.GenerateGrid();

        //SetHexVisualData();

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
                              data.WeatherTexture, null, 0f);
        
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



    // Update is called once per frame
    void Update()
    {
        //if(Time.timeSinceLevelLoad > 5)
        //{
        //    manager.DrawChunkInstanced();
        //}
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
                exampleScript.Begin();
            }
        }
    }
#endif
}
