using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.IO.Compression;
using Xevle.Imaging.Image;
using Xevle.IO;
using Xevle.Core.Exceptions;
using Xevle.Data.Encoding;
using Xevle.Data.Compression;
using Xevle.Core;

namespace Xevle.FileFormats.TMX
{
	public class TMX
	{
		static PooledLoader pooledLoader = new PooledLoader(100);

		XmlDocument FileData;

		public string MapVersion { get; private set; }

		public string Orientation { get; private set; }

		public int Width { get; private set; }

		public int Height { get; private set; }

		public int TileWidth { get; private set; }

		public int TileHeight { get; private set; }

		public List<TilesetData> Tilesets { get; private set; }

		public List<LayerData> Layers { get; private set; }

		public List<Objectgroup> ObjectLayers { get; private set; }

		public List<Property> Properties { get; private set; }

		public Property GetProperty(string name)
		{
			foreach (Property i in Properties)
			{
				if (i.Name == name)
				{
					return i;
				}
			}

			return null;
		}

		public void RemoveGidsFromLayerData()
		{
			foreach (LayerData ld in Layers)
			{
				ld.tilesetmap = new TilesetData[ld.width, ld.height];

				for (int y = 0; y < ld.height; y++)
				{
					for (int x = 0; x < ld.width; x++)
					{
						int TileNumber = ld.data[x, y];

						TilesetData ts = GetTileset(TileNumber);
						ld.tilesetmap[x, y] = ts;

						int tilesetNumber = TileNumber - ts.firstgid;
						ld.data[x, y] = tilesetNumber;
					}
				}
			}
		}

		public void ReplaceTilesetInTilesetMap(TilesetData oldTileset, TilesetData newTileset)
		{
			foreach (LayerData ld in Layers)
			{
				for (int y = 0; y < ld.height; y++)
				{
					for (int x = 0; x < ld.width; x++)
					{
						TilesetData ts = ld.tilesetmap[x, y];

						if (ts == oldTileset)
						{
							ld.tilesetmap[x, y] = newTileset;
						}
					}
				}
			}
		}

		public void AddsGidsToLayerData()
		{
			foreach (LayerData ld in Layers)
			{
				for (int y = 0; y < ld.height; y++)
				{
					for (int x = 0; x < ld.width; x++)
					{
						int TileNumber = ld.data[x, y];

						TilesetData ts = ld.tilesetmap[x, y];
	
						int tilesetNumber = TileNumber + ts.firstgid;
						ld.data[x, y] = tilesetNumber;
					}
				}
			}
		}

		public TMX(string filename)
		{
			Open(filename, true);
		}

		public TMX(string filename, bool loadTilesets)
		{
			Open(filename, loadTilesets);
		}

