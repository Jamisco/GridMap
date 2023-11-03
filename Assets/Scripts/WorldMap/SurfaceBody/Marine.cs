using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.WorldMap.PlanetGenerator;

namespace Assets.Scripts.WorldMap.Biosphere
{
    [System.Serializable]
    public class Marine : SurfaceBody
    {
        public override BiomeData GetBiomeData(GridValues grid)
        {
            return GetBiomeData(Biomes.Ocean);
        }
    }
}
