using System;
using UnityEngine;

namespace VoxelEngine
{
	public class ChunkFill : Chunk
	{
		[Header("Settings")]
		[SerializeField] private float blockSize = 1f;
		
		[Header("Growth")]
		[SerializeField] private Vector3Int bounds = Vector3Int.one * 20;
		private void Start()
		{
			InitializeAsObject(bounds.x, bounds.y, bounds.z);
			for(var x=0; x<Blocks.GetLength(0);x++)
				for(var y=0; y<Blocks.GetLength(1);y++)
					for (var z = 0; z < Blocks.GetLength(2); z++)
						Blocks[x, y, z] = WorldColors.fruitColor;
			this.RebuildChunk(blockSize);
		}
	}
}