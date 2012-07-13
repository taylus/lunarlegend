using System;
using System.Linq;
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
        //remove carriage return from carriage return + newline pairs
        text = Regex.Replace(text, "\r\n", "\n");

        StringBuilder sb = new StringBuilder();
        lines = new List<string>();

        string[] tokens = Regex.Split(text, @"(\s)").Where(w => w != string.Empty).ToArray();
        foreach (string token in tokens)
        {
            if (token == "\n" || font.MeasureString(sb.ToString() + token).X > Width - (Padding * 2))
            {
                lines.Add(sb.ToString());
                sb.Clear();
            }

            if(token != "\n") sb.Append(token);
        }
        
        lines.Add(sb.ToString());
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