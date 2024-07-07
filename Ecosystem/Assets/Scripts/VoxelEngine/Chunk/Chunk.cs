using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelEngine
{
    public class Chunk : MonoBehaviourPlus
    {
        #region Parameters
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshCollider meshCollider;
        [SerializeField] private Rigidbody rBody;
        
        [HideInInspector] public const int TypeWorld = 0;
        [HideInInspector] public const int TypeFf = 1;
        [HideInInspector] public const int TypeObj = 2;

        [HideInInspector] public int FromX = 0;
        [HideInInspector] public int FromZ = 0;
        [HideInInspector] public int FromY = 0;
        [HideInInspector] public int ToX = 0;
        [HideInInspector] public int ToZ = 0;
        [HideInInspector] public int ToY = 0;
        [HideInInspector] public int Type = TypeWorld;
        [HideInInspector] public bool Dirty = false;
        [HideInInspector] public bool FfActive = false;
        [HideInInspector] public Vector3 Position;
        [HideInInspector] public Quaternion Rotation;
        [HideInInspector] public int TotalBlocksFf = 0;
        [HideInInspector] private int noOfCollisions = 0;

        [HideInInspector] public int[,,] Blocks;
        private Vector3Int localPosOffset = Vector3Int.zero;
        public Vector3Int LocalPosOffset => localPosOffset;
        #endregion
        
        #region Initialize
        public void InitializeAsWorld(int _x, int _y, int _z, int _fromX, int _fromY, int _fromZ, int _toX, int _toY, int _toZ)
        {
            var mats = new Material[1];
            mats [0] = new Material(Shader.Find ("Standard (Vertex Color)"));
            //		mats [1] = new Material(Shader.Find ("Toon/Lit"));
            //		mats [2] = new Material(Shader.Find ("Diffuse"));
            meshRenderer.materials = mats;
            //this.obj.GetComponent<MeshRenderer>().material.shader = Shader.Find("Standard (Vertex Color)");
            meshRenderer.material.EnableKeyword("_VERTEXCOLOR");
            
            FromX = _fromX;
            FromY = _fromY;
            FromZ = _fromZ;
            ToX = _toX;
            ToY = _toY;
            ToZ = _toZ;
            Type = TypeWorld;
            gameObject.name = $"WORLD_CHUNK ({_x}, {_y}, {_z})";
        }

        public void InitializeAsObject(int _width, int _height, int _depth)
        {
            var mats = new Material[1];
            mats [0] = new Material(Shader.Find ("Standard (Vertex Color)"));
            //		mats [1] = new Material(Shader.Find ("Toon/Lit"));
            //		mats [2] = new Material(Shader.Find ("Diffuse"));
            meshRenderer.materials = mats;
            //this.obj.GetComponent<MeshRenderer>().material.shader = Shader.Find("Standard (Vertex Color)");
            meshRenderer.material.EnableKeyword("_VERTEXCOLOR");
            FromX = -_width/2;
            FromY = -_height/2;
            FromZ = -_depth/2;
            ToX = _width/2;
            ToY = _height/2;
            ToZ = _depth/2;
            localPosOffset = new Vector3Int(_width / 2, _height / 2, _depth / 2);
            Blocks = new int[_width, _height, _depth];
            Type = TypeObj;
            Debug.LogWarning($"{gameObject.name}.Chunk.InitializeAsObject FromX {FromX} ToX {ToX}, " +
                             $"FromY {FromY} ToY {ToY}, FromZ {FromZ} ToZ {ToZ}, ");
            // gameObject.name = "OBJ_CHUNK";
        }
        #endregion
        
        #region Blocks

        public void SetBlockLocal(int _x, int _y, int _z, int _color)
        {
            try
            {
                Debug.Log($"Chunk.SetBlockLocal block ({_x + localPosOffset.x}, {_y +localPosOffset.y}, {_z + localPosOffset.z}) " +
                          $"from local position ({_x}, {_y}, {_z}), color {_color}");
                Blocks[_x + localPosOffset.x, _y +localPosOffset.y, _z + localPosOffset.z] = _color;
            }
            catch (Exception e)
            {
                Debug.LogError($"Chunk.SetBlockLocal exception {e}, " +
                               $"unable to set block ({_x + localPosOffset.x}, {_y +localPosOffset.y}, {_z + localPosOffset.z}) " +
                               $"from local position ({_x}, {_y}, {_z}), color {_color}");
            }
            
        }
        
        #endregion
        
        #region Mesh
        public void SetMesh(Vector3[] _vertices, int[] _tris, Color32[] _colors)
        {
            meshRenderer.enabled = false;
            var mesh = meshFilter.mesh;
            mesh.Clear ();
            mesh.vertices = _vertices;
            mesh.triangles = _tris;
            mesh.colors32 = _colors;
            mesh.RecalculateNormals ();
            mesh.RecalculateBounds ();
            meshRenderer.enabled = true;
            Dirty = false;
            meshCollider.sharedMesh = mesh;
        }
        #endregion
        
        #region Physics

        public void SetMass(float _value)
        {
            rBody.mass = _value;
        }

        public void EnablePhys() 
        {
            meshCollider.convex = true;
            this.Blocks = new int[ToX-FromX, ToY-FromY, ToZ-FromZ]; // +5 on all?
            this.Position.Set (FromX, FromY, FromZ);
            rBody.isKinematic = false;
        }

        public void CollisionBounce() {
            // TBD
            if (Random.value > 0.95) {
//			Vector3 pos = this.obj.GetComponent<MeshCollider> ().transform.position;
//			for (int x = 0; x < this.toX; x++) {
//				for (int y = 0; y < this.toY; y++) {
//					for (int z = 0; z < this.toZ; z++) {
//						if (this.blocks [x, y, z] != 0) {
//							if (Random.value > 0.8) {
//								// TBD: Apply rotation.
//								BlockPool.AddBlock ((int)pos.x + x, (int)pos.y + y, (int)pos.z + z, this.blocks [x, y, z], 1, 0.001f);
//							}
//						}
//					}
//				}
//			}
                Destroy (gameObject);
            }
        }

        /*public void CollisionExplode() {
            this.noOfCollisions++;
            var pos = this.Obj.GetComponent<MeshCollider> ().transform.position;
            //if (this.noOfCollisions > 10 && this.blocks.Length < 300) {
            // TBD:  foreach block in chunk
            for (var x = 0; x < this.ToX; x++) {
                for (var y = 0; y < this.ToY; y++) {
                    for (var z = 0; z < this.ToZ; z++) {
                        if (this.Blocks [x, y, z] != 0) {
                            if (Random.value > 0.5) {
                                // TBD: Apply rotation.
                                BlockPool.AddBlock ((int)pos.x + x, (int)pos.y + y, (int)pos.z + z, this.Blocks[x,y,z], 1, 0.001f);
                            }
                        }
                    }
                }
            }
            GameObject.Destroy (this.Obj);
            //} else if(this.noOfCollisions > 10 ){
            //			// Remove random blocks
            //			int noOfBlocks = (int)Random.Range(1,this.blocks.Length/2);
            //			for(int i = 0; i < noOfBlocks; i++) {
            //				int x = (int)Random.Range (0, toX);
            //				int y = (int)Random.Range (0, toY);
            //				int z = (int)Random.Range (0, toZ);
            //				if (this.blocks [x, y, z] != 0) {
            //					this.blocks [x, y, z] = 0;
            //					BlockPool.AddBlock ((int)pos.x + x, (int)pos.y + y, (int)pos.z + z, this.blocks[x,y,z], 1, 0.001f);
            //				}
            //			}
            //			position = pos;
            //			World.RebuildChunks (this);
            //		}
            //}
        }*/
        
        #endregion
    }
}
