using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Graphics;

//represents a basic entity that can be triggered by some means (player touch, worldspawn, etc)
//this is essentially how Tiled Objects are incorporated into the game world
//all entities must implement a constructor that only takes a Tiled Object 
//due to the way they are instantiated w/ reflection on worldspawn
public abstract class Entity
{
    public Object Object { get; protected set; }
    public bool Active { get; set; }
    public Entity Next { get; set; }  //for entity chaining
    //TODO: fire scheduler to delay firing next entity?
    //Once implemented, make trigger_delay entity
    //Button -> Sound (long, think Zelda "you solved the puzzle" sound) -> Delay (duration of sound) -> Wall (open a door)

    //ties Tiled Objects to their corresponding Entity classes
    //called during worldspawn to initialize entities of the proper type
    public static string GetEntityTypeName() { return null; }

    //default ctor
    public Entity(Object obj)
    {
        Object = obj;
        Active = true;
    }

    //called when/if this entity is triggered by something
    public virtual void Fire() 
    {
        //fire next entity in chain (if there is one)
        if (Next != null) Next.Fire();
    }

    //delayed construction performed in worldspawn's second pass to wire up the entity 
    //from its Tiled Object's properties after all entities have been instantiated
    public virtual void Initialize()
    {
        string nextEntityName = Object.Properties.GetValue("target");
        if (!string.IsNullOrWhiteSpace(nextEntityName))
        {
            Next = World.Current.Entities.GetByName(nextEntityName);
        }
    }

    public void Delete()
    {
        World.Current.Entities.Remove(this);
    }

    //returns the inheriting entity type that represents the given Tiled Object type
    public static Type GetEntityType(string type)
    {
        return (from Type t in Assembly.GetExecutingAssembly().GetTypes() 
                where t != typeof(Entity) &&
                    typeof(Entity).IsAssignableFrom(t) && !t.IsAbstract &&
                    (string)t.GetMethod("GetEntityTypeName").Invoke(null, null) == type 
                select t).FirstOrDefault();
    }

    protected Texture2D LoadTextureFromProperty(string propertyName)
    {
        string imgFile = Object.Properties.GetValue(propertyName);
        if (string.IsNullOrWhiteSpace(imgFile)) return null;
        imgFile = Path.Combine(World.Current.Map.MapFileDir, imgFile);
        return BaseGame.LoadTexture(imgFile, true);
    }

    protected SoundEffect LoadSoundEffectFromProperty(string propertyName)
    {
        string soundFile = Object.Properties.GetValue(propertyName);
        if (string.IsNullOrWhiteSpace(soundFile)) return null;
        soundFile = Path.Combine(World.Current.Map.MapFileDir, soundFile);
        return BaseGame.LoadSoundEffect(soundFile, true);
    }
}

//describes anything that has a position in the world (and thus the screen)
public interface IWorldEntity
{
    int Width { get; set; }
    int Height { get; set; }

    float WorldX { get; set; }
    float WorldY { get; set; }
    Vector2 WorldPosition { get; }
    Rectangle WorldRect { get; }
    List<Point> OccupyingTiles { get; }

    float ScreenX { get; }
    float ScreenY { get; }
    Vector2 ScreenPosition { get; }
    Rectangle ScreenRect { get; }
    bool IsOnScreen { get; }
}

//represents an entity that has a position in the world
public abstract class WorldEntity : Entity, IWorldEntity
{
    public int Width { get; set; }
    public int Height { get; set; }

    public float WorldX { get; set; }
    public float WorldY { get; set; }
    public Vector2 WorldPosition { get { return new Vector2(WorldX, WorldY); } }
    public Rectangle WorldRect { get { return new Rectangle((int)Math.Round(WorldX), (int)Math.Round(WorldY), Width, Height); } }
    public List<Point> OccupyingTiles { get { return World.Current.GetOccupyingTiles(WorldRect); } }
    public Rectangle InteractRect
    {
        get
        {
            //a rect 1/2 tile size in each direction
            //if the player is within this rect, they can use this entity
            int x = (int)WorldX - (World.Current.TileWidth / 2);
            int y = (int)WorldY - (World.Current.TileHeight / 2);
            int w = Width + World.Current.TileWidth;
            int h = Height + World.Current.TileHeight;
            return new Rectangle(x, y, w, h);
        }
    }
    public bool Solid { get; set; }

