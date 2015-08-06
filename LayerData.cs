using System;
using System.IO;

namespace Xevle.FileFormats.TMX
{
	public class LayerData
	{
		public TilesetData[,] tilesetmap;
		public string name;
		public int width;
		public int height;
		public int[,] data;

		public void SaveLayer(string filename)
		{
			StreamWriter sw = new StreamWriter(filename);

			for (int y = 0; y < height; y++)
			{
				string line = "";

				for (int x = 0; x < width; x++)
				{
					line += data[x, y].ToString() + "\t";
				}

				sw.WriteLine(line);
			}

			sw.Close();
		}
	}
}

