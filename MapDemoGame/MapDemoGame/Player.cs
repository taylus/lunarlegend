using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public class Player
{
    private World world;

    //constant by which to reduce movement speed per axis when moving diagonally (1 / sqrt(2))
    //private const float DIAG_FACTOR = 0.707106781f;
    private const float DIAG_FACTOR = 0.85f;

    public float Speed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Color Color { get; protected set; }  //TODO: replace with texture later

    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public Vector2 WorldPosition { get { return new Vector2(WorldX, WorldY); } }
    public Rectangle WorldRect { get { return new Rectangle((int)Math.Round(WorldX), (int)Math.Round(WorldY), Width, Height); } }

    public float ScreenX { get { return ScreenPosition.X; } }
    public float ScreenY { get { return ScreenPosition.Y; } }
    public Vector2 ScreenPosition { get { return world.WorldToScreenCoordinates(WorldPosition); } }
    public Rectangle ScreenRect { get { return new Rectangle((int)Math.Round(ScreenX), (int)Math.Round(ScreenY), Width, Height); } }

    private const float DEFAULT_SPEED = 3.5f;

    public Player(World world, Vector2 pos, int w, int h, float speed = DEFAULT_SPEED)
    {
        this.world = world;
        WorldX = pos.X;
        WorldY = pos.Y;
        Speed = speed;
        Width = w;
        Height = h;
        Color = Color.Red;
    }

    public void Draw(SpriteBatch sb)
    {
        Util.DrawRectangle(sb, ScreenRect, Color);
    }

    public void TeleportTo(Vector2 pos)
    {
        WorldX = pos.X;
        WorldY = pos.Y;
        world.CenterViewOnPlayer(this);
    }

    public Point GetTileCoordinates(Vector2? offset = null)
    {
        Vector2 v = offset != null? offset.Value : Vector2.Zero;
        return new Point((int)((WorldX + v.X) / world.TileWidth), (int)((WorldY + v.Y) / world.TileHeight));
    }

    public void Move(KeyboardState keyboard)
    {
        //NOTE: collision checks are "predictive" which poses a "tunnelling" problem when moving at high speeds
        //      checks against the map borders are OK because those continue indefinitely
        //      checks against walls with specified sizes means if our speed > the wall's size, we'll pass right through it
        //      if anything needs to move that fast (seems unlikely) this will need to change to some kind of continuous collision detection

        //walls are currently grid-aligned, full tile blocks
        //moving into a wall prevents movement, but the player will "sidestep" if they just barely touched it

        //move slower if going diagonally
        bool movingDiagonally = (keyboard.IsKeyDown(Keys.W) && (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ||
                                 keyboard.IsKeyDown(Keys.A) && (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ||
                                 keyboard.IsKeyDown(Keys.S) && (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ||
                                 keyboard.IsKeyDown(Keys.D) && (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)));
        float playerSpeed = movingDiagonally ? Speed * DIAG_FACTOR : Speed;

        if (keyboard.IsKeyDown(Keys.W))
        {
            //move less if we can't move a full step
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY, 0));
            Vector2 viewScrollOffset = new Vector2(0, -playerMoveDist);

            //collision detection against walls
            if (world.CollisionLayer != null)
            {
                //predict collisions and adjust movement distance accordingly
                Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY - playerMoveDist), Width, Height);
                List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
                world.Map.HighlightedTiles = tiles;
                if (world.CollisionLayer.TileIntersect(tiles))
                {
                    if (!movingDiagonally)
                    {
                        //"sidestep" to the right if there's no tile to the upper right
                        Point upperRightTileCoords = GetTileCoordinates(new Vector2(Width, -playerMoveDist));
                        if (!world.CollisionLayer.ContainsTileAt(upperRightTileCoords))
                        {
                            float collisionOverlap = (world.TileWidth - (WorldX % world.TileWidth)) % world.TileWidth;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldX += sideStepDist;
                            viewScrollOffset.X += sideStepDist;
                        }

                        //"sidestep" to the left if we're not already moving left, and there's no tile to the upper left
                        Point upperLeftTileCoords = GetTileCoordinates(new Vector2(0, -playerMoveDist));
                        if (!world.CollisionLayer.ContainsTileAt(upperLeftTileCoords))
                        {
                            float collisionOverlap = WorldX % world.TileWidth;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldX -= sideStepDist;
                            viewScrollOffset.X -= sideStepDist;
                        }
                    }

                    //can't fully move up -- move the distance between us and the current tile's top side
                    playerMoveDist = WorldY % world.TileHeight;
                }
            }

            WorldY -= playerMoveDist;
            world.ScrollViewWithinMapBounds(this, viewScrollOffset);
        }
        else if (keyboard.IsKeyDown(Keys.S))
        {
            //move less if we can't move a full step
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY + Height, world.HeightPx));
            Vector2 viewScrollOffset = new Vector2(0, playerMoveDist);

            if (world.CollisionLayer != null)
            {
                //adjust movement distance for wall collisions
                Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY + playerMoveDist), Width, Height);
                List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
                world.Map.HighlightedTiles = tiles;
                if (world.CollisionLayer.TileIntersect(tiles))
                {
                    if (!movingDiagonally)
                    {
                        //"sidestep" to the right if there's no tile to the lower right
                        Point lowerRightTileCoords = GetTileCoordinates(new Vector2(Width, Height + playerMoveDist));
                        if (!world.CollisionLayer.ContainsTileAt(lowerRightTileCoords))
                        {
                            float collisionOverlap = (world.TileWidth - (WorldX % world.TileWidth)) % world.TileWidth;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldX += sideStepDist;
                            viewScrollOffset.X += sideStepDist;
                        }

                        //"sidestep" to the left if there's no tile to the lower left
                        Point lowerLeftTileCoords = GetTileCoordinates(new Vector2(0, Height + playerMoveDist));
                        if (!world.CollisionLayer.ContainsTileAt(lowerLeftTileCoords))
                        {
                            float collisionOverlap = WorldX % world.TileWidth;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldX -= sideStepDist;
                            viewScrollOffset.X -= sideStepDist;
                        }
                    }

                    //can't fully move down -- move the distance between us and the current tile's bottom side
                    playerMoveDist = Util.NearestMultiple((int)WorldY, world.TileHeight) - WorldY;
                }
            }

            WorldY += playerMoveDist;
            world.ScrollViewWithinMapBounds(this, viewScrollOffset);
        }
        if (keyboard.IsKeyDown(Keys.A))
        {
            //move less if we can't move a full step
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX, 0));
            Vector2 viewScrollOffset = new Vector2(-playerMoveDist, 0);

            if (world.CollisionLayer != null)
            {
                //adjust movement distance for wall collisions
                Rectangle predictRect = new Rectangle((int)(WorldX - playerMoveDist), (int)WorldY, Width, Height);
                List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
                world.Map.HighlightedTiles = tiles;
                if (world.CollisionLayer.TileIntersect(tiles))
                {
                    if (!movingDiagonally)
                    {
                        //"sidestep" up if there's no tile to the upper left
                        Point upperLeftTileCoords = GetTileCoordinates(new Vector2(-playerMoveDist, -playerMoveDist));
                        if (!world.CollisionLayer.ContainsTileAt(upperLeftTileCoords))
                        {
                            float collisionOverlap = WorldY % world.TileHeight;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldY -= sideStepDist;
                            viewScrollOffset.Y -= sideStepDist;
                        }

                        //"sidestep" down if there's no tile to the lower left
                        Point lowerLeftTileCoords = GetTileCoordinates(new Vector2(-playerMoveDist, Height + playerMoveDist));
                        if (!world.CollisionLayer.ContainsTileAt(lowerLeftTileCoords))
                        {
                            float collisionOverlap = (world.TileHeight - (WorldY % world.TileHeight)) % world.TileHeight;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldY += sideStepDist;
                            viewScrollOffset.Y += sideStepDist;
                        }
                    }

                    //can't fully move left -- move the distance between us and the current tile's left side
                    playerMoveDist = WorldX % world.TileWidth;
                }
            }

            WorldX -= playerMoveDist;
            world.ScrollViewWithinMapBounds(this, viewScrollOffset);
        }
        else if (keyboard.IsKeyDown(Keys.D))
        {
            //move less if we can't move a full step
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX + Width, world.WidthPx));
            Vector2 viewScrollOffset = new Vector2(playerMoveDist, 0);

            if (world.CollisionLayer != null)
            {
                //adjust movement distance for wall collisions
                Rectangle predictRect = new Rectangle((int)(WorldX + playerMoveDist), (int)WorldY, Width, Height);
                List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
                world.Map.HighlightedTiles = tiles;
                if (world.CollisionLayer.TileIntersect(tiles))
                {
                    if (!movingDiagonally)
                    {
                        //"sidestep" up if there's no tile to the upper right
                        Point upperRightTileCoords = GetTileCoordinates(new Vector2(Width + playerMoveDist, 0));
                        if (!world.CollisionLayer.ContainsTileAt(upperRightTileCoords))
                        {
                            float collisionOverlap = WorldY % world.TileHeight;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldY -= sideStepDist;
                            viewScrollOffset.Y -= sideStepDist;
                        }

                        //"sidestep" down if there's no tile to the lower right
                        Point lowerRightTileCoords = GetTileCoordinates(new Vector2(Width + playerMoveDist, Height));
                        if (!world.CollisionLayer.ContainsTileAt(lowerRightTileCoords))
                        {
                            float collisionOverlap = (world.TileHeight - (WorldY % world.TileHeight)) % world.TileHeight;
                            float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                            WorldY += sideStepDist;
                            viewScrollOffset.Y += sideStepDist;
                        }
                    }

                    //can't fully move right -- move the distance between us and the current tile's right side
                    playerMoveDist = Util.NearestMultiple((int)WorldX, world.TileWidth) - WorldX;
                }
            }

            WorldX += playerMoveDist;
            world.ScrollViewWithinMapBounds(this, viewScrollOffset);
        }
    }
}