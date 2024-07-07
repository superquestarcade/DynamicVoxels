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
			var startPoint = Vector3Int.zero;
			SetBlockLocal(startPoint.x, startPoint.y, startPoint.z, WorldColors.stemColor);
			// Blocks[startPoint.x, startPoint.y, startPoint.z] = WorldColors.stemColor;
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

			SetBlockLocal(growthPoint.x, growthPoint.y, growthPoint.z, growthColor);
			// Blocks[growthPoint.x, growthPoint.y, growthPoint.z] = growthColor;
			this.RebuildChunk(blockSize);
			lastGrowthTime = Time.time;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			var blockPosOffset = new Vector3((FromX + ToX) / 2f, (FromY + ToY) / 2f, (FromZ + ToZ) / 2f);
			Gizmos.DrawWireCube(transform.position + blockPosOffset, bounds);
		}
	}
}