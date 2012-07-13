using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
    //TODO: opacity
    //TODO: gradients
    //TODO: timed text rendering/fading

    private List<string> lines = new List<string>();
    private SpriteFont font;

    public MessageBox(int x, int y, int w, int h, SpriteFont font, string text = null)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
        this.font = font;

        Padding = 8;
        BackgroundColor = Color.DarkBlue;
        FontColor = Color.White;

        if (text != null) WrapText(text);
    }

    //wraps the given text horizontally within a single MessageBox
    public void WrapText(string text)
    {
        //replace multiple spaces with one space
        text = Regex.Replace(text, "[ ]{2,}", " ");

        //replace carriage-return + newline with just a newline
        text = Regex.Replace(text, "\r\n", "\n");

        lines = new List<string>();
        string[] words = text.Split(' ', '\n');
        StringBuilder sb = new StringBuilder();

        foreach (string word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                lines.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                if (font.MeasureString(sb.ToString() + word).X > Width - (Padding * 2))
                {
                    lines.Add(sb.ToString().Trim());
                    sb.Clear();
                }

                sb.Append(word + ' ');
            }
        }

        lines.Add(sb.ToString().Trim());
    }

    public void Draw(SpriteBatch sb)
    {
        Util.DrawRectangle(sb, Rectangle, BackgroundColor);

        for (int i = 0; i < lines.Count; i++)
        {
            sb.DrawString(font, lines[i], new Vector2(X + Padding, Y + Padding + (font.LineSpacing * i)), Color.White);
        }
    }
}

public class MessageBoxSeries
{
    public List<MessageBox> MessageBoxes { get; set; }

    public static MessageBoxSeries WrapText(string text)
    {
        //TODO: implement me
        //do the same sort of logic as MessageBox.WrapText, but wrap across multiple 
        //MessageBoxes when there isn't enough vertical room remaining
        return null;
    }
}