		void Open(string filename, bool loadTilesets)
		{
			Tilesets = new List<TilesetData>();
			Layers = new List<LayerData>();
			ObjectLayers = new List<Objectgroup>();
			Properties = new List<Property>();

			FileData = new XmlDocument();
			FileData.Load(filename);

			#region Get map info
			XmlNodeList xnl = FileData.SelectNodes("/map");

			MapVersion = xnl[0].Attributes["version"].Value;
			Orientation = xnl[0].Attributes["orientation"].Value;

			Width = Convert.ToInt32(xnl[0].Attributes["width"].Value);
			Height = Convert.ToInt32(xnl[0].Attributes["height"].Value);

			TileWidth = Convert.ToInt32(xnl[0].Attributes["tilewidth"].Value);
			TileHeight = Convert.ToInt32(xnl[0].Attributes["tileheight"].Value);
			#endregion

			#region Read properties
			xnl = FileData.SelectNodes("/map/properties");

			foreach (XmlNode j in xnl)
			{
				XmlNodeList subnodes = j.SelectNodes("child::property");

				foreach (XmlNode pNode in subnodes)
				{
					string name = pNode.Attributes[0].Name;
					string value = pNode.Attributes[0].Value;

					Properties.Add(new Property(pNode));
				}
			}
			#endregion

			#region Search tilesets
			xnl = FileData.SelectNodes("/map/tileset");

			foreach (XmlNode j in xnl)
			{
				// Tilesets
				TilesetData ts = new TilesetData();

				ts.imgsource = j.SelectNodes("child::image")[0].Attributes[0].Value; //Image Source für den Layer
				string imgsourceComplete = Paths.GetPath(filename) + ts.imgsource;

				// Load Tileset
				XmlNodeList nodelist = j.SelectNodes("child::tile");

				foreach (XmlNode tileXml in nodelist)
				{
					Tile tile = new Tile();
					tile.ID = tileXml.Attributes["id"].Value.ToString();

					xnl = tileXml.SelectNodes("child::properties");

					foreach (XmlNode jProp in xnl)
					{
						XmlNodeList subnodes = jProp.SelectNodes("child::property");

						foreach (XmlNode pNode in subnodes)
						{
							tile.Properties.Add(new Property(pNode));
						}
					}

					ts.Tiles.Add(tile);
				}

				// Load tiles
				if (loadTilesets)
				{
					try
					{
						ts.img = (Image8i)pooledLoader.FromFile(imgsourceComplete);
					}
					catch (FileNotFoundException ex)
					{
						throw new TilesetNotExistsException(ex.Message);
					}
				}

				// Attributes
				ts.name = j.Attributes["name"].Value; 
				ts.firstgid = Convert.ToInt32(j.Attributes["firstgid"].Value);
				ts.tilewidth = Convert.ToInt32(j.Attributes["tilewidth"].Value);
				ts.tileheight = Convert.ToInt32(j.Attributes["tileheight"].Value);

				Tilesets.Add(ts);
			}
			#endregion

			#region Layers ermitteln
			xnl = FileData.SelectNodes("/map/layer");

			foreach (XmlNode j in xnl) //pro layer
			{
				// Layers
				LayerData lr = new LayerData();

				// Attributes
				lr.name = j.Attributes["name"].Value;
				lr.width = Convert.ToInt32(j.Attributes["width"].Value);
				lr.height = Convert.ToInt32(j.Attributes["height"].Value);

				// Layer data
				// Attributes should be "<data encoding="base64" compression="gzip">"
				string encoding = j["data"].Attributes["encoding"].Value;

				string compression = "uncompressed";

				if (j["data"].Attributes["compression"] != null)
				{
					compression = j["data"].Attributes["compression"].Value;
				}

				if (encoding != "base64")
				{
					throw (new NotImplementedException("Weitere Codierungsarten sind noch nicht implementiert!"));
				}

				if (compression != "uncompressed" && compression != "gzip")
				{
					throw (new NotSupportedCompressionException("Weitere Kompressionsverfahren sind noch nicht implementiert!"));
				}

				// Base64 encoding
				string layerdataBase64Compressed = j.SelectNodes("child::data")[0].InnerText;
				layerdataBase64Compressed = layerdataBase64Compressed.TrimStart('\n');
				layerdataBase64Compressed = layerdataBase64Compressed.Trim();
				byte[] layerdataCompressed = Base64.Decode(layerdataBase64Compressed); 

				// gzip decode, if nessecary
				byte[] layerdataDecompressed;
				if (compression == "uncompressed")
				{
					layerdataDecompressed = layerdataCompressed;
				}
				else
				{
					layerdataDecompressed = gzip.Decompress(layerdataCompressed);
				}

				// Interpret coded data
				lr.data = new int[lr.width, lr.height];

				BinaryReader br = new BinaryReader(new MemoryStream(layerdataDecompressed));

				for (int y = 0; y < lr.height; y++)
				{
					for (int x = 0; x < lr.width; x++)
					{
						lr.data[x, y] = br.ReadInt32();
					}
				}
				
				Layers.Add(lr);
			}
			#endregion

			#region Objektlayer ermitteln
			xnl = FileData.SelectNodes("/map/objectgroup");

			foreach (XmlNode j in xnl) //pro layer
			{
				ObjectLayers.Add(new Objectgroup(j));
			}
			#endregion
		}

