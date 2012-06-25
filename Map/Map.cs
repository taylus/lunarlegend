using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Property = System.Collections.Generic.KeyValuePair<string, string>;

//TODO: put all of this stuff into a project with its own unit tests
// - Test that tile GIDs are read correctly in every format
// - Test that tiles are cropped from tilesets correctly (check crop rect)

//TODO: features to add at a later time
// - External tilesets (.tsx files, see maps/desert)
// - Support for tilesets with tile dimensions different from the map's (why would you do this?)
// - Tileset spacing and margins around each tile
// - Per-layer properties (why?)
// - Per-tileset properties (why?)
// - Per-tile properties (maybe for walls? tile.properties("solid") = true)
//   - These are set in the tileset's tile... do they apply to ALL instances of that tile on the map?

/// <summary>
/// Represents a parsed Tiled map.
/// This class is strongly tied to the map.xsd generated classes, and will need to be updated whenever they are.
/// </summary>
public class Map
{
    //tmx file path/name info
    private string tmxDirName;
    private string tmxFileName;
    private string tmxPathName { get { return Path.Combine(tmxDirName, tmxFileName); } }
    public string Name { get { return tmxFileName; } }

    //deserialized tmx file contents
    private Tiled.map map;

    //GraphicsDevice for loading tilemap images
    private GraphicsDevice graphicsDevice;

    //standard map properties read from the tmx
    public int Width { get; protected set; }
    public int Height { get; protected set; }
    public int TileWidth { get; protected set; }
    public int TileHeight { get; protected set; }
    public int WidthPx { get { return Width * TileWidth; } }
    public int HeightPx { get { return Height * TileHeight; } }
    public string Version { get; protected set; }

    //map elements read from the tmx
    public List<Property> Properties { get; protected set; }
    public List<TileSet> TileSets { get; protected set; }
    public List<Layer> Layers { get; protected set; }
    public List<ObjectGroup> ObjectGroups { get; protected set; }

    public Map(string tmxFile, GraphicsDevice gd)
    {
        try
        {
            map = DeserializeTMX(tmxFile);
            ValidateMapAttributes();

            tmxDirName = Path.GetDirectoryName(tmxFile);
            tmxFileName = Path.GetFileName(tmxFile);
            graphicsDevice = gd;

            Width = int.Parse(map.width);
            Height = int.Parse(map.height);
            TileWidth = int.Parse(map.tilewidth);
            TileHeight = int.Parse(map.tileheight);
            Version = map.version;

            LoadMapElements();
        }
        catch (Exception e)
        {
            throw new Exception("Error loading map.", e);
        }
    }

    private void ValidateMapAttributes()
    {
        if (map.orientation != Tiled.orientationT.orthogonal) throw new Exception("This map loader only supports orthogonal orientation.");
        if (string.IsNullOrWhiteSpace(map.width)) throw new Exception("Map width is not specified.");
        if (string.IsNullOrWhiteSpace(map.height)) throw new Exception("Map height is not specified.");
        if (string.IsNullOrWhiteSpace(map.tilewidth)) throw new Exception("Map tile width is not specified.");
        if (string.IsNullOrWhiteSpace(map.tileheight)) throw new Exception("Map tile height is not specified.");
    }

    private void LoadMapElements()
    {
        Properties = LoadProperties(map.properties);
        TileSets = (from Tiled.tileset ts in map.tileset select new TileSet(tmxDirName, ts, graphicsDevice)).ToList();
        Layers = (from object o in map.Items where o.GetType() == typeof(Tiled.layer) select new Layer((Tiled.layer)o)).ToList();
        ObjectGroups = (from object o in map.Items where o.GetType() == typeof(Tiled.objectgroup) select new ObjectGroup((Tiled.objectgroup)o)).ToList();
    }

    public static List<Property> LoadProperties(Tiled.properties properties)
    {
        if (properties == null) return new List<Property>();
        return (from Tiled.property p in properties.property select new Property(p.name, p.value)).ToList();
    }

    private Tiled.map DeserializeTMX(string tmxFile)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(tmxFile);

        //TODO: validate against xsd

        //HACK: Tiled does not include the xmlns in .tmx files... set it here (must match namespace from map.xsd!)
        doc.DocumentElement.SetAttribute("xmlns", "http://mapeditor.org");

        Tiled.map map;
        XmlSerializer serializer = new XmlSerializer(typeof(Tiled.map));
        using (StringReader reader = new StringReader(doc.OuterXml))
        {
            map = (Tiled.map)serializer.Deserialize(reader);
        }

        return map;
    }

    public void Draw(SpriteBatch sb, bool debugDrawObjects = false)
    {
        foreach (Layer layer in Layers)
        {
            //don't bother with invisible layers
            if (layer.Opacity <= 0) continue;

            //layer opacity (white means no color tinting in XNA)
            Color layerColor = Color.Lerp(Color.Transparent, Color.White, MathHelper.Clamp(layer.Opacity, 0, 1));

            //draw each tile in the layer
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Tile tile = layer.Tiles[x, y];
                    if (tile.GID == 0) continue;

                    //get the tileset that owns this tile, and the source rect within it
                    TileSet tileset;
                    Rectangle tileSrcRect;
                    TileSets.ResolveTileGID(tile.GID, out tileset, out tileSrcRect);
                    Rectangle tileDestRect = new Rectangle(x * TileWidth, y * TileHeight, TileWidth, TileHeight);
  
                    //flip and rotation settings
                    SpriteEffects flip = SpriteEffects.None;
                    float rotation = 0.0f;
                    if (!tile.FlippedDiagonally)
                    {
                        if (tile.FlippedHorizontally) flip |= SpriteEffects.FlipHorizontally;
                        if (tile.FlippedVertically) flip |= SpriteEffects.FlipVertically;
                    }
                    else
                    {
                        if (tile.FlippedHorizontally) rotation = MathHelper.PiOver2;
                        else rotation = -MathHelper.PiOver2;
                    }

                    if (!tile.FlippedDiagonally)
                    {
                        //no rotation, but possibly horizontally/vertically flipped
                        sb.Draw(tileset.Texture, tileDestRect, tileSrcRect, layerColor, 0.0f, Vector2.Zero, flip, 0);
                    }
                    else
                    {
                        //if tile is rotated, need to draw at an adjusted dest rect due to the way XNA draws things with offsets
                        Rectangle adjustedDestRect = new Rectangle(tileDestRect.X + tileDestRect.Width / 2, tileDestRect.Y + tileDestRect.Height / 2, TileWidth, TileHeight);
                        sb.Draw(tileset.Texture, adjustedDestRect, tileSrcRect, layerColor, rotation, tileDestRect.Center.ToVector2(), flip, 0);
                    }
                }
            }
        }

        if (debugDrawObjects)
        {
            foreach (ObjectGroup objGroup in ObjectGroups)
            {
                foreach (Object obj in objGroup.Objects)
                {
                    Util.DrawRectangle(sb, obj.Rectangle, Object.DEFAULT_COLOR);
                }
            }
        }
    }
}