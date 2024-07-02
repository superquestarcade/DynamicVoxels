using UnityEngine;

namespace Metatron.Utilities
{
	public static class MathExtensions
	{
		public static float ClampAngle(this float _angle)
		{
			return (_angle % 360) + (_angle < 0 ? 360 : 0);
		}

		public static float ClampAngle(this float _angle, float _min, float _max)
		{
			var clampedAngle = _angle.ClampAngle();
			var clampedMin = _min.ClampAngle(); 
			var clampedMax = _max.ClampAngle();
			if (clampedMax - clampedMin == 0) return clampedAngle;
			
			// _max -60 = 300
			// _min 60 = 60
			// Range 60 to 300
			// clampedMax - clampedMin = 240 > 0
			// Clamp range should fall on the clampedMin + (clampedMax-clampedMin)/2 side of the orientation
			// This side of the orientation does not share 0
			if (clampedMax - clampedMin > 0)
				return Mathf.Clamp(clampedAngle, clampedMin, clampedMax);

			// _max 60 = 60
			// _min -60 = 300
			// Range 300 to 60
			// clampedMax - clampedMin = -240 < 0
			// Clamp range should fall on the clampedMin + ((360-clampedMin) + clampedMax)/2 side of the orientation
			// This side of the orientation shares 0
			if (clampedAngle >= clampedMin || clampedAngle <= clampedMax) return clampedAngle;
			var midAngle = (180 + clampedMin + ((360 - clampedMin) + clampedMax) / 2).ClampAngle();
			return clampedAngle > midAngle ? clampedMin : clampedMax;
		}
	}
}