		public TilesetData GetTileset(int number)
		{
			TilesetData ret = new TilesetData();

			foreach (TilesetData i in Tilesets)
			{
				if (number >= i.firstgid)
				{
					ret = i;
				}
			}

			return ret;
		}

		public Image8i GetTile(int number)
		{
			TilesetData ts = GetTileset(number);
			int tilesetNumber = number - ts.firstgid;

			int tilesPerLine = (int)(ts.img.Width / TileWidth);

			int tsPosX = tilesetNumber % tilesPerLine;
			int tsPosY = tilesetNumber / tilesPerLine;

			int tilesetPixelStartX = tsPosX * ts.tilewidth;
			int tilesetPixelStartY = tsPosY * ts.tileheight;

			try
			{
				return ts.img.GetSubimage((uint)tilesetPixelStartX, (uint)tilesetPixelStartY, (uint)ts.tilewidth, (uint)ts.tileheight);
			}
			catch
			{
				return new Image8i((uint)ts.tilewidth, (uint)ts.tileheight, ChannelFormat.RGBA);
			}
		}

		public void Save(string filename, bool compressed = true)
		{
			XmlDocument fileData = new XmlDocument();
			
            #region Root speichern und Attribute anhängen
            XmlNode root=fileData.AddRoot("map");

            fileData.AddAttribute(root, "version", MapVersion);
            fileData.AddAttribute(root, "orientation", Orientation);

            fileData.AddAttribute(root, "width", Width);
            fileData.AddAttribute(root, "height", Height);

            fileData.AddAttribute(root, "tilewidth", TileWidth);
            fileData.AddAttribute(root, "tileheight", TileHeight);
            #endregion

            #region Properties speichern
            if(Properties.Count>0)
            {
                XmlNode properties=fileData.AddElement(root, "properties");

                foreach(Property prop in Properties)
                {
                    XmlNode propertyXml=fileData.AddElement(properties, "property");
                    fileData.AddAttribute(propertyXml, "name", prop.Name);
                    fileData.AddAttribute(propertyXml, "value", prop.Value);
                }
            }
            #endregion

            #region Tilesets
            foreach(TilesetData tileset in Tilesets)
            {
                XmlNode tilesetXml=fileData.AddElement(root, "tileset");
                fileData.AddAttribute(tilesetXml, "firstgid", tileset.firstgid);
                fileData.AddAttribute(tilesetXml, "name", tileset.name);
                fileData.AddAttribute(tilesetXml, "tilewidth", tileset.tilewidth);
                fileData.AddAttribute(tilesetXml, "tileheight", tileset.tileheight);

                XmlNode imageTag=fileData.AddElement(tilesetXml, "image");
                fileData.AddAttribute(imageTag, "source", tileset.imgsource);

                foreach(Tile tile in tileset.Tiles)
                {
                    XmlNode tileTag=fileData.AddElement(tilesetXml, "tile");
                    fileData.AddAttribute(tileTag, "id", tile.ID);

                    if(tile.Properties.Count>0)
                    {
                        XmlNode properties=fileData.AddElement(tileTag, "properties");

                        foreach(Property prop in tile.Properties)
                        {
                            XmlNode propertyXml=fileData.AddElement(properties, "property");
                            fileData.AddAttribute(propertyXml, "name", prop.Name);
                            fileData.AddAttribute(propertyXml, "value", prop.Value);
                        }
                    }
                }

            }
            #endregion

            #region Layer
            foreach(LayerData layer in Layers)
            {
                XmlNode layerXml=fileData.AddElement(root, "layer");
                fileData.AddAttribute(layerXml, "name", layer.name);
                fileData.AddAttribute(layerXml, "width", layer.width);
                fileData.AddAttribute(layerXml, "height", layer.height);

                XmlNode dataTag=fileData.AddElement(layerXml, "data", ConvertLayerDataToString(layer, compressed));
                fileData.AddAttribute(dataTag, "encoding", "base64");

                if(compressed)
                {
                    fileData.AddAttribute(dataTag, "compression", "gzip");
                }
            }
            #endregion

            #region Objectlayer
            foreach(Objectgroup objGroup in ObjectLayers)
            {
                XmlNode objGroupXml=fileData.AddElement(root, "objectgroup");
                fileData.AddAttribute(objGroupXml, "name", objGroup.Name);
                fileData.AddAttribute(objGroupXml, "width", objGroup.Width);
                fileData.AddAttribute(objGroupXml, "height", objGroup.Height);
                fileData.AddAttribute(objGroupXml, "x", objGroup.X);
                fileData.AddAttribute(objGroupXml, "y", objGroup.Y);

                foreach(Object obj in objGroup.Objects)
                {
                    XmlNode objXml=fileData.AddElement(objGroupXml, "object");
                    fileData.AddAttribute(objXml, "name", obj.Name);
                    fileData.AddAttribute(objXml, "type", obj.Type);
                    fileData.AddAttribute(objXml, "x", obj.X);
                    fileData.AddAttribute(objXml, "y", obj.Y);
                    fileData.AddAttribute(objXml, "width", obj.Width);
                    fileData.AddAttribute(objXml, "height", obj.Height);

                    XmlNode objPropertiesXml=fileData.AddElement(objXml, "properties");

                    foreach(Property objProp in obj.Properties)
                    {
                        XmlNode propertyXml=fileData.AddElement(objPropertiesXml, "property");
                        fileData.AddAttribute(propertyXml, "name", objProp.Name);
                        fileData.AddAttribute(propertyXml, "value", objProp.Value);
                    }
                }
            }
            #endregion

            fileData.Save(filename);
		}

