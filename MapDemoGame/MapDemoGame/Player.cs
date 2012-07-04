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

    public void Move(KeyboardState keyboard)
    {
        //NOTE: the collision checks against the map borders are "predictive", which works even for fast speeds because they continue indefinitely
        //      against walls with specified widths, we'll pass through them if our speed > their width (tunneling)
        //      this will be a problem if anything is moving very fast, but I don't think that will be the case anywhere

        //TODO: diagonal walls? glide along slope by moving in X and Y instead of just one
        //      tiles could store a "I am diagonal moving upper right", etc diag type
        //      how to enter this into Tiled/read it back out of TMX?

        if (keyboard.IsKeyDown(Keys.W))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY, 0));
            float viewMoveDist = playerMoveDist;

            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY - playerMoveDist), Width, Height);
            List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
            world.Map.HighlightedTiles = tiles;
            if (world.CollisionLayer.TileIntersect(tiles))
            {
                //can't fully move up -- move the distance between us and the current tile's top side
                playerMoveDist = WorldY % world.TileHeight;
            }

            WorldY -= playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(viewMoveDist, MathHelper.Distance(world.ViewY, 0));
            if (ScreenY + (Height / 2) < world.ViewHeight / 2)
                world.ViewY -= worldWiewScrollDist;
        }
        else if (keyboard.IsKeyDown(Keys.S))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY + Height, world.HeightPx));
            float viewMoveDist = playerMoveDist;

            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY + playerMoveDist), Width, Height);
            List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
            world.Map.HighlightedTiles = tiles;
            if (world.CollisionLayer.TileIntersect(tiles))
            {
                //can't fully move down -- move the distance between us and the current tile's bottom side
                playerMoveDist = Util.NearestMultiple((int)WorldY, world.TileHeight) - WorldY;
            }

            WorldY += playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(viewMoveDist, MathHelper.Distance(world.ViewY + world.ViewHeight, world.HeightPx));
            if (ScreenY + (Height / 2) >= world.ViewHeight / 2)
                world.ViewY += worldWiewScrollDist;
        }
        if (keyboard.IsKeyDown(Keys.A))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX, 0));
            float viewMoveDist = playerMoveDist;

            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)(WorldX - playerMoveDist), (int)WorldY, Width, Height);
            List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
            world.Map.HighlightedTiles = tiles;
            if (world.CollisionLayer.TileIntersect(tiles))
            {
                //can't fully move left -- move the distance between us and the current tile's left side
                playerMoveDist = WorldX % world.TileWidth;
            }

            WorldX -= playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(viewMoveDist, MathHelper.Distance(world.ViewX, 0));
            if (ScreenX + (Width / 2) < world.ViewWidth / 2)
                world.ViewX -= worldWiewScrollDist;
        }
        else if (keyboard.IsKeyDown(Keys.D))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX + Width, world.WidthPx));
            float viewMoveDist = playerMoveDist;

            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)(WorldX + playerMoveDist), (int)WorldY, Width, Height);
            List<Point> tiles = world.Map.GetOccupyingTiles(predictRect);
            world.Map.HighlightedTiles = tiles;
            if (world.CollisionLayer.TileIntersect(tiles))
            {
                //can't fully move right -- move the distance between us and the current tile's right side
                playerMoveDist = Util.NearestMultiple((int)WorldX, world.TileWidth) - WorldX;
            }

            WorldX += playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(viewMoveDist, MathHelper.Distance(world.ViewX + world.ViewWidth, world.WidthPx));
            if (ScreenX + (Width / 2) >= world.ViewWidth / 2)
                world.ViewX += worldWiewScrollDist;
        }
    }
}