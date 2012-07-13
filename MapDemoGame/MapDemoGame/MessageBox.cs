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
    public Color BackgroundColor { get; set; }
    public Color FontColor { get; set; }
    public SpriteFont Font { get; set; }
    //TODO: opacity
    //TODO: gradients
    //TODO: timed text rendering/fading

    private List<string> lines = new List<string>();

    public MessageBox(int x, int y, int w, int h, SpriteFont font, ref string text)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
        Font = font;

        Padding = 4;
        BackgroundColor = Color.DarkBlue;
        FontColor = Color.White;

        if (text != null) text = WrapText(text);
    }

    public MessageBox(MessageBox template, ref string text) : 
        this(template.X, template.Y, template.Width, template.Height, template.Font, ref text)
    {

    }

    public static MessageBox CreateTemplate(int x, int y, int w, int h, SpriteFont font)
    {
        string s = null;
        return new MessageBox(x, y, w, h, font, ref s);
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
            if (token == "\n" || Font.MeasureString(sb.ToString() + token).X > Width - (Padding * 2))
            {
                lines.Add(sb.ToString());
                sb.Clear();

                if (((lines.Count + 1) * Font.LineSpacing) > Height - (Padding * 2))
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

        for (int i = 0; i < lines.Count; i++)
        {
            sb.DrawString(Font, lines[i], new Vector2(X + Padding, Y + Padding + (Font.LineSpacing * i)), Color.White);
        }
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

    public MessageBoxSeries(int x, int y, int w, int h, SpriteFont font, string text = null)
    {
        //store all the settings in the template so we don't need to keep copies
        TemplateMessageBox = MessageBox.CreateTemplate(x, y, w, h, font);

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