    public float ScreenX { get { return ScreenPosition.X; } }
    public float ScreenY { get { return ScreenPosition.Y; } }
    public Vector2 ScreenPosition { get { return World.Current.WorldToScreenCoordinates(WorldPosition); } }
    public Rectangle ScreenRect { get { return new Rectangle((int)Math.Round(ScreenX), (int)Math.Round(ScreenY), Width, Height); } }
    public bool IsOnScreen { get { return Overworld.GameWindow.Contains((int)ScreenX, (int)ScreenY); } }

    public WorldEntity(Object obj) : base(obj) 
    {
        Solid = false;
    }

    //called when/if this entity is touched by the player
    public virtual void Touch(Player p) { }

    //called when/if this entity is used by the player
    public virtual void Use(Player p) { }

    public virtual void Draw(SpriteBatch sb) { }

    public override void Initialize()
    {
        WorldX = Object.X;
        WorldY = Object.Y;
        Width = Object.Width;
        Height = Object.Height;
        base.Initialize();
    }
}

public class MultiManager : Entity
{
    new public static string GetEntityTypeName() { return "multi_manager"; }
    public List<Entity> Targets { get; set; }

    public MultiManager(Object obj) : base(obj) 
    {
        Targets = new List<Entity>();
    }

    public override void Initialize()
    {
        for (int i = 0; ; i++)
        {
            string targetEntityName = Object.Properties.GetValue("target" + i);
            if(string.IsNullOrWhiteSpace(targetEntityName)) break;

            Entity targetEntity = World.Current.Entities.GetByName(targetEntityName);
            if (targetEntity != null) Targets.Add(targetEntity);
        }
    }

    public override void Fire()
    {
        foreach (Entity e in Targets)
        {
            e.Fire();
        }
    }
}

public class PlayerSpawn : WorldEntity
{
    new public static string GetEntityTypeName() { return "info_player_start"; }
    public PlayerSpawn(Object obj) : base(obj) { }
}

public class TeleportEntrance : WorldEntity
{
    new public static string GetEntityTypeName() { return "trigger_teleport"; }

    public override void Touch(Player p)
    {
        if (Next != null)
        {
            p.TeleportTo(Next.Object.Position);
        }
    }

    public TeleportEntrance(Object obj) : base(obj) { }
}

public class TeleportDestination : WorldEntity
{
    new public static string GetEntityTypeName() { return "info_teleport_destination"; }
    public TeleportDestination(Object obj) : base(obj) { }
}

public class ChangeLevel : WorldEntity
{
    new public static string GetEntityTypeName() { return "trigger_changelevel"; }
    public string LevelName 
    { 
        get 
        { 
            string levelName = Object.Properties.GetValue("level");
            if(levelName.EndsWith(".tmx")) return levelName;
            return levelName + ".tmx";
        } 
    }

    public ChangeLevel(Object obj) : base(obj) { }

    public override void Touch(Player p)
    {
        Overworld.LoadWorld(Path.Combine("maps", LevelName));
    }
}

public class NPC : WorldEntity
{
    new public static string GetEntityTypeName() { return "info_npc"; }
    public Texture2D Image { get; set; }
    public MessageBox Dialogue { get; set; }

    //TODO: spritesheets/animations
    //TODO: behavior enum (stationary, random movement, predefined path)
    //TODO: opacity

    public NPC(Object obj) : base(obj) 
    {
        Solid = true;
    }

    public override void Initialize()
    {
        //initialize WorldEntity properties first
        //NPC's bounding box will be overwritten by image size
        base.Initialize();

        Image = LoadTextureFromProperty("img");
        if (Image == null) return;
        Width = Image.Width;
        Height = Image.Height;

        string text = Object.Properties.GetValue("text");
        if (!string.IsNullOrWhiteSpace(text))
        {
            //TODO: fix coupling between this and Overworld game
            //NPCs shouldn't need to concern themselves with MessageBox positioning
            if (text.EndsWith(".graphml", StringComparison.OrdinalIgnoreCase))
            {
                string graphMLFile = Path.Combine(World.Current.Map.MapFileDir, text);
                Dialogue = MessageBox.LoadFromGraphFile(Overworld.CreateMessageBoxTemplate(), graphMLFile);
            }
            else
            {
                text = Regex.Unescape(text);
                Dialogue = new MessageBox(Overworld.CreateMessageBoxTemplate(), text);
            }
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        sb.Draw(Image, ScreenPosition.Round(), Color.White);
        if (World.Current.Debug)
        {
            Util.DrawRectangle(sb, World.Current.ScreenToWorldRectangle(InteractRect), Color.Lerp(Color.LimeGreen, Color.Transparent, 0.55f));
        }
    }
}

public class Enemy : WorldEntity
{
    new public static string GetEntityTypeName() { return "info_enemy"; }
    public Texture2D Image { get; set; }
    public List<EnemyCombatEntity> CombatParty { get; private set; }

