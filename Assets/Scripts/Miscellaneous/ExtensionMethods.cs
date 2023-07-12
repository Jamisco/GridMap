using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace Assets.Scripts.Miscellaneous
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Will return the first component in all of the objects children of the given type,
        /// with the given name.Do not recommend for use in performance sensitive scenarios
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameObject"></param>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public static T GetComponentByName<T>(this GameObject gameObject, string componentName, bool includeActive = true) where T : Component
        {
            return gameObject.GetComponentsInChildren<T>(includeActive)
             .First(x => x.name.Equals(componentName));
        }

        public static T GetComponentByName<T>(this Component gameObject, string componentName, bool includeActive = true) where T : Component
        {
            return gameObject.GetComponentsInChildren<T>(includeActive)
             .First(x => x.name.Equals(componentName));
        }


        public static GameObject GetGameObject(this GameObject gameObject, string objectName)
        {
            return gameObject.GetComponentByName<Transform>(objectName).gameObject;
        }

        private static Random random = new Random(Environment.TickCount);
        private static readonly object syncLock = new object();
        // https://csharpindepth.com/Articles/Random
        // why you should lock your random generator

        /// <summary>
        /// Gets a Random number being -1 and 1. These functions are ass do not use
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public static double NextDouble(this Random RandGenerator, double MinValue, double MaxValue)
        {
            lock (syncLock)
            {
                return RandGenerator.NextDouble() * (MaxValue - MinValue) + MinValue;
            }
        }

        public static float NextFloat(this Random RandGenerator, float MinValue, float MaxValue)
        {            
            float ran = (float)(RandGenerator.NextDouble() * (MaxValue - MinValue) + MinValue);

            return ran;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="minRange">min range of new value</param>
        /// <param name="maxRange">max range of new value</param>
        /// <returns></returns>
        public static float Normalize(float value, float minValue, float maxValue, float minRange, float maxRange)
        {
            // Calculate the range of the input values
            float valueRange = maxValue - minValue;

            // Normalize the value
            float normalizedValue = (value - minValue) / valueRange;
            normalizedValue = (normalizedValue * (maxRange - minRange)) + minRange;

            return normalizedValue;
        }
    }
}