		private string ConvertLayerDataToString(LayerData layer, bool compressed)
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);

			for (int y = 0; y < layer.height; y++)
			{
				for (int x = 0; x < layer.width; x++)
				{
					bw.Write(layer.data[x, y]);
				}
			}

			// gzip decoding
			byte[] layerdataCompressed;

			if (compressed)
			{
				layerdataCompressed = gzip.Compress(ms.ToArray());
			}
			else
			{
				layerdataCompressed = ms.ToArray();
			}

			//Base64 Encodierung
			string layerdataEncoded = Base64.Encode(layerdataCompressed);
			return layerdataEncoded;
		}

		public Image8i Render()
		{
			return Render(Width * TileWidth, Height * TileHeight);
		}

		public Image8i Render(string onlyLayer)
		{
			return Render(Width * TileWidth, Height * TileHeight, onlyLayer);
		}

		private Image8i Render(int width, int height)
		{
			return Render(width, height, "");
		}

		private Image8i Render(int width, int height, string onlyLayer)
		{
			Image8i ret = new Image8i((uint)width, (uint)height, ChannelFormat.RGBA);
			ret = ret.ToAlphaInvertedImage();

			foreach (LayerData i in Layers)
			{
				if (onlyLayer == "")
				{
					if (i.name == "Collision") continue;
				}
				else
				{
					if (i.name != onlyLayer) continue;
				}

				for (int y = 0; y < i.height; y++)
				{
					for (int x = 0; x < i.width; x++)
					{
						int number = i.data[x, y];
						if (number <= 0) continue; 
						Image8i Tile = GetTile(number);

						int CorFactorX = 0;
						int CorFactorY = (int)(Tile.Height - TileHeight);

						ret.Draw(x * TileWidth - CorFactorX, y * TileHeight - CorFactorY, Tile, true);
					}
				}
			
			}

			return ret;
		}
	}
}
