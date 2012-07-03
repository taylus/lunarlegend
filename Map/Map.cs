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

    //the single layer this map uses for collision
    public Layer CollisionLayer { get; protected set; }

    //debug: highlight these tiles when drawing
    public List<Point> HighlightedTiles { get; set; }
    private SpriteFont font;
    private static readonly Color tileCoordColor = Color.Lerp(Color.Transparent, Color.White, 0.25f);

    public Map(string tmxFile, GraphicsDevice gd, SpriteFont font, string collisionLayerName = null)
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
            HighlightedTiles = new List<Point>();
            this.font = font;

            if (!string.IsNullOrWhiteSpace(collisionLayerName))
            {
                SetCollisionLayer(collisionLayerName);
            }
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
        Draw(sb, graphicsDevice.Viewport.Bounds, debugDrawObjects);
    }

    public void Draw(SpriteBatch sb, Rectangle viewWindowPx, bool debug = false)
    {
        foreach (Layer layer in Layers)
        {
            //don't bother with invisible layers
            if (layer.Opacity <= 0) continue;

            //only draw the collision layer in debug mode
            if (layer == CollisionLayer && !debug) continue;

            //layer opacity (white means no color tinting in XNA)
            Color layerColor = Color.Lerp(Color.Transparent, Color.White, MathHelper.Clamp(layer.Opacity, 0, 1));

            //convert view position and dimensions from pixel to tile units
            Rectangle viewWindowTiles = new Rectangle();
            viewWindowTiles.X = viewWindowPx.X / TileWidth;
            viewWindowTiles.Y = viewWindowPx.Y / TileHeight;
            viewWindowTiles.Width = (int)Math.Ceiling(viewWindowPx.Width / (double)TileWidth);
            viewWindowTiles.Height = (int)Math.Ceiling(viewWindowPx.Height / (double)TileWidth);

            //possible partial tile offset
            int viewTileOffsetX = viewWindowPx.X % TileWidth;
            int viewTileOffsetY = viewWindowPx.Y % TileHeight;
            if (viewTileOffsetX > 0) viewWindowTiles.Width += 1;
            if (viewTileOffsetY > 0) viewWindowTiles.Height += 1;

            //draw each tile in the layer
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Rectangle tileDestRect = new Rectangle((x - viewWindowTiles.X) * TileWidth - viewTileOffsetX,
                                                           (y - viewWindowTiles.Y) * TileHeight - viewTileOffsetY,
                                                           TileWidth, TileHeight);

                    //don't draw tiles we won't see
                    if (!viewWindowTiles.Contains(x, y)) continue;

                    Tile tile = layer.Tiles[x, y];
                    if (tile.GID == 0) continue;

                    //get the tileset that owns this tile, and the source rect within it
                    TileSet tileset;
                    Rectangle tileSrcRect;
                    TileSets.ResolveTileGID(tile.GID, out tileset, out tileSrcRect);

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

                    Color tileColor = debug && HighlightedTiles.Contains(new Point(x, y)) ? new Color(255, 255, 0, 128) : layerColor;

                    if (!tile.FlippedDiagonally)
                    {
                        //no rotation, but possibly horizontally/vertically flipped
                        sb.Draw(tileset.Texture, tileDestRect, tileSrcRect, tileColor, 0.0f, Vector2.Zero, flip, 0);
                        if (debug)
                        {
                            string coords = string.Format("{0},{1}", x, y);
                            Vector2 msgOrigin = font.MeasureString(coords) / 2;
                            sb.DrawString(font, coords, new Vector2((int)(tileDestRect.Center.X - msgOrigin.X), (int)(tileDestRect.Center.Y - msgOrigin.Y)), tileCoordColor);
                        }
                    }
                    else
                    {
                        //if tile is rotated, need to offset dest rect due to the way XNA draws things centered when rotated
                        Rectangle adjustedDestRect = new Rectangle(tileDestRect.X + tileDestRect.Width / 2, tileDestRect.Y + tileDestRect.Height / 2, TileWidth, TileHeight);
                        sb.Draw(tileset.Texture, adjustedDestRect, tileSrcRect, tileColor, rotation, new Vector2(TileWidth / 2, TileHeight / 2), flip, 0);
                        if (debug)
                        {
                            string coords = string.Format("{0},{1}", x, y);
                            Vector2 msgOrigin = font.MeasureString(coords) / 2;
                            sb.DrawString(font, coords, new Vector2((int)(adjustedDestRect.Center.X - msgOrigin.X), (int)(adjustedDestRect.Center.Y - msgOrigin.Y)), tileCoordColor);
                        }
                    }
                }
            }
        }

        if (debug)
        {
            DrawGridlines(sb, viewWindowPx);

            foreach (ObjectGroup objGroup in ObjectGroups)
            {
                foreach (Object obj in objGroup.Objects)
                {
                    Rectangle objRect = new Rectangle((int)(obj.Position.X - viewWindowPx.X), 
                                                      (int)(obj.Position.Y - viewWindowPx.Y), 
                                                      obj.Width, obj.Height);
                    Util.DrawRectangle(sb, objRect, Object.DEFAULT_COLOR);
                }
            }
        }
    }

    private void DrawGridlines(SpriteBatch sb, Rectangle viewWindowPx)
    {
        for (int x = 0; x < HeightPx; x += TileWidth)
        {
            Util.DrawLine(sb, 1.0f, new Vector2(x - viewWindowPx.X, 0), new Vector2(x - viewWindowPx.X, viewWindowPx.Height), Color.Black);
        }

        for (int y = 0; y < HeightPx; y += TileHeight)
        {
            Util.DrawLine(sb, 1.0f, new Vector2(0, y - viewWindowPx.Y), new Vector2(viewWindowPx.Width, y - viewWindowPx.Y), Color.Black);
        }
    }

    public void SetCollisionLayer(string name)
    {
        Layer layer = Layers.GetByName(name);
        if (layer != null) 
            CollisionLayer = layer;
        else 
            throw new Exception("Error setting collision layer: layer \"" + name + "\" not found."); 
    }

    //gets the first (any will do) tile from the collision layer
    public Tile GetWallTile()
    {
        if (CollisionLayer == null) return new Tile();
        return (from Tile t in CollisionLayer.Tiles where t.GID != 0 select t).FirstOrDefault();
    }

    //returns the first object with this name in the first objectgroup found to contain it
    //if you use the same name for multiple objects, you're gonna have a bad time
    public Object GetObject(string name)
    {
        return (from ObjectGroup objGroup in ObjectGroups where objGroup.ContainsObject(name) select objGroup.GetObject(name)).FirstOrDefault();
    }

    //get all tile coordinates that the given pixel coordinate rectangle occupies
    public List<Point> GetOccupyingTiles(Rectangle rect)
    {
        int tileUpperLeftX = rect.Left / TileWidth;
        int tileUpperLeftY = rect.Top / TileHeight;
        int tileBottomRightX = (rect.Right - 1) / TileWidth;
        int tileBottomRightY = (rect.Bottom - 1) / TileHeight;
        List<Point> occupiedTiles = new List<Point>();

        for (int x = tileUpperLeftX; x <= tileBottomRightX; x++)
        {
            for (int y = tileUpperLeftY; y <= tileBottomRightY; y++)
            {
                occupiedTiles.Add(new Point(x, y));
            }
        }

        return occupiedTiles;
    }

    //gets the tile coordinate containing the given pixel coordinate
    public Point GetTileAt(Vector2 pos)
    {
        return new Point((int)pos.X / TileWidth, (int)pos.Y / TileHeight);
    }
}