using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelEngine
{
	public class GrowingChunk : Chunk
	{
		[Header("Settings")]
		[SerializeField] private float blockSize = 1f;
		
		[Header("Growth")]
		[SerializeField] private Vector3Int bounds = Vector3Int.one * 20;
		[SerializeField] private float growthRate = 3f;

		private float spawnTime, lastGrowthTime;

		private Vector3Int previousGrowthPoint;

		private void Start()
		{
			spawnTime = Time.time;
			InitializeAsObject(bounds.x, bounds.y, bounds.z);
			AddSeedling();
		}

		private void Update()
		{
			if(Time.time - lastGrowthTime > growthRate) AddRandomGrowth();
		}

		private void AddSeedling()
		{
			var startPoint = new Vector3Int(Mathf.RoundToInt(bounds.x / 2f), 0, Mathf.RoundToInt(bounds.z / 2f));
			Blocks[startPoint.x, startPoint.y, startPoint.z] = WorldColors.stemColor;
			previousGrowthPoint = startPoint;
			this.RebuildChunk(blockSize);
		}

		private void AddRandomGrowth()
		{
			if (!this.IsWithinBounds(previousGrowthPoint + Vector3Int.up)) return;
			var growthPoint = previousGrowthPoint;
			var growthColor = 0;
			if (Random.Range(0, 5) > 0)
			{
				// Stem
				growthPoint += Vector3Int.up;
				growthColor = WorldColors.stemColor;
				previousGrowthPoint = growthPoint;
			}
			else
			{
				// Leaf
				growthPoint = previousGrowthPoint.Adjacent();
				growthColor = WorldColors.flowerStemColor;
			}
			Blocks[growthPoint.x, growthPoint.y, growthPoint.z] = growthColor;
			this.RebuildChunk(blockSize);
			lastGrowthTime = Time.time;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position, bounds);
		}
	}
}