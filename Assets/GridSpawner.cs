using Assets.Scripts.WorldMap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.WorldMap.GridManager;

public class GridSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    public GridManager manager;

    private void Awake()
    {
        manager = GetComponent<GridManager>();
        manager.GenerateGrid();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.timeSinceLevelLoad > 5)
        {
            manager.DrawChunkInstanced();
        }
    }
}
