using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine
{
	public class ProceduralWorld : MonoBehaviourPlus
	{
		[SerializeField] private int dirtDepth = 7;
		public void Landscape(ref int[,,] _worldBlocks)
		{
			var width = _worldBlocks.GetLength(0);
			var depth = _worldBlocks.GetLength(2);
			if(DebugMessages) Debug.Log($"ProceduralWorld.Landscape width {width}, depth {depth}");
			// Draw ground (dirt + grass layers)
			for(var x = 0; x < width; x++) 
				for (var z = 0; z < depth; z++)
					FlatTerrainAtPosition(ref _worldBlocks, x, z, dirtDepth);
		}

		private void FlatTerrainAtPosition(ref int[,,] _worldBlocks, int _xPos, int _zPos, int _dirtDepth)
		{
			for (var y = 0; y < _dirtDepth; y++)
			{
				var col = ((float)y / _dirtDepth) <= 0.5f ? WorldColors.dirtColor1 : WorldColors.dirtColor2;
				_worldBlocks[_xPos, y, _zPos] = col;
			}

			var grassColor = (Random.value > 0.95) ? WorldColors.grassColor1 : WorldColors.grassColor2;
			_worldBlocks[_xPos, _dirtDepth, _zPos] = grassColor;
			if (debugLevel<DebugLevel.All) return;
			if (_xPos != 0 || _zPos != 0) return;
			var depthCheckValue = new List<int>();
			for(var i=0;i<_dirtDepth;i++) depthCheckValue.Add(_worldBlocks[_xPos, i, _zPos]);
			depthCheckValue.Add(_worldBlocks[_xPos, _dirtDepth, _zPos]);
			Debug.Log($"ProceduralWorld.FlatTerrainAtPosition depth data check: {string.Join(", ", depthCheckValue)}");
		}
	}
}