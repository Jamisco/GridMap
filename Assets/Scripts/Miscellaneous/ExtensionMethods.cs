using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
        /// 
        public static void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
        public static Bounds OrthographicBounds3D(this Camera camera)
        {
            float screenAspect = camera.aspect;
            float cameraHeight = camera.orthographicSize * 2;

            Vector3 position = camera.transform.localPosition;

            position.y = 0;

            Bounds bounds = new Bounds(position,
                            new Vector3(cameraHeight * screenAspect, 0,                                 cameraHeight));
            return bounds;
        }

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

        public static void RemoveRange<T>(this List<T> collection, int startIndex, int count)
        {
            if (collection.Count > startIndex)
            {
                collection.RemoveRange(startIndex, count);
            }
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

        /// <summary>
        /// Will log the given milliseconds in a readable format. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeInMilliseconds"></param>
        public static void LogTimer(string message, float timeInMilliseconds)
        {
            Debug.Log(ParseLogTimer(message, timeInMilliseconds));
        }

        public static string ParseLogTimer(string message, float timeInMilliseconds)
        {
            int minutes;
            float seconds;

            string log = "";

            if (timeInMilliseconds >= 60000)
            {
                minutes = (int)(timeInMilliseconds / 60000);
                seconds = (timeInMilliseconds % 60000) / 1000f;
                log = $"{message} {minutes} minutes {seconds} seconds";
            }
            else
            {
                log = $"{message} {timeInMilliseconds / 1000f} seconds";
            }

            return log;
        }

        public static bool TryRemoveElementsInRange<TValue>([DisallowNull] this IList<TValue> list, int index, int count, [NotNullWhen(false)] out Exception error)
        {
            try
            {
                if (list is List<TValue> genericList)
                {
                    genericList.RemoveRange(index, count);
                }
                else
                {
                    if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
                    if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                    if (list.Count - index < count) throw new ArgumentException("index and count do not denote a valid range of elements in the list");

                    for (var i = count; i > 0; --i)
                        list.RemoveAt(index);
                }
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }

            error = null;
            return true;
        }


        public static Mesh CloneMesh(this Mesh parent)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = parent.vertices;
            mesh.triangles = parent.triangles;
            mesh.uv = parent.uv;
            mesh.normals = parent.normals;
            mesh.tangents = parent.tangents;
            mesh.colors = parent.colors;
            mesh.bindposes = parent.bindposes;
            mesh.boneWeights = parent.boneWeights;
            mesh.subMeshCount = parent.subMeshCount;
            mesh.name = parent.name;
            mesh.bounds = parent.bounds;
            mesh.indexFormat = parent.indexFormat;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
