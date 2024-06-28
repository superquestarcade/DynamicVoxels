namespace VoxelEngine
{
	public static class WorldColors
	{
		public static int[] wallColors = new int[] 
		{
        (165 & 0xFF) << 24 | (123 & 0xFF) << 16 | (82 & 0xFF) << 8,
            (193 & 0xFF) << 24 | (189 & 0xFF) << 16 | (172 & 0xFF) << 8,
            (177 & 0xFF) << 24 | (82 & 0xFF) << 16 | (82 & 0xFF) << 8
		};
		public static int[] roofColors = new int[] 
		{
        (205 & 0xFF) << 24 | (96 & 0xFF) << 16 | (96 & 0xFF) << 8,
            (107 & 0xFF) << 24 | (107 & 0xFF) << 16 | (107 & 0xFF) << 8,
            (177 & 0xFF) << 24 | (173 & 0xFF) << 16 | (156 & 0xFF) << 8,
            (220 & 0xFF) << 24 | (142 & 0xFF) << 16 | (59 & 0xFF) << 8
		};

		public static int[] flowerColors = new int[] 
		{
        (212 & 0xFF) << 24 | (37 & 0xFF) << 16 | (169 & 0xFF) << 8,
            (237 & 0xFF) << 24 | (92 & 0xFF) << 16 | (63 & 0xFF) << 8,
            (2 & 0xFF) << 24 | (173 & 0xFF) << 16 | (210 & 0xFF) << 8,
            (2 & 0xFF) << 24 | (173 & 0xFF) << 16 | (149 & 0xFF) << 8,
            (210 & 0xFF) << 24 | (0 & 0xFF) << 16 | (128 & 0xFF) << 8,
            (225 & 0xFF) << 24 | (197 & 0xFF) << 16 | (67 & 0xFF) << 8
		};

		public static int streetLightColor = (131 & 0xFF) << 24 | (131 & 0xFF) << 16 | (131 & 0xFF) << 8;
		public static int sideWalkColor = (214 & 0xFF) << 24 | (209 & 0xFF) << 16 | (190 & 0xFF) << 8;
		public static int roadColor = (104 & 0xFF) << 24 | (104 & 0xFF) << 16 | (104 & 0xFF) << 8;
		public static int drainColor = (40 & 0xFF) << 24 | (40 & 0xFF) << 16 | (40 & 0xFF) << 8;
		public static int dirtColor1 = (128 & 0xFF) << 24 | (95 & 0xFF) << 16 | (62 & 0xFF) << 8;
		public static int dirtColor2 = (162 & 0xFF) << 24 | (134 & 0xFF) << 16 | (24 & 0xFF) << 8;
		public static int grassColor1 = (137 & 0xFF) << 24 | (179 & 0xFF) << 16 | (74 & 0xFF) << 8;
		public static int grassColor2 = (120 & 0xFF) << 24 | (159 & 0xFF) << 16 | (65 & 0xFF) << 8;
		public static int fruitColor = (225 & 0xFF) << 24 | (71 & 0xFF) << 16 | (71 & 0xFF) << 8;
		public static int treeColor = (137 & 0xFF) << 24 | (180 & 0xFF) << 16 | (75 & 0xFF) << 8;
		public static int stemColor = (143 & 0xFF) << 24 | (106 & 0xFF) << 16 | (70 & 0xFF) << 8;
		public static int doorColor = (153 & 0xFF) << 24 | (106 & 0xFF) << 16 | (70 & 0xFF) << 8;
		public static int flowerStemColor = (120 & 0xFF) << 24 | (190 & 0xFF) << 16 | (65 & 0xFF) << 8;
	}
}