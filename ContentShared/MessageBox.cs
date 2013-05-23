﻿using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public struct MessageBoxChoice
{
    public string Text;
    public MessageBox Next;

    public MessageBoxChoice(string text, MessageBox next)
    {
        Text = text;
        Next = next;
    }
}

public class MessageBox
{
    //TODO: gradient backgrounds?
    //TODO: timed text rendering/fading?

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
    public List<MessageBoxChoice> Choices { get; set; }

    private List<string> lines = new List<string>();
    private const int TEXT_LEFT_PADDING = 4;

    public MessageBox(int x, int y, int w, int h, SpriteFont font, ref string text)
    {
        X = x;
        Y = y;
        Width = w;
        Height = h;
        Font = font;

        Padding = 8;
        BorderWidth = 2;
        Opacity = 0.65f;
        BackgroundColor = Color.Lerp(Color.Transparent, Color.DarkBlue, MathHelper.Clamp(Opacity, 0, 1));
        //BorderColor = Color.Lerp(Color.Transparent, Color.LightSteelBlue, MathHelper.Clamp(Opacity, 0, 1));
        BorderColor = Color.LightSteelBlue;
        FontColor = Color.White;

        Choices = new List<MessageBoxChoice>();
        if (text != null) text = WrapText(text);
    }

    public MessageBox(int x, int y, int w, int h, SpriteFont font, string text = null) :
        this(x, y, w, h, font, ref text)
    {

    }

    public MessageBox(MessageBox template, ref string text) : 
        this(template.X, template.Y, template.Width, template.Height, template.Font, ref text)
    {

    }

    public MessageBox(MessageBox template, string text = null) :
        this(template, ref text)
    {

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
            if (token == "\f")
            {
                //explicit page break, return what remains
                lines.Add(sb.ToString());
                return text.Substring(processedChars);
            }
            else if (token == "\n" || Font.MeasureString(sb.ToString() + token).X > Width - ((Padding + BorderWidth) * 2) - TEXT_LEFT_PADDING)
            {
                lines.Add(sb.ToString());
                sb.Clear();

                if (((lines.Count + 1) * Font.LineSpacing) > Height - ((Padding + BorderWidth) * 2))
                {
                    //another line won't fit, return what remains
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

        for (int i = 0; i < lines.Count; i++)
        {
            sb.DrawString(Font, lines[i], new Vector2(X + Padding + TEXT_LEFT_PADDING, Y + Padding + (Font.LineSpacing * i)), Color.White);
        }
    }

    public override string ToString()
    {
        return string.Join("\n", lines);
    }
}

public class MessageBoxSeries : IEnumerable<MessageBox>
{
    private int curMsgBoxIndex = 0;

    public MessageBox TemplateMessageBox { get; set; }
    public List<MessageBox> MessageBoxes { get; set; }
    public int Count { get { return MessageBoxes != null? MessageBoxes.Count : 0; } }
    public MessageBox this[int i] { get { return MessageBoxes[i]; } }
    public MessageBox Active 
    { 
        get 
        {
            if (curMsgBoxIndex >= MessageBoxes.Count) return null;
            return this[curMsgBoxIndex]; 
        } 
    }

    public MessageBoxSeries(MessageBox template, string text = null)
    {
        TemplateMessageBox = template;

        if (text == null)
            MessageBoxes = new List<MessageBox>(new[] { TemplateMessageBox });
        else
            MessageBoxes = WrapText(text);
    }

    public MessageBoxSeries(int x, int y, int w, int h, SpriteFont font, string text = null) :
        this(new MessageBox(x, y, w, h, font), text)
    {

    }

    public MessageBoxSeries(Rectangle bounds, SpriteFont font, string text = null)
        : this(bounds.X, bounds.Y, bounds.Width, bounds.Height, font, text)
    {

    }

    //center the box horizontally within the bounds rect
    public MessageBoxSeries(Rectangle bounds, int y, int w, int h, SpriteFont font, string text = null)
        : this(bounds.Center.X - w/2, y, w, h, font, text)
    {
        
    }

    //center the box horizontally and vertically within the bounds rect
    public MessageBoxSeries(Rectangle bounds, int w, int h, SpriteFont font, string text = null)
        : this(bounds.Center.X - (w / 2), bounds.Center.Y - (h / 2), w, h, font, text)
    {

    }

    //factory-like method to wrap a block of text into possibly several MessageBoxes, all using this one's style template
    //useful for adding a dynamic number of message boxes from a block of text at runtime, e.g.
    //mbs.MessageBoxes.AddRange(mbs.WrapText("Long enough string to span more than one MessageBox..."))
    public List<MessageBox> WrapText(string text)
    {
        List<MessageBox> messageBoxes = new List<MessageBox>();

        while (text.Length > 0)
        {
            messageBoxes.Add(new MessageBox(TemplateMessageBox, ref text));
        }

        return messageBoxes;
    }

    //draw the currently active MessageBox
    public void Draw(SpriteBatch sb)
    {
        if (Active != null)
            Active.Draw(sb);
    }

    //reset the currently active MessageBox to the first one in this series
    public void Reset()
    {
        curMsgBoxIndex = 0;
    }

    //advance to the next MessageBox (if one exists)
    public void Advance()
    {
        if(HasNextMessageBox()) curMsgBoxIndex++;
    }

    //is there a next MessageBox in this series?
    public bool HasNextMessageBox()
    {
        return curMsgBoxIndex < (Count - 1);
    }

    public override string ToString()
    {
        return "Count = " + MessageBoxes.Count;
    }

    public IEnumerator<MessageBox> GetEnumerator()
    {
        return MessageBoxes.GetEnumerator();
    }

    //allows this class to be foreach'd over
    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (MessageBox msgBox in MessageBoxes)
        {
            yield return msgBox;
        }
    }
}