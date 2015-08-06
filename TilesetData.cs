using System;
using System.Collections.Generic;
using Xevle.Imaging.Image;

namespace Xevle.FileFormats.TMX
{
	public class TilesetData: IComparable
	{
		public TilesetData()
		{
			Tiles = new List<Tile>();
		}

		public string name;
		public int firstgid;
		public int tilewidth;
		public int tileheight;

		public string imgsource;
		public Image8i img;

		public List<Tile> Tiles;

		#region IComparable Members
		public int CompareTo(object obj)
		{
			TilesetData tmp = (TilesetData)obj;
			if (tmp.firstgid < this.firstgid)
			{
				return 1;
			}
			else if (tmp.firstgid == this.firstgid)
			{
				return 0;
			}
			else
			{
				return -1;
			}
		}
		#endregion
	}
}