    public Enemy(Object obj) : base(obj) 
    {
        //not solid -> touching begins combat
    }

    public override void Initialize()
    {
        //initialize WorldEntity properties first
        //enemy's bounding box will be overwritten by image size
        base.Initialize();
        Image = LoadTextureFromProperty("img");
        if (Image == null) return;
        Width = Image.Width;
        Height = Image.Height;
        CombatParty = LoadPartyFromProperty("partyID");
    }

    public override void Touch(Player p)
    {
        Overworld.BeginCombat(this, CombatParty);
    }

    private List<EnemyCombatEntity> LoadPartyFromProperty(string propertyName)
    {
        string partyProperty = Object.Properties.GetValue(propertyName);
        int partyID;
        if(!int.TryParse(partyProperty, out partyID))
        {
            throw new ArgumentException(string.Format("Enemy party ID must be an integer. Got: {0} for info_enemy '{1}'", partyProperty, Object.Name));
        }

        return Enemies.LoadPartyByID(partyID);
    }

    public override void Draw(SpriteBatch sb)
    {
        sb.Draw(Image, ScreenPosition.Round(), Color.White);
        if (World.Current.Debug)
        {
            Util.DrawRectangle(sb, World.Current.ScreenToWorldRectangle(InteractRect), Color.Lerp(Color.LimeGreen, Color.Transparent, 0.55f));
        }
    }
}

//will these really be placed on their own much?
//maybe store the sound on some entities as a chained Sound entity, instead of an XNA SoundEffect?
public class Sound : Entity
{
    new public static string GetEntityTypeName() { return "info_sound"; }
    public SoundEffect SoundEffect { get; set; }

    public Sound(Object obj) : base(obj) { }

    public void Play()
    {
        if(SoundEffect != null) SoundEffect.Play();
    }

    public override void Fire()
    {
        Play();
        base.Fire();
    }
}

public class Music : Entity
{
    new public static string GetEntityTypeName() { return "info_music"; }
    public Song Song { get; set; }

    public Music(Object obj) : base(obj) { }
    public void Play()
    {
        if(Song != null) MediaPlayer.Play(Song);
    }
}

public class Wall : WorldEntity
{
    new public static string GetEntityTypeName() { return "func_wall"; }
    private Texture2D closedImage;
    private Texture2D openImage;

    public Wall(Object obj) : base(obj)
    {
        Solid = true;
    }

    public override void Initialize()
    {
        base.Initialize();
        closedImage = LoadTextureFromProperty("closed_img");
        openImage = LoadTextureFromProperty("open_img");
    }

    public override void Fire()
    {
        Solid = !Solid;
    }

    public override void Draw(SpriteBatch sb)
    {
        Texture2D tex = Solid ? closedImage : openImage;
        if (tex != null)
        {
            sb.Draw(tex, ScreenPosition.Round(), Color.White);
        }
    }
}

public class Button : WorldEntity
{
    new public static string GetEntityTypeName() { return "trigger_button"; }
    private Texture2D upImage;
    private Texture2D downImage;
    public SoundEffect Sound { get; set; }
    public Button(Object obj) : base(obj) { }

    public override void Touch(Player p)
    {
        if (Next != null)
        {
            if(Sound != null) Sound.Play();
            Next.Fire();
            Active = false;
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        Sound = LoadSoundEffectFromProperty("sound");
        upImage = LoadTextureFromProperty("up_img");
        downImage = LoadTextureFromProperty("down_img");
    }

    public override void Draw(SpriteBatch sb)
    {
        Texture2D tex = Active ? upImage : downImage;
        if (tex != null)
        {
            sb.Draw(tex, ScreenPosition.Round(), Color.White);
        }
    }
}

public static class EntityExtensions
{
    public static Entity GetByName(this List<Entity> entities, string name)
    {
        return entities.Where(e => e.Object.Name == name).FirstOrDefault();
    }

    public static void Remove(this List<Entity> entities, string name)
    {
        entities.Remove(entities.GetByName(name));
    }
}