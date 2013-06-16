﻿using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MessageBoxChoice
{
    public string Text;
    public MessageBox Next;

    public MessageBoxChoice(string text, MessageBox next)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("MessageBoxChoice text cannot be empty.");
        if (next == null) throw new ArgumentException("MessageBoxChoice target cannot be null.");

        Text = text;
        Next = next;
    }
}

public class MessageBox : Box
{
    //TODO: timed text rendering/fading?
    //TODO: pixel-perfect line scrolling (render all text to offscreen surface, scroll a sliding window of that surface)

    //a MessageBox's height is set in terms of lines
    //its width is calculated based on font height and box padding
    public int HeightInLines { get; set; }
    public SpriteFont Font { get; set; }
    public Color FontColor { get; set; }
    public IList<MessageBoxChoice> Choices { get { return choices.AsReadOnly(); } }
    public MessageBox Next { get; set; }
    public bool HasMoreLinesToDisplay 
    { 
        get 
        {
            return (firstDisplayedLineIndex + HeightInLines) < (lines.Count + Choices.Count); 
        } 
    }

    private int firstDisplayedLineIndex = 0;
    private List<string> lines = new List<string>();
    private List<MessageBoxChoice> choices { get; set; }
    private int selectedChoiceIndex = 0;
    private static readonly Color DEFAULT_FONT_COLOR = Color.White;

    public MessageBox(int x, int y, int w, int h, SpriteFont font, ref string text) : base(x, y, w, h)
    {
        Font = font;
        FontColor = DEFAULT_FONT_COLOR;
        choices = new List<MessageBoxChoice>();

        HeightInLines = h;
        Height = HeightInLines * font.LineSpacing + (Padding * 2);
        text = WrapText(text);
    }

    //this ctor is just to allow a literal string to be passed in the ref param
    public MessageBox(int x, int y, int w, int h, SpriteFont font, string text = null) :
        this(x, y, w, h, font, ref text)
    {

    }

    public MessageBox(MessageBox template, ref string text) : 
        this(template.X, template.Y, template.Width, template.HeightInLines, template.Font, ref text)
    {

    }

    //this ctor is just to allow a literal string to be passed in the ref param
    public MessageBox(MessageBox template, string text = null) :
        this(template, ref text)
    {

    }

    //wraps the given text horizontally within a single MessageBox
    public string WrapText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

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
                //explicit page break; return what remains, to be loaded into another MessageBox
                lines.Add(sb.ToString());
                return text.Substring(processedChars);
            }
            else if (token == "\n" || Font.MeasureString(sb.ToString() + token).X > Width - (Padding + BorderWidth) * 2)
            {
                lines.Add(sb.ToString());
                sb.Clear();
            }

            //don't append leading whitespace onto the next line
            if(token != "\n" && !(sb.Length == 0 && token == " ")) sb.Append(token);
            processedChars += token.Length;
        }
        
        lines.Add(sb.ToString());
        return string.Empty;
    }

    public void AdvanceLines()
    {
        if (!HasMoreLinesToDisplay) return;

        if (firstDisplayedLineIndex + HeightInLines < lines.Count)
        {
            firstDisplayedLineIndex++;
        }
        else
        {
            //no more text, but maybe choices; scroll enough to display them all
            firstDisplayedLineIndex += Choices.Count;
        }
    }

    public void ResetLines()
    {
        firstDisplayedLineIndex = 0;
        selectedChoiceIndex = 0;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        for (int i = firstDisplayedLineIndex; i < lines.Count; i++)
        {
            int localLineNumber = i - firstDisplayedLineIndex;
            int y = Y + Padding + (Font.LineSpacing * localLineNumber);
            if ((y + Font.LineSpacing) > (Y + Height)) break;
            int x = X + Padding;
            sb.DrawString(Font, lines[i], new Vector2(x, y), Color.White);
        }

        if (!HasMoreLinesToDisplay)
        {
            //based on how many lines of text we're displaying, what local line number are the choices starting on?
            int choiceStartingLine = lines.Count - firstDisplayedLineIndex;

            //only draw the choices if we can fit *all* of them in the box from this line
            if (Choices.Count * Font.LineSpacing < Y + Height - (choiceStartingLine * Font.LineSpacing))
            {
                for (int i = 0; i < Choices.Count; i++)
                {
                    int x = X + Padding + (Padding * 3);  //indent choices a little bit
                    int y = Y + Padding + ((choiceStartingLine + i) * Font.LineSpacing);

                    Color choiceColor = Choices[i] == SelectedChoice ? Color.Yellow : Color.White;
                    sb.DrawString(Font, Choices[i].Text, new Vector2(x, y), choiceColor);
                }
            }
        }
        else
        {
            //TODO: draw a little down triangle in the bottom right corner to indicate there's more text
        }
    }

    //restricts access to Choices so the box's height can grow if needed
    //should make boxes big enough so this doesn't happen, but a box's size 
    //suddenly changing will point out the problem since it may not be obvious
    public void AddChoice(MessageBoxChoice mbc)
    {
        choices.Add(mbc);
        if(choices.Count > HeightInLines) HeightInLines = choices.Count;
    }

    public void AddChoice(string text, MessageBox next)
    {
        AddChoice(new MessageBoxChoice(text, next));
    }

    public MessageBoxChoice SelectedChoice
    {
        get
        {
            if (selectedChoiceIndex >= Choices.Count) return null;
            return Choices[selectedChoiceIndex];
        }
    }

    public void SelectNextChoice()
    {
        selectedChoiceIndex++;
        if (selectedChoiceIndex >= Choices.Count) selectedChoiceIndex = 0;  //wraparound
    }

    public void SelectPreviousChoice()
    {
        selectedChoiceIndex--;
        if (selectedChoiceIndex < 0) selectedChoiceIndex = Choices.Count - 1;  //wraparound
    }

    public override string ToString()
    {
        return string.Join("\n", lines);
    }
}