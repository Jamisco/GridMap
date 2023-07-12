using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.WorldMap
{
    public class SpriteContainer : MonoBehaviour
    {
        public string MainTextureName;
        public string LightName;

        public Dictionary<float, Sprite> VegetationSprites = new Dictionary<float, Sprite>();

        private static Vector2 newPivot = new(0, 0); // this is = pivot.bottom in inspector
        public Sprite CreateVegation(Sprite aSprite, Material material, float percentIntensity)
        {
            return Sprite.Create(material.GetTexture(MainTextureName) as Texture2D, aSprite.rect, newPivot, aSprite.pixelsPerUnit / .87f, 1, SpriteMeshType.Tight, aSprite.border);
        }

        public Sprite GetSpriteWithPercent(float percentIntensity)
        {
            return VegetationSprites.TryGetValue(percentIntensity, out Sprite sprite) ? sprite : null;
        }


        public float minAlpha;
        public float maxAlpha;
        public float NormalizeAlpha(float percentIntensity)
        {
            float range = maxAlpha - minAlpha;

            float normalized = percentIntensity * range;

            return minAlpha + normalized;
        }
    }
}
