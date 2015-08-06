using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Xevle.FileFormats.TMX
{
	public class Objectgroup
	{
		public List<Object> Objects { get; set; }

		public string Name { get; private set; }

		public int Width { get; private set; }

		public int Height { get; private set; }

		public int X { get; private set; }

		public int Y { get; private set; }

		public int Visible { get; private set; }

		public Objectgroup(string name, int width, int height, int x, int y)
		{
			Name = name;
			Width = width;
			Height = height;
			X = x;
			Y = y;
		}

		public Objectgroup(XmlNode node)
		{
			Objects = new List<Object>();

			foreach (XmlAttribute i in node.Attributes)
			{
				switch (i.Name.ToLower())
				{
					case "name":
						{
							Name = i.Value.ToString();
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
					case "visible":
						{
							Visible = Convert.ToInt32(i.Value);
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
					case "object":
						{
							Objects.Add(new Object(i));
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
