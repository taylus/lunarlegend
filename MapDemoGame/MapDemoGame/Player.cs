using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

public class Player
{
    private World world;

    //constant by which to reduce movement speed per axis when moving diagonally (1 / sqrt(2))
    //private const float DIAG_FACTOR = 0.707106781F;
    private const float DIAG_FACTOR = 0.85F;

    //interval by which to scroll the map view, to stop early if a full step would move us beyond map boundaries
    private const float SCROLL_STEP = 0.1f;

    public float Speed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Color Color { get; protected set; }  //TODO: replace with texture later

    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public Rectangle BoundingBox { get { return new Rectangle((int)ScreenX, (int)ScreenY, Width, Height); } }
    public Vector2 WorldPosition
    {
        get
        {
            return new Vector2(WorldX, WorldY);
        }
        set
        {
            WorldX = value.X;
            WorldY = value.Y;
        }
    }

    public float ScreenX { get { return ScreenPosition.X; } }
    public float ScreenY { get { return ScreenPosition.Y; } }
    public Vector2 ScreenPosition { get { return world.WorldToScreenCoordinates(WorldPosition); } }

    private const float DEFAULT_SPEED = 5.0f;

    public Player(World world, Vector2 pos, int w, int h, float speed = DEFAULT_SPEED)
    {
        this.world = world;
        WorldPosition = pos;
        Speed = speed;
        Width = w;
        Height = h;
        Color = Color.Red;
    }

    public void Draw(SpriteBatch sb)
    {
        Util.DrawRectangle(sb, BoundingBox, Color);
    }

    public void Move(KeyboardState keyboard)
    {
        //TODO: simplify this? it works, but it's messy

        //NOTE: the collision checks against the map borders are "predictive", which works even for fast speeds because they continue indefinitely
        //      against walls with specified widths, we'll pass through them if our speed > their width (tunneling)
        //      this will be a problem if anything is moving very fast, but I don't think that will be the case anywhere

        if (keyboard.IsKeyDown(Keys.W))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float dist = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;
            dist = MathHelper.Min(dist, MathHelper.Distance(WorldY, 0));

            //check if movement is possible (TODO: collisions)
            WorldY -= dist;

            //scroll the map view, if possible
            if (world.ViewY > 0 && ScreenY < world.ViewHeight / 2)
            {
                //scroll in small increments (in case we can't move a full step)
                for (float i = 0; i < dist; i += SCROLL_STEP)
                {
                    world.ViewY -= SCROLL_STEP;
                    if (world.ViewY < 0)
                    {
                        world.ViewY = 0;
                        break;
                    }
                }
            }
        }
        else if (keyboard.IsKeyDown(Keys.S))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float dist = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;
            dist = MathHelper.Min(dist, MathHelper.Distance(WorldY + Height, world.HeightPx));

            //check if movement is possible (TODO: collisions)
            WorldY += dist;

            //scroll the map view, if possible
            if ((world.ViewY + world.ViewHeight) < world.HeightPx && ScreenY > world.ViewHeight / 2)
            {
                //scroll in small increments (in case we can't move a full step)
                for (float i = 0; i < dist; i += SCROLL_STEP)
                {
                    world.ViewY += SCROLL_STEP;
                    if (world.ViewY + world.ViewHeight > world.HeightPx)
                    {
                        world.ViewY = world.HeightPx - world.ViewHeight;
                        break;
                    }
                }
            }
        }
        if (keyboard.IsKeyDown(Keys.A))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float dist = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;
            dist = MathHelper.Min(dist, MathHelper.Distance(WorldX, 0));

            //check if movement is possible (TODO: collisions)
            WorldX -= dist;

            //scroll the map view, if possible
            if (world.ViewX > 0 && ScreenX < world.ViewWidth / 2)
            {
                //scroll in small increments (in case we can't move a full step)
                for (float i = 0; i < dist; i += SCROLL_STEP)
                {
                    world.ViewX -= SCROLL_STEP;
                    if (world.ViewX < 0)
                    {
                        world.ViewX = 0;
                        break;
                    }
                }
            }
        }
        else if (keyboard.IsKeyDown(Keys.D))
        {
            //move slower if going diagonally, and move less if we can't move a full step
            float dist = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;
            dist = MathHelper.Min(dist, MathHelper.Distance(WorldX + Width, world.WidthPx));

            //check if movement is possible (TODO: collisions)
            WorldX += dist;

            //scroll the map view, if possible
            if ((world.ViewX + world.ViewWidth) < world.WidthPx && ScreenX > world.ViewWidth / 2)
            {
                //scroll in small increments (in case we can't move a full step)
                for (float i = 0; i < dist; i += SCROLL_STEP)
                {
                    world.ViewX += SCROLL_STEP;
                    if (world.ViewX + world.ViewWidth > world.WidthPx)
                    {
                        world.ViewX = world.WidthPx - world.ViewWidth;
                        break;
                    }
                }
            }
        }
    }
}