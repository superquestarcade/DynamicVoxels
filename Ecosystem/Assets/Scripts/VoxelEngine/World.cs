using System.Collections.Generic;
using UnityEngine;

namespace VoxelEngine
{
    public class World : MonoBehaviourSingleton<World>
    {
        [SerializeField] private ProceduralWorld proceduralWorld;
        [SerializeField] private Chunk chunkPrefab;
    
        [SerializeField] private int floorHeight = 22; // Where to floor is in the world "height".
        [SerializeField] private int width = 30; // Number of chunks (width * chunkSize = actual world size = rWidth)
        [SerializeField] private int height = 2;
        [SerializeField] private int depth = 30; 
        [SerializeField] private int chunkSize = 32;
        private int RWidth => width * chunkSize;
        private int RDepth => depth * chunkSize;
        private int RHeight => height * chunkSize;
        [SerializeField] private int blockSize = 1;
        private bool started = false;
    
        private List<List<Vector3>> floodFill = new List<List<Vector3>>();

        private int[,,] blocks; //  = new int[width*chunkSize, height*chunkSize, depth*chunkSize];
        private Chunk[,,] chunks; // = new Chunk[width+1, height+1, depth+1];

        private List<Chunk> rebuildList = new List<Chunk>();
    
        // Start is called before the first frame update
        void Start()
        {
            // create the chunks
            CreateChunks ();

            // Uncomment to produce a procedurally built city
            proceduralWorld.Landscape(ref blocks);
            // Comment out this when using the above Landscape()
            /*MapHandler m = new MapHandler();
            Vox.LoadModel("Assets/maps/test2.vox", "map");*/

            RebuildDirtyChunks (true);
            started = true;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    
        private void CreateChunks() 
        {
            chunks = new Chunk[width, height, depth];
            for(var x = 0; x < width; x++) {
                for(var y = 0; y < height; y++) {
                    for(var z = 0; z < depth; z++)
                    {
                        var chunk = Instantiate(chunkPrefab, transform);
                        chunk.InitializeAsWorld(x,y,z,
                            x * blockSize * chunkSize,
                            y * blockSize * chunkSize,
                            z * blockSize * chunkSize,
                            x * blockSize * chunkSize + chunkSize,
                            y * blockSize * chunkSize + chunkSize,
                            z * blockSize * chunkSize + chunkSize);
                        chunks[x, y, z] = chunk;
                    }
                }
            }
            blocks = new int[width*chunkSize, height*chunkSize, depth*chunkSize];
        }
    
        private void RebuildDirtyChunks(bool _all = false) 
        {
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    for (var z = 0; z < depth; z++) {
                        if (chunks [x, y, z].Dirty || _all) {
                            RebuildChunk (chunks [x, y, z]);
                        }
                    }
                }
            }
        }
    
