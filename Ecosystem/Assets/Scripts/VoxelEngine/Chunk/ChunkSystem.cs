using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine
{
	public static class VoxelSystem
	{
		public static void RebuildChunk(this Chunk _chunk, float _blockSize)
		{
            if (_chunk.Type is Chunk.TypeWorld)
            {
                Debug.LogError("This method is not designed to handle world chunks, only single chunks local to their own trasform");
                return;
            }
            var sides = 0;
            var vertices = new List<Vector3> ();
            var colors = new List<Color32> ();
            var tri = new List<int> ();
            var blocks = _chunk.Blocks;

            // Block structure
            // BLOCK: [R-color][G-color][B-color][0][00][back_left_right_above_front]
            //           8bit    8bit     8it    1bit(below-face)  2bit(floodfill)     5bit(faces)

            // Reset faces
            var chunkYorg = _chunk.ToY;
            var chunkMaxY = _chunk.ToY;
            for (var y = 0; y < blocks.GetLength(1); y++) {
                var empty = true;
                for (var x = 0; x < blocks.GetLength(0); x++) {
                    for (var z = 0; z < blocks.GetLength(2); z++) {
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

            // Check blocks if occluded & set block data accordingly
            for (var y = 0; y < blocks.GetLength(1); y++) {
                for (var x = 0; x < blocks.GetLength(0); x++) {
                    for (var z = 0; z < blocks.GetLength(2); z++) {
                        if ((blocks [x, y, z] >> 8) == 0) {
                            continue; // Skip empty blocks_
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
                        if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                            if(y > blocks.GetLength(1) ) {
                                if (blocks [x, y - 1, z] != 0) {
                                    below = 1;
                                    blocks [x, y - 1, z] = blocks [x, y, z] | 0x80;
                                }
                            }
                            if(x < blocks.GetLength(0)-1) {
                                if (blocks [x + 1, y, z] != 0) {
                                    right = 1;
                                    blocks [x, y, z] = blocks [x, y, z] | 0x4;
                                }
                            }
                        }
                        if (y < blocks.GetLength(1) - 1) {
                            if (blocks [x, y + 1, z] != 0) {
                                above = 1;
                                blocks [x, y, z] = blocks [x, y, z] | 0x2;
                            }
                        }
                        if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                            if(z < blocks.GetLength(2) - 1) {
                                if (blocks [x, y, z + 1] != 0) {
                                    front = 1;
                                    blocks [x, y, z] = blocks [x, y, z] | 0x1;
                                }
                            }	
                        }

                        // If we are building a standalone mesh, remove invisible
                        if (front == 1 && left == 1 && right == 1 && above == 1 && back == 1 && below == 1) {
                            if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                                blocks [x, y, z] = 0;
                            }
                            continue; // block is hidden
                        }
                        // Draw block tris & verts
                        if(below == 0) {
                            if((blocks[x,y,z] & 0x80) == 0) {
                                var maxX = 0;
                                var maxZ = 0;

                                for(var x_ = x; x_ < blocks.GetLength(0); x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x80) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpZ = 0;
                                    for (var z_ = z; z_ < blocks.GetLength(2); z_++) {
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

                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize- _blockSize, z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3 (x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize) - _chunk.LocalPosOffset);
                                vertices.Add ( new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize + (_blockSize * maxZ))  - _chunk.LocalPosOffset);

                                // Add triangle indeces
                                tri.Add(idx+2);
                                tri.Add(idx+1);
                                tri.Add(idx);

                                idx = vertices.Count;

                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize-_blockSize, z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);

                                tri.Add(idx+2);
                                tri.Add(idx+1);
                                tri.Add(idx);

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

                                for (var x_ = x; x_ < blocks.GetLength(0); x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x2) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpZ = 0;
                                    for (var z_ = z; z_ < blocks.GetLength(2); z_++) {
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

                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize, z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3 (x * _blockSize - _blockSize, y * _blockSize, z * _blockSize - _blockSize) - _chunk.LocalPosOffset);
                                vertices.Add ( new Vector3(x * _blockSize - _blockSize, y * _blockSize, z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;

                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize, z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);


                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

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

                                for (var x_ = x; x_ < blocks.GetLength(0); x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x10) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < blocks.GetLength(1); y_++) {
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

                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize + (_blockSize * maxY), z * _blockSize - _blockSize) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize - _blockSize, z * _blockSize - _blockSize) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;


                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize + (_blockSize * maxY), z * _blockSize - _blockSize) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize - _blockSize) - _chunk.LocalPosOffset);

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

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

                                for (var x_ = x; x_ < blocks.GetLength(0); x_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x_, y, z] & 0x1) == 0 && SameColor (blocks [x_, y, z], blocks [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < blocks.GetLength(1); y_++) {
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

                                vertices.Add (new Vector3(x * _blockSize + (_blockSize * maxX), y * _blockSize + (_blockSize * maxY), z * _blockSize) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3( x * _blockSize - _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3( x * _blockSize + (_blockSize * maxX), y * _blockSize - _blockSize, z * _blockSize ) - _chunk.LocalPosOffset);

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3( x * _blockSize - _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3( x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3( x * _blockSize + (_blockSize * maxX), y * _blockSize - _blockSize, z * _blockSize ) - _chunk.LocalPosOffset);

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

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

                                for (var z_ = z; z_ < blocks.GetLength(2); z_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x, y, z_] & 0x8) == 0 && SameColor (blocks [x, y, z_], blocks [x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < blocks.GetLength(1); y_++) {
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

                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize - _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize - _blockSize) - _chunk.LocalPosOffset);

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);


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

                                for (var z_ = z; z_ < blocks.GetLength(2); z_++) {
                                    // Check not drawn + same color
                                    if ((blocks [x, y, z_] & 0x4) == 0 && SameColor (blocks [x, y, z_], blocks [x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < blocks.GetLength(1); y_++) {
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

                                vertices.Add (new Vector3(x * _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize, y * _blockSize - _blockSize, z * _blockSize + (_blockSize * maxZ) ) - _chunk.LocalPosOffset);
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3(x * _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize + (_blockSize * maxZ)) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize, y * _blockSize - _blockSize, z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);
                                vertices.Add (new Vector3(x * _blockSize, y * _blockSize + (_blockSize * maxY), z * _blockSize - _blockSize ) - _chunk.LocalPosOffset);

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

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
            if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                if (_chunk.Type == Chunk.TypeFf) _chunk.SetMass(0.1f * vertices.Count);
            }
            _chunk.SetMesh(vertices.ToArray(), tri.ToArray(), colors.ToArray());
            //_chunk.gameObject.transform.position = new Vector3(_chunk.Position.x, _chunk.Position.y, _chunk.Position.z);
            _chunk.ToY = chunkYorg;
		}
        
        public static bool SameColor(int _block1, int _block2)
        {
            return ((_block1 >> 8) & 0xFFFFFF) == ((_block2 >> 8) & 0xFFFFFF) && _block1 != 0 && _block2 != 0;
        }

        public static bool IsWithinBounds(this Chunk _chunk, Vector3Int _value)
        {
            if (_value.x < _chunk.FromX || _value.x > _chunk.ToX) return false;
            if (_value.y < _chunk.FromY || _value.y > _chunk.ToY) return false;
            if (_value.z < _chunk.FromZ || _value.z > _chunk.ToZ) return false;
            return true;
        }
	}
}