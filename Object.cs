using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Xevle.FileFormats.TMX
{
	public class Object
	{
		public List<Property> Properties { get; private set; }

		public string Name { get; private set; }

		public string Type { get; private set; }

		public int X { get; set; }

		public int Y { get; set; }

		public int Width { get; set; }

		public int Height { get; set; }

		public Object(string name, string type, int width, int height, int x, int y)
		{
			Properties = new List<Property>();

			Name = name;
			Type = type;
			Width = width;
			Height = height;
			X = x;
			Y = y;
		}

		public Object(XmlNode node)
		{
			Properties = new List<Property>();

			foreach (XmlAttribute i in node.Attributes)
			{
				switch (i.Name.ToLower())
				{
					case "name":
						{
							Name = i.Value.ToString();
							break;
						}
					case "type":
						{
							Type = i.Value.ToString();
							break;
						}
					case "x":
						{
							X = Convert.ToInt32(i.Value);
							break;
						}
					case "y":
						{
							Y = Convert.ToInt32(i.Value);
							break;
						}
					case "width":
						{
							Width = Convert.ToInt32(i.Value);
							break;
						}
					case "height":
						{
							Height = Convert.ToInt32(i.Value);
							break;
						}
					default:
						{
							break;
						}
				}
			}

			foreach (XmlNode i in node.ChildNodes)
			{
				switch (i.Name.ToLower())
				{
					case "properties":
						{
							foreach (XmlNode j in i.ChildNodes)
							{
								Properties.Add(new Property(j));
							}
							break;
						}
					default:
						{
							break;
						}
				}
			}
		}
	}
}
