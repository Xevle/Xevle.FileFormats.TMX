using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Xevle.FileFormats.TMX
{
	public class Property
	{
		public string Name { get; private set; }

		public string Value { get; set; }

		public Property(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public Property(XmlNode node)
		{
			foreach (XmlAttribute i in node.Attributes)
			{
				switch (i.Name.ToLower())
				{
					case "name":
						{
							Name = i.Value.ToString();
							break;
						}
					case "value":
						{
							Value = i.Value.ToString();
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
					default:
						{
							break;
						}
				}
			}
		}
	}
}
