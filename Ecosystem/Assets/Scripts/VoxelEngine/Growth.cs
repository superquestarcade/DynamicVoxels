using UnityEngine;

namespace VoxelEngine
{
	public static class Growth
	{
		public static Vector3Int Adjacent(this Vector3Int _block)
		{
			var randomIndex = Random.Range(0, 4);
			return randomIndex switch
			{
				0 => _block + Vector3Int.left,
				1 => _block + Vector3Int.forward,
				2 => _block + Vector3Int.right,
				3 => _block + Vector3Int.back,
				_ => default,
			};
		}
	}
}