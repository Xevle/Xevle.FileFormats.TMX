using System;
using System.Collections.Generic;

namespace Xevle.FileFormats.TMX
{
	public class Tile
	{
		public Tile()
		{
			Properties = new List<Property>();
		}

		public string ID;
		public List<Property> Properties;
	}
}

