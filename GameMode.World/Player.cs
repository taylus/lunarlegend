using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public class Player : IWorldEntity
{
    public AnimatedSprite Sprite { get; private set; }

    //constant by which to reduce movement speed per axis when moving diagonally (1 / sqrt(2))
    //private const float DIAG_FACTOR = 0.707106781f;
    private const float DIAG_FACTOR = 0.75f;

    public float Speed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public Vector2 WorldPosition { get { return new Vector2(WorldX, WorldY); } }
    public Rectangle WorldRect { get { return new Rectangle((int)Math.Round(WorldX), (int)Math.Round(WorldY), Width, Height); } }
    public List<Point> OccupyingTiles { get { return World.Current.GetOccupyingTiles(WorldRect); } }

    //a copy of WorldRect inflated by 1 pixel on every side, to allow for touch activation of solid entities by bumping into them
    //public Rectangle InflatedWorldRect { get { return new Rectangle(WorldRect.X - 1, WorldRect.Y - 1, WorldRect.Width + 2, WorldRect.Height + 2); } }

    public float ScreenX { get { return ScreenPosition.X; } }
    public float ScreenY { get { return ScreenPosition.Y; } }
    public Vector2 ScreenPosition { get { return World.Current.WorldToScreenCoordinates(WorldPosition); } }
    public Rectangle ScreenRect { get { return new Rectangle((int)Math.Round(ScreenX), (int)Math.Round(ScreenY), Width, Height); } }
    public bool IsOnScreen { get { return true; } }  //the player is always onscreen

    private const float DEFAULT_SPEED = 3.0f;

    public Player(Vector2 worldPos, int w, int h, float speed = DEFAULT_SPEED)
    {
        WorldX = worldPos.X;
        WorldY = worldPos.Y;
        Speed = speed;
        Width = w;
        Height = h;

        //TODO: put in a factory method, and later persist/load the specifics
        //this sprite sheet also doesn't have diagonally directions, but this game does
        //replace with a more suitable one later; this will require programming changes
        Sprite = new AnimatedSprite("world/locke.png", 16, 24, 2.0f);
        Sprite.SpriteSheetOrigin = new Point(2, 48);
        Sprite.FrameMarginWidth = 14;
        Sprite.FrameMarginHeight = 6;
        Sprite.Animations.Add(new Animation("walk_down", TimeSpan.FromMilliseconds(200), true, new Point(0, 0), new Point(0, 1), new Point(0, 0), new Point(0, 2)));
        Sprite.Animations.Add(new Animation("walk_up", TimeSpan.FromMilliseconds(200), true, new Point(1, 0), new Point(1, 1), new Point(1, 0), new Point(1, 2)));
        Sprite.Animations.Add(new Animation("walk_left", TimeSpan.FromMilliseconds(200), true, new Point(2, 0), new Point(2, 1), new Point(2, 0), new Point(2, 2)));
        Sprite.Animations.Add(new Animation("walk_right", TimeSpan.FromMilliseconds(200), true, new Point(3, 0), new Point(3, 1), new Point(3, 0), new Point(3, 2)));
        Sprite.Animations.Add(new Animation("face_down", TimeSpan.Zero, false, new Point(0, 0)));
        Sprite.Animations.Add(new Animation("face_up", TimeSpan.Zero, false, new Point(1, 0)));
        Sprite.Animations.Add(new Animation("face_left", TimeSpan.Zero, false, new Point(2, 0)));
        Sprite.Animations.Add(new Animation("face_right", TimeSpan.Zero, false, new Point(3, 0)));
        Sprite.SetAnimation("face_down");
    }

    public void Draw(SpriteBatch sb)
    {
        Sprite.Draw(sb);

        if(World.Current.Debug)
            Util.DrawRectangle(sb, ScreenRect, Color.Lerp(Color.Red, Color.Transparent, 0.5f));
    }

    public void Update(GameTime currentGameTime)
    {
        Sprite.Update(currentGameTime);
        //center, but align the bottom of the sprite with the bottom of the bounding box
        //the sprite is really just a graphical effect drawn on top of the box
        Sprite.CenterOn(ScreenRect.Center);
        Sprite.Y = ScreenRect.Y - Math.Abs(ScreenRect.Height - (int)Sprite.ScaledHeight);
        TouchEntities();
    }

    //touch activate any entities we're physically on top of
    private void TouchEntities()
    {
        //TODO: spatially index the entities so we're not checking all of them
        //iterate backwards to allow modifying the collection while this loop runs
        List<WorldEntity> activeEntities = World.Current.Entities.OfType<WorldEntity>().Where(e => e.Active).ToList();
        for (int i = activeEntities.Count - 1; i >= 0; i--)
        {
            WorldEntity w = activeEntities[i];
            if (WorldRect.Intersects(w.WorldRect))
            {
                w.Touch(this);
            }
        }
    }

    public void TeleportTo(Vector2 pos)
    {
        WorldX = pos.X;
        WorldY = pos.Y;
        World.Current.CenterViewOnPlayer(this);
    }

    public Point GetTileCoordinates(Vector2? offset = null)
    {
        Vector2 v = offset != null? offset.Value : Vector2.Zero;
        return new Point((int)((WorldX + v.X) / World.Current.TileWidth), (int)((WorldY + v.Y) / World.Current.TileHeight));
    }

    public void Move(KeyboardState keyboard)
    {
        //if we're not pressing any movement keys, just keep facing in the same direction
        //makes the player stop animating once they stop pressing movement keys
        if (!(keyboard.IsKeyDown(Buttons.MOVE_UP) || keyboard.IsKeyDown(Buttons.MOVE_DOWN) ||
              keyboard.IsKeyDown(Buttons.MOVE_LEFT) || keyboard.IsKeyDown(Buttons.MOVE_RIGHT)))
        {
            if (Sprite.Animation.Name == "walk_down") Sprite.SetAnimation("face_down");
            else if (Sprite.Animation.Name == "walk_left") Sprite.SetAnimation("face_left");
            else if (Sprite.Animation.Name == "walk_right") Sprite.SetAnimation("face_right");
            else if (Sprite.Animation.Name == "walk_up") Sprite.SetAnimation("face_up");
            return;
        }

        //move slower if going diagonally
        bool movingDiagonally = (keyboard.IsKeyDown(Buttons.MOVE_UP) && (keyboard.IsKeyDown(Buttons.MOVE_LEFT) || keyboard.IsKeyDown(Buttons.MOVE_RIGHT)) ||
                                 keyboard.IsKeyDown(Buttons.MOVE_LEFT) && (keyboard.IsKeyDown(Buttons.MOVE_UP) || keyboard.IsKeyDown(Buttons.MOVE_DOWN)) ||
                                 keyboard.IsKeyDown(Buttons.MOVE_DOWN) && (keyboard.IsKeyDown(Buttons.MOVE_LEFT) || keyboard.IsKeyDown(Buttons.MOVE_RIGHT)) ||
                                 keyboard.IsKeyDown(Buttons.MOVE_RIGHT) && (keyboard.IsKeyDown(Buttons.MOVE_UP) || keyboard.IsKeyDown(Buttons.MOVE_DOWN)));
        float playerSpeed = movingDiagonally ? Speed * DIAG_FACTOR : Speed;

        if (keyboard.IsKeyDown(Buttons.MOVE_UP))
        {
            MoveUp(movingDiagonally, playerSpeed);
        }
        else if (keyboard.IsKeyDown(Buttons.MOVE_DOWN))
        {
            MoveDown(movingDiagonally, playerSpeed);
        }
        if (keyboard.IsKeyDown(Buttons.MOVE_LEFT))
        {
            MoveLeft(movingDiagonally, playerSpeed);
        }
        else if (keyboard.IsKeyDown(Buttons.MOVE_RIGHT))
        {
            MoveRight(movingDiagonally, playerSpeed);
        }
    }


    #region Movement Functions

    //These functions implement the actual player movement and world tile collision detection
    //They're all similar in structure, but because each one is a different direction, the variables involved
    //(world rect coordinates and dimensions, sign on the player's movement) change enough to make refactoring not worth it
    //Note that collision checks are "predictive" which poses a "tunnelling" problem when moving at high speeds
    //Checks against the map borders are OK because those continue indefinitely, but checks against walls with 
    //specified sizes means if our speed > the wall's size, we'll pass right through it if anything needs to move
    //that fast (seems unlikely) this will need to change to some kind of continuous collision detection
    //Walls are currently grid-aligned, full tile blocks
    //Moving into a wall prevents movement, but the player will "sidestep" it if they aren't completely blocked

    private void MoveUp(bool movingDiagonally, float playerSpeed)
    {
        //move less if we can't move a full step
        float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY, 0));
        Vector2 viewScrollOffset = new Vector2(0, -playerMoveDist);

        //collision detection against walls
        if (World.Current.CollisionLayer != null)
        {
            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY - playerMoveDist), Width, Height);
            List<Point> tiles = World.Current.GetOccupyingTiles(predictRect);
            World.Current.Map.HighlightedTiles = tiles;
            if (World.Current.CollisionLayer.TileIntersect(tiles))
            {
                if (!movingDiagonally)
                {
                    //"sidestep" to the right if there's no tile to the upper right
                    Point upperRightTileCoords = GetTileCoordinates(new Vector2(Width, -playerMoveDist));
                    if (!World.Current.CollisionLayer.ContainsTileAt(upperRightTileCoords) && !World.Current.EntityCollision(upperRightTileCoords))
                    {
                        float collisionOverlap = (World.Current.TileWidth - (WorldX % World.Current.TileWidth)) % World.Current.TileWidth;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldX += sideStepDist;
                        viewScrollOffset.X += sideStepDist;
                    }

                    //"sidestep" to the left if we're not already moving left, and there's no tile to the upper left
                    Point upperLeftTileCoords = GetTileCoordinates(new Vector2(0, -playerMoveDist));
                    if (!World.Current.CollisionLayer.ContainsTileAt(upperLeftTileCoords) && !World.Current.EntityCollision(upperLeftTileCoords))
                    {
                        float collisionOverlap = WorldX % World.Current.TileWidth;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldX -= sideStepDist;
                        viewScrollOffset.X -= sideStepDist;
                    }
                }

                //can't fully move up -- move the distance between us and the current tile's top side
                playerMoveDist = WorldY % World.Current.TileHeight;
            }
        }
        foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>().Where(w => w.Solid))
        {
            Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY - playerMoveDist), Width, Height);
            if (predictRect.Intersects(wEnt.WorldRect))
            {
                playerMoveDist = MathHelper.Max(0, WorldY - (wEnt.WorldY + wEnt.Height));
            }
        }

        if (playerMoveDist <= 0)
        {
            //stop animating when blocked by something solid
            Sprite.SetAnimation("face_up");
        }
        else if (!movingDiagonally || Sprite.Animation.Name.StartsWith("face"))
        {
            //4-directional animations with 8-directional movement,
            //so only change animation when not moving diagonally, unless we're at a stand-still
            Sprite.SetAnimation("walk_up");
        }
        WorldY -= playerMoveDist;
        World.Current.ScrollViewWithinMapBounds(this, viewScrollOffset);
    }

    private void MoveDown(bool movingDiagonally, float playerSpeed)
    {
        //move less if we can't move a full step
        float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY + Height, World.Current.HeightPx));
        Vector2 viewScrollOffset = new Vector2(0, playerMoveDist);

        if (World.Current.CollisionLayer != null)
        {
            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY + playerMoveDist), Width, Height);
            List<Point> tiles = World.Current.GetOccupyingTiles(predictRect);
            World.Current.Map.HighlightedTiles = tiles;
            if (World.Current.CollisionLayer.TileIntersect(tiles))
            {
                if (!movingDiagonally)
                {
                    //"sidestep" to the right if there's no tile to the lower right
                    Point lowerRightTileCoords = GetTileCoordinates(new Vector2(Width, Height + playerMoveDist));
                    if (!World.Current.CollisionLayer.ContainsTileAt(lowerRightTileCoords) && !World.Current.EntityCollision(lowerRightTileCoords))
                    {
                        float collisionOverlap = (World.Current.TileWidth - (WorldX % World.Current.TileWidth)) % World.Current.TileWidth;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldX += sideStepDist;
                        viewScrollOffset.X += sideStepDist;
                    }

                    //"sidestep" to the left if there's no tile to the lower left
                    Point lowerLeftTileCoords = GetTileCoordinates(new Vector2(0, Height + playerMoveDist));
                    if (!World.Current.CollisionLayer.ContainsTileAt(lowerLeftTileCoords) && !World.Current.EntityCollision(lowerLeftTileCoords))
                    {
                        float collisionOverlap = WorldX % World.Current.TileWidth;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldX -= sideStepDist;
                        viewScrollOffset.X -= sideStepDist;
                    }
                }

                //can't fully move down -- move the distance between us and the current tile's bottom side
                playerMoveDist = Util.NearestMultiple((int)WorldY, World.Current.TileHeight) - WorldY;
            }
        }
        foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>().Where(w => w.Solid))
        {
            Rectangle predictRect = new Rectangle((int)WorldX, (int)(WorldY + playerMoveDist), Width, Height);
            if (predictRect.Intersects(wEnt.WorldRect))
            {
                playerMoveDist = MathHelper.Max(0, wEnt.WorldY - (WorldY + Height));
            }
        }

        if (playerMoveDist <= 0)
        {
            //stop animating when blocked by something solid
            Sprite.SetAnimation("face_down");
        }
        else if (!movingDiagonally || Sprite.Animation.Name.StartsWith("face"))
        {
            //4-directional animations with 8-directional movement,
            //so only change animation when not moving diagonally, unless we're at a stand-still
            Sprite.SetAnimation("walk_down");
        }
        WorldY += playerMoveDist;
        World.Current.ScrollViewWithinMapBounds(this, viewScrollOffset);
    }

    private void MoveLeft(bool movingDiagonally, float playerSpeed)
    {
        //move less if we can't move a full step
        float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX, 0));
        Vector2 viewScrollOffset = new Vector2(-playerMoveDist, 0);

        if (World.Current.CollisionLayer != null)
        {
            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)(WorldX - playerMoveDist), (int)WorldY, Width, Height);
            List<Point> tiles = World.Current.GetOccupyingTiles(predictRect);
            World.Current.Map.HighlightedTiles = tiles;
            if (World.Current.CollisionLayer.TileIntersect(tiles))
            {
                if (!movingDiagonally)
                {
                    //"sidestep" up if there's no tile to the upper left
                    Point upperLeftTileCoords = GetTileCoordinates(new Vector2(-playerMoveDist, 0));
                    if (!World.Current.CollisionLayer.ContainsTileAt(upperLeftTileCoords) && !World.Current.EntityCollision(upperLeftTileCoords))
                    {
                        float collisionOverlap = WorldY % World.Current.TileHeight;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldY -= sideStepDist;
                        viewScrollOffset.Y -= sideStepDist;
                    }

                    //"sidestep" down if there's no tile to the lower left
                    Point lowerLeftTileCoords = GetTileCoordinates(new Vector2(-playerMoveDist, Height));
                    if (!World.Current.CollisionLayer.ContainsTileAt(lowerLeftTileCoords) && !World.Current.EntityCollision(lowerLeftTileCoords))
                    {
                        float collisionOverlap = (World.Current.TileHeight - (WorldY % World.Current.TileHeight)) % World.Current.TileHeight;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldY += sideStepDist;
                        viewScrollOffset.Y += sideStepDist;
                    }
                }

                //can't fully move left -- move the distance between us and the current tile's left side
                playerMoveDist = WorldX % World.Current.TileWidth;
            }
        }
        foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>().Where(w => w.Solid))
        {
            Rectangle predictRect = new Rectangle((int)(WorldX - playerMoveDist), (int)WorldY, Width, Height);
            if (predictRect.Intersects(wEnt.WorldRect))
            {
                playerMoveDist = MathHelper.Max(0, WorldX - (wEnt.WorldX + wEnt.Width));
            }
        }

        if (playerMoveDist <= 0)
        {
            //stop animating when blocked by something solid
            Sprite.SetAnimation("face_left");
        }
        else if (!movingDiagonally || Sprite.Animation.Name.StartsWith("face"))
        {
            //4-directional animations with 8-directional movement,
            //so only change animation when not moving diagonally, unless we're at a stand-still
            Sprite.SetAnimation("walk_left");
        }
        WorldX -= playerMoveDist;
        World.Current.ScrollViewWithinMapBounds(this, viewScrollOffset);
    }

    private void MoveRight(bool movingDiagonally, float playerSpeed)
    {
        //move less if we can't move a full step
        float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX + Width, World.Current.WidthPx));
        Vector2 viewScrollOffset = new Vector2(playerMoveDist, 0);

        if (World.Current.CollisionLayer != null)
        {
            //adjust movement distance for wall collisions
            Rectangle predictRect = new Rectangle((int)(WorldX + playerMoveDist), (int)WorldY, Width, Height);
            List<Point> tiles = World.Current.GetOccupyingTiles(predictRect);
            World.Current.Map.HighlightedTiles = tiles;
            if (World.Current.CollisionLayer.TileIntersect(tiles))
            {
                if (!movingDiagonally)
                {
                    //"sidestep" up if there's no tile to the upper right
                    Point upperRightTileCoords = GetTileCoordinates(new Vector2(Width + playerMoveDist, 0));
                    if (!World.Current.CollisionLayer.ContainsTileAt(upperRightTileCoords) && !World.Current.EntityCollision(upperRightTileCoords))
                    {
                        float collisionOverlap = WorldY % World.Current.TileHeight;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldY -= sideStepDist;
                        viewScrollOffset.Y -= sideStepDist;
                    }

                    //"sidestep" down if there's no tile to the lower right
                    Point lowerRightTileCoords = GetTileCoordinates(new Vector2(Width + playerMoveDist, Height));
                    if (!World.Current.CollisionLayer.ContainsTileAt(lowerRightTileCoords) && !World.Current.EntityCollision(lowerRightTileCoords))
                    {
                        float collisionOverlap = (World.Current.TileHeight - (WorldY % World.Current.TileHeight)) % World.Current.TileHeight;
                        float sideStepDist = Math.Min(playerMoveDist, collisionOverlap);
                        WorldY += sideStepDist;
                        viewScrollOffset.Y += sideStepDist;
                    }
                }

                //can't fully move right -- move the distance between us and the current tile's right side
                playerMoveDist = Util.NearestMultiple((int)WorldX, World.Current.TileWidth) - WorldX;
            }
        }
        foreach (WorldEntity wEnt in World.Current.Entities.OfType<WorldEntity>().Where(w => w.Solid))
        {
            Rectangle predictRect = new Rectangle((int)(WorldX + playerMoveDist), (int)WorldY, Width, Height);
            if (predictRect.Intersects(wEnt.WorldRect))
            {
                playerMoveDist = MathHelper.Max(0, wEnt.WorldX - (WorldX + Width));
            }
        }

        if (playerMoveDist <= 0)
        {
            //stop animating when blocked by something solid
            Sprite.SetAnimation("face_right");
        }
        else if (!movingDiagonally || Sprite.Animation.Name.StartsWith("face"))
        {
            //4-directional animations with 8-directional movement,
            //so only change animation when not moving diagonally, unless we're at a stand-still
            Sprite.SetAnimation("walk_right");
        }

        WorldX += playerMoveDist;
        World.Current.ScrollViewWithinMapBounds(this, viewScrollOffset);
    }

    #endregion
}