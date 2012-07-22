using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//TODO: nest this class inside MessageBoxSeries?
public class MessageBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Rectangle Rectangle { get { return new Rectangle(X, Y, Width, Height); } }
    public int Padding { get; set; }
    public int BorderWidth { get; set; }
    public float Opacity { get; set; }
    public Color BorderColor { get; set; }
    public Color BackgroundColor { get; set; }
    public Color FontColor { get; set; }
    public SpriteFont Font { get; set; }
    public Texture2D Portrait { get; set; }
    private int PortraitWidth { get { return Portrait == null ? 0 : Portrait.Width + (2 * PORTRAIT_PADDING); } }
    private int PortraitHeight { get { return Portrait == null ? 0 : Portrait.Height + (2 * PORTRAIT_PADDING); } }
    //TODO: gradient backgrounds
    //TODO: timed text rendering/fading

    private List<string> lines = new List<string>();
    private const int PORTRAIT_PADDING = 4;
    private const int TEXT_LEFT_PADDING = 10;

    public MessageBox(int x, int y, int w, int h, SpriteFont font, Texture2D portrait, ref string text)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
        Font = font;
        Portrait = portrait;

        //resize to fit the portrait if necessary
        if (Portrait != null)
        {
            Width = Math.Max(Width, (Portrait.Width) + (PORTRAIT_PADDING * 2));
            Height = Math.Max(Height, Portrait.Height + (PORTRAIT_PADDING * 2));
        }

        Padding = 4;
        BorderWidth = 2;
        Opacity = 0.65f;
        BackgroundColor = Color.Lerp(Color.Transparent, Color.DarkBlue, MathHelper.Clamp(Opacity, 0, 1));
        //BorderColor = Color.Lerp(Color.Transparent, Color.LightSteelBlue, MathHelper.Clamp(Opacity, 0, 1));
        BorderColor = Color.LightSteelBlue;
        FontColor = Color.White;

        if (text != null) text = WrapText(text);
    }

    public MessageBox(MessageBox template, ref string text) : 
        this(template.X, template.Y, template.Width, template.Height, template.Font, template.Portrait, ref text)
    {

    }

    public static MessageBox CreateTemplate(int x, int y, int w, int h, SpriteFont font, Texture2D portrait)
    {
        string s = null;
        return new MessageBox(x, y, w, h, font, portrait, ref s);
    }

    //wraps the given text horizontally within a single MessageBox
    public string WrapText(string text)
    {
        //remove carriage return from carriage return + newline pairs
        text = Regex.Replace(text, "\r\n", "\n").Trim();

        StringBuilder sb = new StringBuilder();
        lines = new List<string>();
        int processedChars = 0;

        string[] tokens = Regex.Split(text, @"(\s)").Where(w => w != string.Empty).ToArray();
        foreach (string token in tokens)
        {
            if (token == "\n" || Font.MeasureString(sb.ToString() + token).X > Width - ((Padding + BorderWidth) * 2) - PortraitWidth - TEXT_LEFT_PADDING)
            {
                lines.Add(sb.ToString());
                sb.Clear();

                if (((lines.Count + 1) * Font.LineSpacing) > Height - ((Padding + BorderWidth) * 2))
                {
                    //another line won't fit... return what remains
                    return text.Substring(processedChars);
                }
            }

            if(token != "\n") sb.Append(token);
            processedChars += token.Length;
        }
        
        lines.Add(sb.ToString());
        return string.Empty;
    }

    public void Draw(SpriteBatch sb)
    {
        Util.DrawRectangle(sb, Rectangle, BackgroundColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X, Y), new Vector2(X + Width, Y), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X + Width, Y), new Vector2(X + Width, Y + Height), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X + Width, Y + Height), new Vector2(X, Y + Height), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X, Y + Height), new Vector2(X, Y), BorderColor);

        if (Portrait != null)
        {
            sb.Draw(Portrait, new Vector2(X + PORTRAIT_PADDING, GetVertCenteredPortraitTop() + PORTRAIT_PADDING), Color.White);
        }

        for (int i = 0; i < lines.Count; i++)
        {
            sb.DrawString(Font, lines[i], new Vector2(X + PortraitWidth + Padding + TEXT_LEFT_PADDING, Y + Padding + (Font.LineSpacing * i)), Color.White);
        }
    }

    private int GetVertCenteredPortraitTop()
    {
        return (Y + (Height / 2)) - (PortraitHeight / 2);
    }

    public override string ToString()
    {
        return string.Join("\n", lines);
    }
}

public class MessageBoxSeries : IEnumerable<MessageBox>
{
    public MessageBox TemplateMessageBox { get; set; }
    public List<MessageBox> MessageBoxes { get; set; }
    public int Count { get { return MessageBoxes != null? MessageBoxes.Count : 0; } }
    public MessageBox this[int i] { get { return MessageBoxes[i]; } }

    public MessageBoxSeries(int x, int y, int w, int h, SpriteFont font, Texture2D portrait, string text = null)
    {
        //store all the settings in the template so we don't need to keep copies
        TemplateMessageBox = MessageBox.CreateTemplate(x, y, w, h, font, portrait);

        if (text == null)
            MessageBoxes = new List<MessageBox>();
        else
            MessageBoxes = WrapText(text);
    }

    public List<MessageBox> WrapText(string text)
    {
        List<MessageBox> messageBoxes = new List<MessageBox>();

        while (text.Length > 0)
        {
            messageBoxes.Add(new MessageBox(TemplateMessageBox, ref text));
        }

        return messageBoxes;
    }

    public override string ToString()
    {
        return "Count = " + MessageBoxes.Count;
    }

    public IEnumerator<MessageBox> GetEnumerator()
    {
        return MessageBoxes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (MessageBox msgBox in MessageBoxes)
        {
            yield return msgBox;
        }
    }
}