using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoxelEngine;

namespace Player
{
	[RequireComponent(typeof(Chunk))]
	public class VoxelModelLoader : MonoBehaviourPlus
	{
		[SerializeField] private Chunk modelChunk;
		[SerializeField] private string modelFilePath = "/Assets/VoxModels/Player/player.vox";
		[SerializeField] private bool loadOnValidate = false;
        [SerializeField] private float blockSize = 1;

		private void OnValidate()
		{
			if (!loadOnValidate) return;
			LoadVoxModel();
			loadOnValidate = false;
		}

		private void LoadVoxModel()
		{
			Debug.Assert(modelFilePath.EndsWith(".vox"));
			var blocks = Vox.LoadModel(Application.dataPath + modelFilePath);
			if (blocks == null)
			{
				Debug.LogError($"VoxelModelLoader.LoadVoxModel unable to load model {modelFilePath}");
				return;
			}
			modelChunk.InitializeAsObject(blocks.GetLength(0), blocks.GetLength(1), blocks.GetLength(2));
			GetMeshDataFromBlocks(blocks, out var vertices, out var tris, out var colors);
            modelChunk.SetMesh(vertices, tris, colors);
		}

		private void GetMeshDataFromBlocks(int[,,] _blocks, out Vector3[] _vertices, out int[] _tris,
			out Color32[] _colors, bool _center = true)
		{
			var sides = 0;
            var vertices = new List<Vector3> ();
            var colors = new List<Color32> ();
            var tris = new List<int> ();
            var blocks = _blocks;
            var width = _blocks.GetLength(0) * blockSize;
            var height = _blocks.GetLength(1) * blockSize;
            var depth = _blocks.GetLength(2) * blockSize;
            var vertexOffset = (_center ? (new Vector3(width, height, depth)/2f) - (Vector3.one * blockSize) : Vector3.zero);
            Debug.Log($"VoxelModelLoader.GetMeshDataFromBlocks vertexOffset {vertexOffset}");

            // Block structure
            // BLOCK: [R-color][G-color][B-color][0][00][back_left_right_above_front]
            //           8bit    8bit     8it    1bit(below-face)  2bit(floodfill)     5bit(faces)

            // Reset faces
            var chunkYorg = modelChunk.ToY;
            var chunkMaxY = modelChunk.ToY;
            for (var y = modelChunk.FromY; y < modelChunk.ToY; y++) {
                var empty = true;
                for (var x = modelChunk.FromX; x < modelChunk.ToX; x++) {
                    for (var z = modelChunk.FromZ; z < modelChunk.ToZ; z++) {
                        if (blocks [x, y, z] != 0) {
                            blocks [x, y, z] &= ~(1 << 0);
                            blocks [x, y, z] &= ~(1 << 1);
                            blocks [x, y, z] &= ~(1 << 2);
                            blocks [x, y, z] &= ~(1 << 3);
                            blocks [x, y, z] &= ~(1 << 4);
                            blocks [x, y, z] &= ~(1 << 7);
                            empty = false;
                        }
                    }
                }
                if(empty) {
                    chunkMaxY = y;
                }
            }

            //		if (chunk.ffActive) {
            //		if(chunk.Type == Chunk.Type_WORLD){
            //			Debug.Log ("CHUNK MAX Y: " + chunkMaxY);
            //			chunk.ToY = chunkMaxY;
            //		}

            // Check blocks if occluded & set block data accordingly
            for (var y = modelChunk.FromY; y < modelChunk.ToY; y++) {
                for (var x = modelChunk.FromX; x < modelChunk.ToX; x++) {
                    for (var z = modelChunk.FromZ; z < modelChunk.ToZ; z++) {
                        if ((blocks [x, y, z] >> 8) == 0) {
                            continue; // Skip empty blocks
                        }
                        int left = 0, right = 0, above = 0, front = 0, back = 0, below = 0;
                        if (z > 0) {
                            if (blocks [x, y, z - 1] != 0) { 
                                back = 1;
                                blocks [x, y, z] = blocks [x, y, z] | 0x10;
                            }
                        }
                        if (x > 0) {
                            if (blocks [x - 1, y, z] != 0) {
                                left = 1;
                                blocks [x, y, z] = blocks [x, y, z] | 0x8;
                            }
                        }
                        if (modelChunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                            if(y > modelChunk.ToY ) {
                                if (blocks [x, y - 1, z] != 0) {
                                    below = 1;
                                    blocks [x, y - 1, z] = blocks [x, y, z] | 0x80;
                                }
                            }
                            if(x < modelChunk.ToX-1) {
                                if (blocks [x + 1, y, z] != 0) {
                                    right = 1;
                                    blocks [x, y, z] = blocks [x, y, z] | 0x4;
                                }
                            }
                        } else {
                            below = 1;
                            if (x < width - 1) {
                                if (blocks [x + 1, y, z] != 0) {
                                    right = 1;
                                    blocks [x, y, z] = blocks [x, y, z] | 0x4;
                                }
                            }
                        }
                        if (y < modelChunk.ToY - 1) {
                            if (blocks [x, y + 1, z] != 0) {
                                above = 1;
                                blocks [x, y, z] = blocks [x, y, z] | 0x2;
                            }
                        }
                        if (modelChunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                            if(z < modelChunk.ToZ - 1) {
                                if (blocks [x, y, z + 1] != 0) {
                                    front = 1;
                                    blocks [x, y, z] = blocks [x, y, z] | 0x1;
                                }
                            }	
                        } else {
                            if (z < depth - 1) {
                                if (blocks [x, y, z + 1] != 0) {
                                    front = 1;
                                    blocks [x, y, z] = blocks [x, y, z] | 0x1;
                                }
                            }
                        }

                        // If we are building a standalone mesh, remove invisible
                        if (front == 1 && left == 1 && right == 1 && above == 1 && back == 1 && below == 1) {
                            if (modelChunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                                blocks [x, y, z] = 0;
                            }
                            continue; // block is hidden
                        }
                        // Draw block tris & verts
                        if(below == 0) {
                            if((blocks[x,y,z] & 0x80) == 0) {
                                var maxX = 0;
                                var maxZ = 0;

                                for(var x_ = x; x_ < modelChunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x80) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpZ = 0;
                                    for (var z_ = z; z_ < modelChunk.ToZ; z_++) {
                                        if ((blocks [x_, y, z_] & 0x80) == 0 && SameColor (blocks [x_, y, z_], blocks [x, y, z])) {
                                            tmpZ++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if (tmpZ < maxZ || maxZ == 0) {
                                        maxZ = tmpZ;
                                    }
                                }
                                for (var x_ = x; x_ < x + maxX; x_++) {
                                    for (var z_ = z; z_ < z + maxZ; z_++) {
                                        blocks [x_, y, z_] = blocks [x_, y, z_] | 0x80;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                var idx = vertices.Count;
                                
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize- blockSize, z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3 (x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize)-vertexOffset);
                                vertices.Add ( new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize + (blockSize * maxZ))-vertexOffset);

                                // Add triangle indeces
                                tris.Add(idx+2);
                                tris.Add(idx+1);
                                tris.Add(idx);

                                idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize-blockSize, z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);

                                tris.Add(idx+2);
                                tris.Add(idx+1);
                                tris.Add(idx);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (above == 0) {
                            // Get above (0010)
                            if ((blocks [x, y, z] & 0x2) == 0) {
                                var maxX = 0;
                                var maxZ = 0;

                                for (var x_ = x; x_ < modelChunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x2) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpZ = 0;
                                    for (var z_ = z; z_ < modelChunk.ToZ; z_++) {
                                        if ((blocks [x_, y, z_] & 0x2) == 0 && SameColor (blocks [x_, y, z_], blocks [x, y, z])) {
                                            tmpZ++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if (tmpZ < maxZ || maxZ == 0) {
                                        maxZ = tmpZ;
                                    }
                                }
                                for (var x_ = x; x_ < x + maxX; x_++) {
                                    for (var z_ = z; z_ < z + maxZ; z_++) {
                                        blocks [x_, y, z_] = blocks [x_, y, z_] | 0x2;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize, z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3 (x * blockSize - blockSize, y * blockSize, z * blockSize - blockSize)-vertexOffset);
                                vertices.Add ( new Vector3(x * blockSize - blockSize, y * blockSize, z * blockSize + (blockSize * maxZ)) -vertexOffset);

                                // Add triangle indeces
                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize, z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize, z * blockSize - blockSize )-vertexOffset);


                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (back == 0) {
                            // back  10000
                            if ((blocks [x, y, z] & 0x10) == 0) {
                                var maxX = 0;
                                var maxY = 0;

                                for (var x_ = x; x_ < modelChunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x10) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < modelChunk.ToY; y_++) {
                                        if ((blocks [x_, y_, z] & 0x10) == 0 && SameColor (blocks [x_, y_, z], blocks [x, y, z])) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if (tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for (var x_ = x; x_ < x + maxX; x_++) {
                                    for (var y_ = y; y_ < y + maxY; y_++) {
                                        blocks [x_, y_, z] = blocks [x_, y_, z] | 0x10;
                                    }
                                }
                                maxX--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize + (blockSize * maxY), z * blockSize - blockSize)-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize - blockSize)-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);

                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                idx = vertices.Count;


                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize + (blockSize * maxY), z * blockSize - blockSize)-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize - blockSize)-vertexOffset);

                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (front == 0) {
                            // front 0001
                            if ((blocks [x, y, z] & 0x1) == 0) {
                                var maxX = 0;
                                var maxY = 0;

                                for (var x_ = x; x_ < modelChunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x1) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < modelChunk.ToY; y_++) {
                                        if ((blocks [x_, y_, z] & 0x1) == 0 && SameColor (blocks [x_, y_, z], blocks [x, y, z])) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if (tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for (var x_ = x; x_ < x + maxX; x_++) {
                                    for (var y_ = y; y_ < y + maxY; y_++) {
                                        blocks [x_, y_, z] = blocks [x_, y_, z] | 0x1;
                                    }
                                }
                                maxX--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize + (blockSize * maxY), z * blockSize)-vertexOffset);
                                vertices.Add (new Vector3( x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize )-vertexOffset);
                                vertices.Add (new Vector3( x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize )-vertexOffset);

                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3( x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize )-vertexOffset);
                                vertices.Add (new Vector3( x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize )-vertexOffset);
                                vertices.Add (new Vector3( x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize )-vertexOffset);

                                // Add triangle indeces
                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (left == 0) {
                            if ((blocks [x, y, z] & 0x8) == 0) {
                                var maxZ = 0;
                                var maxY = 0;

                                for (var z_ = z; z_ < modelChunk.ToZ; z_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x, y, z_] & 0x8) == 0 && SameColor (blocks [x, y, z_], blocks [x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < modelChunk.ToY; y_++) {
                                        if ((blocks [x, y_, z_] & 0x8) == 0 && SameColor (blocks [x, y_, z_], blocks [x, y, z])) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if (tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for (var z_ = z; z_ < z + maxZ; z_++) {
                                    for (var y_ = y; y_ < y + maxY; y_++) {
                                        blocks [x, y_, z_] = blocks [x, y_, z_] | 0x8;
                                    }
                                }
                                maxZ--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ))-vertexOffset);

                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize - blockSize)-vertexOffset);

                                // Add triangle indeces
                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);


                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (right == 0) {
                            if ((blocks [x, y, z] & 0x4) == 0) {
                                var maxZ = 0;
                                var maxY = 0;

                                for (var z_ = z; z_ < modelChunk.ToZ; z_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x, y, z_] & 0x4) == 0 && SameColor (blocks [x, y, z_], blocks [x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < modelChunk.ToY; y_++) {
                                        if ((blocks [x, y_, z_] & 0x4) == 0 && SameColor (blocks [x, y_, z_], blocks [x, y, z])) {
                                            tmpY++;
                                        } else {
                                            break;
                                        }
                                    }
                                    if (tmpY < maxY || maxY == 0) {
                                        maxY = tmpY;
                                    }
                                }
                                for (var z_ = z; z_ < z + maxZ; z_++) {
                                    for (var y_ = y; y_ < y + maxY; y_++) {
                                        blocks [x, y_, z_] = blocks [x, y_, z_] | 0x4;
                                    }
                                }
                                maxZ--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize, y * blockSize - blockSize, z * blockSize + (blockSize * maxZ) )-vertexOffset);
                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3(x * blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ))-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize, y * blockSize - blockSize, z * blockSize - blockSize )-vertexOffset);
                                vertices.Add (new Vector3(x * blockSize, y * blockSize + (blockSize * maxY), z * blockSize - blockSize )-vertexOffset);

                                // Add triangle indeces
                                tris.Add(idx);
                                tris.Add(idx+1);
                                tris.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                    }
                }
            }

            _vertices = vertices.ToArray();
            _tris = tris.ToArray();
            _colors = colors.ToArray();
		}
        
        private bool SameColor(int _block1, int _block2)
        {
            return ((_block1 >> 8) & 0xFFFFFF) == ((_block2 >> 8) & 0xFFFFFF) && _block1 != 0 && _block2 != 0;
        }
	}
}