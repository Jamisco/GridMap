using UnityEngine;
using System;

namespace Roytazz.HexMesh
{
	[Serializable]
	public class Noise
	{
		public Vector2 Offset;
		public AnimationCurve ScalingCurve;
		public int Seed = 2525;

		[Range(0.1f, 2f)]
		public float Frequency = 0.1f;
		[Range(1, 8)]
		public int Octaves = 3;
		[Range(0f, 10f)]
		public float Lacunarity = 2.0f;
		[Range(0f, 1f)]
		public float Gain = 0.2f;

		private FastNoise _noise {
			get {
				if (_fastNoise == null) {
					_fastNoise = new FastNoise(Seed);
					_fastNoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
					//Multiply by .005 so that value in inspector has more precision/freedom, since we need a rly small number
					_fastNoise.SetFrequency(Frequency * .005f);
					_fastNoise.SetFractalOctaves(Octaves);
					_fastNoise.SetFractalLacunarity(Lacunarity);
					_fastNoise.SetFractalGain(Gain);
				}
				return _fastNoise;
			}
		}
		private FastNoise _fastNoise;

		//Setting the noise to null. That way the next time its called, it will initialize with the new property values
		public void ReInitialize() => _fastNoise = null;

		/// <summary></summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>A value between 0 and 1</returns>
		public float GetNoise(Vector2 vector) => GetNoise(vector.x, vector.y);
		/// <summary></summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns>A value between 0 and 1</returns>
		public float GetNoise(float x, float y) {
			float noise = _noise.GetNoise(x + Offset.x, y + Offset.y);

			//Noise value is currently between -1 and 1. We normalize the noise between 0 and 1
			noise = (noise + 1) / 2;
			if (ScalingCurve != null && ScalingCurve.keys.Length > 0)
				return ScalingCurve.Evaluate(noise);
			else
				return noise;
		}
    }
}