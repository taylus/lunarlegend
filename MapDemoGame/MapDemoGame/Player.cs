using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public class Player
{
    private World world;

    //constant by which to reduce movement speed per axis when moving diagonally (1 / sqrt(2))
    //private const float DIAG_FACTOR = 0.707106781F;
    private const float DIAG_FACTOR = 0.85F;

    public float Speed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Color Color { get; protected set; }  //TODO: replace with texture later

    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public Vector2 WorldPosition { get { return new Vector2(WorldX, WorldY); } }
    public Rectangle WorldBoundingBox { get { return new Rectangle((int)WorldX, (int)WorldY, Width, Height); } }

    public float ScreenX { get { return ScreenPosition.X; } }
    public float ScreenY { get { return ScreenPosition.Y; } }
    public Vector2 ScreenPosition { get { return world.WorldToScreenCoordinates(WorldPosition); } }
    public Rectangle ScreenBoundingBox { get { return new Rectangle((int)ScreenX, (int)ScreenY, Width, Height); } }

    private const float DEFAULT_SPEED = 4.0f;

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
        Util.DrawRectangle(sb, ScreenBoundingBox, Color);
    }

    public void Move(KeyboardState keyboard)
    {
        //TODO: simplify this? it works, but it's messy

        //NOTE: the collision checks against the map borders are "predictive", which works even for fast speeds because they continue indefinitely
        //      against walls with specified widths, we'll pass through them if our speed > their width (tunneling)
        //      this will be a problem if anything is moving very fast, but I don't think that will be the case anywhere

        //FIX: scrolling the world view in increments makes the player rattle around a bit
        //     as floating point discrepencies add up between the player's world coords and calculated screen coords

        if (keyboard.IsKeyDown(Keys.W))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY, 0));

            //check if movement is possible (TODO: collisions)
            WorldY -= playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(playerMoveDist, MathHelper.Distance(world.ViewY, 0));
            if (ScreenY < world.ViewHeight / 2)
                world.ViewY -= worldWiewScrollDist;
        }
        else if (keyboard.IsKeyDown(Keys.S))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldY + Height, world.HeightPx));

            //check if movement is possible (TODO: collisions)
            WorldY += playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(playerMoveDist, MathHelper.Distance(world.ViewY + world.ViewHeight, world.HeightPx));
            if (ScreenY >= world.ViewHeight / 2)
                world.ViewY += worldWiewScrollDist;
        }
        if (keyboard.IsKeyDown(Keys.A))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX, 0));

            //check if movement is possible (TODO: collisions)
            WorldX -= playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(playerMoveDist, MathHelper.Distance(world.ViewX, 0));
            if (ScreenX < world.ViewWidth / 2)
                world.ViewX -= worldWiewScrollDist;
        }
        else if (keyboard.IsKeyDown(Keys.D))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float playerSpeed = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;
            float playerMoveDist = MathHelper.Min(playerSpeed, MathHelper.Distance(WorldX + Width, world.WidthPx));

            //check if movement is possible (TODO: collisions)
            WorldX += playerMoveDist;

            //must calculate player and view distances separately, as player may move independently of view
            float worldWiewScrollDist = MathHelper.Min(playerMoveDist, MathHelper.Distance(world.ViewX + world.ViewWidth, world.WidthPx));
            if (ScreenX >= world.ViewWidth / 2)
                world.ViewX += worldWiewScrollDist;
        }
    }
}