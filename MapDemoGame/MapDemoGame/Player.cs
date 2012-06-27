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

    private const float DEFAULT_SPEED = 4.0f;

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
        if (keyboard.IsKeyDown(Keys.W))
        {
            //reset the player's position if they moved past the top edge
            if (WorldY <= 0) WorldY = 0;
            else
            {
                //move slower if going diagonally
                float howFar = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;

                //check if movement is possible (TODO: collisions)
                WorldY -= howFar;

                //scroll the map if possible
                if (world.ViewY > 0 && ScreenY < world.ViewHeight / 2)
                    world.ViewY -= howFar;
            }
        }
        else if (keyboard.IsKeyDown(Keys.S))
        {
            //reset the player's position if they moved past the bottom edge
            if ((WorldY + Height) >= world.HeightPx) WorldY = (world.HeightPx - Height);
            else
            {
                //move slower if going diagonally
                float howFar = (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.D)) ? Speed * DIAG_FACTOR : Speed;

                //check if movement is possible (TODO: collisions)
                WorldY += howFar;

                //scroll the map if possible
                if ((world.ViewY + world.ViewHeight) < world.HeightPx && ScreenY > world.ViewHeight / 2)
                    world.ViewY += howFar;
            }
        }
        if (keyboard.IsKeyDown(Keys.A))
        {
            //reset the player's position if they moved past the top edge
            if (WorldX <= 0) WorldX = 0;
            else
            {
                //move slower if going diagonally
                float howFar = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;

                //check if movement is possible (TODO: collisions)
                WorldX -= howFar;

                //scroll the map if possible
                if (world.ViewX > 0 && ScreenX < world.ViewWidth / 2)
                    world.ViewX -= howFar;
            }
        }
        else if (keyboard.IsKeyDown(Keys.D))
        {
            //reset the player's position if they moved past the bottom edge
            if ((WorldX + Width) >= world.WidthPx) WorldX = (world.WidthPx - Width);
            else
            {
                //move slower if going diagonally
                float howFar = (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.S)) ? Speed * DIAG_FACTOR : Speed;

                //check if movement is possible (TODO: collisions)
                WorldX += howFar;

                //scroll the map if possible
                if ((world.ViewX + world.ViewWidth) < world.WidthPx && ScreenX > world.ViewWidth / 2)
                    world.ViewX += howFar;
            }
        }
    }
}

public enum Direction
{
    UP,
    DOWN,
    LEFT,
    RIGHT
}
