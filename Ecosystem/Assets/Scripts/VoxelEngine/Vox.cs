using System.IO;

namespace VoxelEngine
{
	public class Vox
	{
		private static int[] _voxColors = new int[] { 32767, 25599, 19455, 13311, 7167, 1023, 32543, 25375, 19231, 13087, 6943, 799, 32351, 25183, 
        19039, 12895, 6751, 607, 32159, 24991, 18847, 12703, 6559, 415, 31967, 24799, 18655, 12511, 6367, 223, 31775, 24607, 18463, 12319, 6175, 31, 
        32760, 25592, 19448, 13304, 7160, 1016, 32536, 25368, 19224, 13080, 6936, 792, 32344, 25176, 19032, 12888, 6744, 600, 32152, 24984, 18840, 
        12696, 6552, 408, 31960, 24792, 18648, 12504, 6360, 216, 31768, 24600, 18456, 12312, 6168, 24, 32754, 25586, 19442, 13298, 7154, 1010, 32530, 
        25362, 19218, 13074, 6930, 786, 32338, 25170, 19026, 12882, 6738, 594, 32146, 24978, 18834, 12690, 6546, 402, 31954, 24786, 18642, 12498, 6354, 
        210, 31762, 24594, 18450, 12306, 6162, 18, 32748, 25580, 19436, 13292, 7148, 1004, 32524, 25356, 19212, 13068, 6924, 780, 32332, 25164, 19020, 
        12876, 6732, 588, 32140, 24972, 18828, 12684, 6540, 396, 31948, 24780, 18636, 12492, 6348, 204, 31756, 24588, 18444, 12300, 6156, 12, 32742, 
        25574, 19430, 13286, 7142, 998, 32518, 25350, 19206, 13062, 6918, 774, 32326, 25158, 19014, 12870, 6726, 582, 32134, 24966, 18822, 12678, 6534, 
        390, 31942, 24774, 18630, 12486, 6342, 198, 31750, 24582, 18438, 12294, 6150, 6, 32736, 25568, 19424, 13280, 7136, 992, 32512, 25344, 19200, 
        13056, 6912, 768, 32320, 25152, 19008, 12864, 6720, 576, 32128, 24960, 18816, 12672, 6528, 384, 31936, 24768, 18624, 12480, 6336, 192, 31744, 
        24576, 18432, 12288, 6144, 28, 26, 22, 20, 16, 14, 10, 8, 4, 2, 896, 832, 704, 640, 512, 448, 320, 256, 128, 64, 28672, 26624, 22528, 20480, 
        16384, 14336, 10240, 8192, 4096, 2048, 29596, 27482, 23254, 21140, 16912, 14798, 10570, 8456, 4228, 2114, 1  };

        private struct MagicaVoxelData
        {
            public byte X;
            public byte Y;
            public byte Z;
            public byte Color;

            public MagicaVoxelData(BinaryReader _stream, bool _subsample)
            {
                X = (byte)(_subsample ? _stream.ReadByte() / 2 : _stream.ReadByte());
                Y = (byte)(_subsample ? _stream.ReadByte() / 2 : _stream.ReadByte());
                Z = (byte)(_subsample ? _stream.ReadByte() / 2 : _stream.ReadByte());
                Color = _stream.ReadByte();
            }
        }

        public static int[,,] LoadModel(string _filename)
        {
            var stream = new BinaryReader(File.Open(_filename, FileMode.Open));

            int[] colors = null;
            MagicaVoxelData[] voxelData = null;

            var magic = new string(stream.ReadChars(4));
            var version = stream.ReadInt32();
            int sizeX = 0, sizeY = 0, sizeZ = 0;

            if (magic == "VOX ") {
                var subsample = false;
                while (stream.BaseStream.Position < stream.BaseStream.Length)
                {
                    // each chunk has an ID, size and child chunks
                    var chunkId = stream.ReadChars(4);
                    var chunkSize = stream.ReadInt32();
                    var childChunks = stream.ReadInt32();
                    var chunkName = new string(chunkId);

                    // there are only 2 chunks we only care about, and they are SIZE and XYZI
                    if (chunkName == "SIZE")
                    {
                        sizeX = stream.ReadInt32();
                        sizeY = stream.ReadInt32();
                        sizeZ = stream.ReadInt32();
                        //if (sizex > 32 || sizey > 32) subsample = true;

                        stream.ReadBytes(chunkSize - 4 * 3);
                    }
                    else if (chunkName == "XYZI")
                    {
                        // XYZI contains n voxels
                        var numVoxels = stream.ReadInt32();
                        //	int div = (subsample ? 2 : 1);

                        // each voxel has x, y, z and color index values
                        voxelData = new MagicaVoxelData[numVoxels];
                        for (var i = 0; i < voxelData.Length; i++) {
                            voxelData [i] = new MagicaVoxelData (stream, false);
                        }
                    }
                    else if (chunkName == "RGBA")
                    {
                        colors = new int[256];

                        for (var i = 0; i < 256; i++)
                        {
                            var r = stream.ReadByte();
                            var g = stream.ReadByte();
                            var b = stream.ReadByte();
                            var a = stream.ReadByte();

                            // convert RGBA to our custom voxel format (16 bits, 0RRR RRGG GGGB BBBB)
                            colors[i] = (int)(((r & 0xFF) << 24) | ((g & 0xFF) << 16) | (b & 0xFF) << 8);
                        }
                    }
                    else stream.ReadBytes(chunkSize);   // read any excess bytes
                }

                if (voxelData.Length == 0) return null; // failed to read any valid voxel data

                // now push the voxel data into our voxel chunk structure

                /*if (_type == "map") { // Skip this stage, World can assign its own block data with the Chunk returned from this method
                    var scale = 3;
                    World.width = (int)(sizeX * scale) / World.chunkSize;
                    World.height = (int)(sizeZ * scale) / World.chunkSize;
                    World.depth = (int)(sizeY * scale) / World.chunkSize;
                    World.CreateChunks ();
                    var c = 0;
                    for (var i = 0; i < voxelData.Length; i++) {
                        c++;
                        var col = (colors == null ? _voxColors [voxelData [i].Color - 1] : colors [voxelData [i].Color - 1]);

                        for(var x1 =voxelData[i].X *(scale-1)+1; x1 < voxelData[i].X*(scale-1)+scale; x1++) {
                            for(var z1 = voxelData[i].Z*(scale-1)+1; z1 < voxelData[i].Z*(scale-1)+scale; z1++) {
                                for(var y1 = voxelData[i].Y*(scale-1)+1; y1 < voxelData[i].Y*(scale-1)+scale; y1++) {	
                                    World.blocks [x1, z1, y1] = col;
                                }
                            }
                        }
                    }
                    World.RebuildDirtyChunks (true);
                    return null;
                } else {*/
                var blockData = new int[sizeX, sizeZ, sizeY];
                for (var i = 0; i < voxelData.Length; i++) 
                {
                    var col = (colors == null ? _voxColors [voxelData [i].Color - 1] : colors [voxelData [i].Color - 1]);
                    blockData [voxelData [i].X, voxelData [i].Z, voxelData [i].Y] = col;
                }
                stream.Close ();
                return blockData;
                // }
            }
            return null;
        }
	}
}