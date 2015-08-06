using System;
using System.Collections.Generic;
using System.Text;

namespace Xevle.FileFormats.TMX
{
	public class TilesetNotExistsException: Exception
	{
		public string Filename { get; set; }

		public TilesetNotExistsException(string filename)
		{
			Filename = filename;
		}
	}
}