        private void RebuildChunk(Chunk _chunk)
        {
            var sides = 0;
            var vertices = new List<Vector3> ();
            var colors = new List<Color32> ();
            var tri = new List<int> ();
            int[,,] blocks_;

            if(_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                blocks_ = _chunk.Blocks;
            } else {
                blocks_ = blocks;
            }

            // Block structure
            // BLOCK: [R-color][G-color][B-color][0][00][back_left_right_above_front]
            //           8bit    8bit     8it    1bit(below-face)  2bit(floodfill)     5bit(faces)

            // Reset faces
            var chunkYorg = _chunk.ToY;
            var chunkMaxY = _chunk.ToY;
            for (var y = _chunk.FromY; y < _chunk.ToY; y++) {
                var empty = true;
                for (var x = _chunk.FromX; x < _chunk.ToX; x++) {
                    for (var z = _chunk.FromZ; z < _chunk.ToZ; z++) {
                        if (blocks_ [x, y, z] != 0) {
                            blocks_ [x, y, z] &= ~(1 << 0);
                            blocks_ [x, y, z] &= ~(1 << 1);
                            blocks_ [x, y, z] &= ~(1 << 2);
                            blocks_ [x, y, z] &= ~(1 << 3);
                            blocks_ [x, y, z] &= ~(1 << 4);
                            blocks_ [x, y, z] &= ~(1 << 7);
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
            for (var y = _chunk.FromY; y < _chunk.ToY; y++) {
                for (var x = _chunk.FromX; x < _chunk.ToX; x++) {
                    for (var z = _chunk.FromZ; z < _chunk.ToZ; z++) {
                        if ((blocks_ [x, y, z] >> 8) == 0) {
                            continue; // Skip empty blocks_
                        }
                        int left = 0, right = 0, above = 0, front = 0, back = 0, below = 0;
                        if (z > 0) {
                            if (blocks_ [x, y, z - 1] != 0) { 
                                back = 1;
                                blocks_ [x, y, z] = blocks_ [x, y, z] | 0x10;
                            }
                        }
                        if (x > 0) {
                            if (blocks_ [x - 1, y, z] != 0) {
                                left = 1;
                                blocks_ [x, y, z] = blocks_ [x, y, z] | 0x8;
                            }
                        }
                        if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                            if(y > _chunk.ToY ) {
                                if (blocks_ [x, y - 1, z] != 0) {
                                    below = 1;
                                    blocks_ [x, y - 1, z] = blocks_ [x, y, z] | 0x80;
                                }
                            }
                            if(x < _chunk.ToX-1) {
                                if (blocks_ [x + 1, y, z] != 0) {
                                    right = 1;
                                    blocks_ [x, y, z] = blocks_ [x, y, z] | 0x4;
                                }
                            }
                        } else {
                            below = 1;
                            if (x < (width * chunkSize) - 1) {
                                if (blocks_ [x + 1, y, z] != 0) {
                                    right = 1;
                                    blocks_ [x, y, z] = blocks_ [x, y, z] | 0x4;
                                }
                            }
                        }
                        if (y < _chunk.ToY - 1) {
                            if (blocks_ [x, y + 1, z] != 0) {
                                above = 1;
                                blocks_ [x, y, z] = blocks_ [x, y, z] | 0x2;
                            }
                        }
                        if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                            if(z < _chunk.ToZ - 1) {
                                if (blocks_ [x, y, z + 1] != 0) {
                                    front = 1;
                                    blocks_ [x, y, z] = blocks_ [x, y, z] | 0x1;
                                }
                            }	
                        } else {
                            if (z < (chunkSize * depth) - 1) {
                                if (blocks_ [x, y, z + 1] != 0) {
                                    front = 1;
                                    blocks_ [x, y, z] = blocks_ [x, y, z] | 0x1;
                                }
                            }
                        }

                        // If we are building a standalone mesh, remove invisible
                        if (front == 1 && left == 1 && right == 1 && above == 1 && back == 1 && below == 1) {
                            if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                                blocks_ [x, y, z] = 0;
                            }
                            continue; // block is hidden
                        }
                        // Draw block tris & verts
                        if(below == 0) {
                            if((blocks_[x,y,z] & 0x80) == 0) {
                                var maxX = 0;
                                var maxZ = 0;

                                for(var x_ = x; x_ < _chunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks_ [x_, y, z] & 0x80) == 0 && SameColor (blocks_ [x_, y, z], blocks_ [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpZ = 0;
                                    for (var z_ = z; z_ < _chunk.ToZ; z_++) {
                                        if ((blocks_ [x_, y, z_] & 0x80) == 0 && SameColor (blocks_ [x_, y, z_], blocks_ [x, y, z])) {
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
                                        blocks_ [x_, y, z_] = blocks_ [x_, y, z_] | 0x80;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize- blockSize, z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3 (x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize));
                                vertices.Add ( new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize + (blockSize * maxZ)) );

                                // Add triangle indeces
                                tri.Add(idx+2);
                                tri.Add(idx+1);
                                tri.Add(idx);

                                idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize-blockSize, z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));

                                tri.Add(idx+2);
                                tri.Add(idx+1);
                                tri.Add(idx);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks_ [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (above == 0) {
                            // Get above (0010)
                            if ((blocks_ [x, y, z] & 0x2) == 0) {
                                var maxX = 0;
                                var maxZ = 0;

                                for (var x_ = x; x_ < _chunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks_ [x_, y, z] & 0x2) == 0 && SameColor (blocks_ [x_, y, z], blocks_ [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpZ = 0;
                                    for (var z_ = z; z_ < _chunk.ToZ; z_++) {
                                        if ((blocks_ [x_, y, z_] & 0x2) == 0 && SameColor (blocks_ [x_, y, z_], blocks_ [x, y, z])) {
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
                                        blocks_ [x_, y, z_] = blocks_ [x_, y, z_] | 0x2;
                                    }
                                }
                                maxX--;
                                maxZ--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize, z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3 (x * blockSize - blockSize, y * blockSize, z * blockSize - blockSize));
                                vertices.Add ( new Vector3(x * blockSize - blockSize, y * blockSize, z * blockSize + (blockSize * maxZ)) );

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize, z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize, z * blockSize - blockSize ));


                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks_ [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (back == 0) {
                            // back  10000
                            if ((blocks_ [x, y, z] & 0x10) == 0) {
                                var maxX = 0;
                                var maxY = 0;

                                for (var x_ = x; x_ < _chunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks_ [x_, y, z] & 0x10) == 0 && SameColor (blocks_ [x_, y, z], blocks_ [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < _chunk.ToY; y_++) {
                                        if ((blocks_ [x_, y_, z] & 0x10) == 0 && SameColor (blocks_ [x_, y_, z], blocks_ [x, y, z])) {
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
                                        blocks_ [x_, y_, z] = blocks_ [x_, y_, z] | 0x10;
                                    }
                                }
                                maxX--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize + (blockSize * maxY), z * blockSize - blockSize));
                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize - blockSize));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;


                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize + (blockSize * maxY), z * blockSize - blockSize));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize - blockSize));

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks_ [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (front == 0) {
                            // front 0001
                            if ((blocks_ [x, y, z] & 0x1) == 0) {
                                var maxX = 0;
                                var maxY = 0;

                                for (var x_ = x; x_ < _chunk.ToX; x_++) {
                                    // Check not drawn + same color
                                    if ((blocks_ [x_, y, z] & 0x1) == 0 && SameColor (blocks_ [x_, y, z], blocks_ [x, y, z])) {
                                        maxX++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < _chunk.ToY; y_++) {
                                        if ((blocks_ [x_, y_, z] & 0x1) == 0 && SameColor (blocks_ [x_, y_, z], blocks_ [x, y, z])) {
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
                                        blocks_ [x_, y_, z] = blocks_ [x_, y_, z] | 0x1;
                                    }
                                }
                                maxX--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize + (blockSize * maxX), y * blockSize + (blockSize * maxY), z * blockSize));
                                vertices.Add (new Vector3( x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize ));
                                vertices.Add (new Vector3( x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize ));

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3( x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize ));
                                vertices.Add (new Vector3( x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize ));
                                vertices.Add (new Vector3( x * blockSize + (blockSize * maxX), y * blockSize - blockSize, z * blockSize ));

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks_ [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (left == 0) {
                            if ((blocks_ [x, y, z] & 0x8) == 0) {
                                var maxZ = 0;
                                var maxY = 0;

                                for (var z_ = z; z_ < _chunk.ToZ; z_++) {
                                    // Check not drawn + same color
                                    if ((blocks_ [x, y, z_] & 0x8) == 0 && SameColor (blocks_ [x, y, z_], blocks_ [x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < _chunk.ToY; y_++) {
                                        if ((blocks_ [x, y_, z_] & 0x8) == 0 && SameColor (blocks_ [x, y_, z_], blocks_ [x, y, z])) {
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
                                        blocks_ [x, y_, z_] = blocks_ [x, y_, z_] | 0x8;
                                    }
                                }
                                maxZ--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ)));

                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3(x * blockSize - blockSize, y * blockSize + (blockSize * maxY), z * blockSize - blockSize));

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);


                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks_ [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                        if (right == 0) {
                            if ((blocks_ [x, y, z] & 0x4) == 0) {
                                var maxZ = 0;
                                var maxY = 0;

                                for (var z_ = z; z_ < _chunk.ToZ; z_++) {
                                    // Check not drawn + same color
                                    if ((blocks_ [x, y, z_] & 0x4) == 0 && SameColor (blocks_ [x, y, z_], blocks_ [x, y, z])) {
                                        maxZ++;
                                    } else {
                                        break;
                                    }
                                    var tmpY = 0;
                                    for (var y_ = y; y_ < _chunk.ToY; y_++) {
                                        if ((blocks_ [x, y_, z_] & 0x4) == 0 && SameColor (blocks_ [x, y_, z_], blocks_ [x, y, z])) {
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
                                        blocks_ [x, y_, z_] = blocks_ [x, y_, z_] | 0x4;
                                    }
                                }
                                maxZ--;
                                maxY--;

                                var idx = vertices.Count;

                                vertices.Add (new Vector3(x * blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3(x * blockSize, y * blockSize - blockSize, z * blockSize + (blockSize * maxZ) ));
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                idx = vertices.Count;
                                vertices.Add (new Vector3(x * blockSize, y * blockSize + (blockSize * maxY), z * blockSize + (blockSize * maxZ)));
                                vertices.Add (new Vector3(x * blockSize, y * blockSize - blockSize, z * blockSize - blockSize ));
                                vertices.Add (new Vector3(x * blockSize, y * blockSize + (blockSize * maxY), z * blockSize - blockSize ));

                                // Add triangle indeces
                                tri.Add(idx);
                                tri.Add(idx+1);
                                tri.Add(idx+2);

                                sides += 6;
                                for (var n = 0; n < 6; n++) {
                                    colors.Add (new Color32((byte)((blocks_ [x, y, z] >> 24) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 16) & 0xFF),
                                        (byte)((blocks_ [x, y, z] >> 8) & 0xFF),
                                        (byte)255
                                    ));
                                }
                            }
                        }
                    }
                }
            }
            if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                if(vertices.Count == 0) {
                    Destroy (_chunk.gameObject); // Remove empty chunks.
                    return;
                }
                // TBD: Perhaps have a tag
                /*if (_chunk.Type == Chunk.TypeFf) {
                    _chunk.Obj.GetComponent<Rigidbody> ().mass = 0.1f * vertices.Count;
                }*/
                if (_chunk.Type == Chunk.TypeFf) _chunk.SetMass(0.1f * vertices.Count);
            }
            // Todo: I changed out this code below for a mesh update method inside the chunk & it isn't working
            _chunk.SetMesh(vertices.ToArray(), tri.ToArray(), colors.ToArray());
            
            /*_chunk.Obj.GetComponent<Renderer> ().enabled = false;
            var mesh = _chunk.Obj.GetComponent<MeshFilter>().mesh;
            mesh.Clear ();
            mesh.vertices = vertices.ToArray ();
            mesh.triangles = tri.ToArray ();
            mesh.colors32 = colors.ToArray ();
            mesh.RecalculateNormals ();
            mesh.RecalculateBounds ();
            _chunk.Obj.GetComponent<Renderer> ().enabled = true;

            _chunk.Dirty = false;

            _chunk.Obj.GetComponent<MeshCollider> ().sharedMesh = mesh;*/

            if (_chunk.Type is Chunk.TypeObj or Chunk.TypeFf) {
                _chunk.gameObject.transform.position = new Vector3(_chunk.Position.x, _chunk.Position.y, _chunk.Position.z);
            } else {
                _chunk.gameObject.transform.position = new Vector3(
                    (_chunk.FromX/chunkSize)-chunkSize/2 - (_chunk.FromX/chunkSize) + chunkSize/2,
                    (_chunk.FromY/chunkSize)-chunkSize/2 - (_chunk.FromY/chunkSize) + chunkSize/2,
                    (_chunk.FromZ/chunkSize)-chunkSize/2 - (_chunk.FromZ/chunkSize) + chunkSize/2
                );

            }

            _chunk.ToY = chunkYorg;
        }

        public bool IsWithinWorld(int _x, int _y, int _z)
        {
            return _x > 0 && _x < width*chunkSize &&
                   _y > 0 && _y <height*chunkSize &&
                   _z > 0 && _z < depth*chunkSize;
        }
    
        private bool SameColor(int _block1, int _block2)
        {
            return ((_block1 >> 8) & 0xFFFFFF) == ((_block2 >> 8) & 0xFFFFFF) && _block1 != 0 && _block2 != 0;
        }

        public void AddBlock(int _x, int _y, int _z, int _color)
        {
            if(_x < 0 || _y < 0 || _z < 0 || _x > width*chunkSize-1 || _y > height*chunkSize-1 || _z > depth*chunkSize-1) {
                return;
            }
            if(blocks[_x, _y, _z] == 0) {
                blocks [_x, _y, _z] = _color; 
            }

            var chunk = GetChunk(_x, _y, _z);
            if (chunk is not {Dirty: false}) return;
            chunk.Dirty = true;
            rebuildList.Add (chunk);
        }

        private Chunk GetChunk(int _x, int _y, int _z)
        {
            var posx = _x  / chunkSize;
            var posy = _y  / chunkSize;
            var posz = _z  / chunkSize;
            if(posx < 0 || posz < 0 || posz > depth || posx > width || posy < 0 || posy > height) {
                return null;
            }
            return chunks[posx, posy, posz];
        }
    }
}
