using System;
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

public class MessageBox
{
    //TODO: gradient backgrounds?
    //TODO: timed text rendering/fading?

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int HeightInLines { get; set; }
    public int Height { get { return HeightInLines * Font.LineSpacing + (Padding * 2); } }
    public Rectangle Rectangle { get { return new Rectangle(X, Y, Width, Height); } }
    public int Padding { get; set; }
    public int BorderWidth { get; set; }
    public float Opacity { get; set; }
    public Color BorderColor { get; set; }
    public Color BackgroundColor { get; set; }
    public Color FontColor { get; set; }
    public SpriteFont Font { get; set; }
    public IList<MessageBoxChoice> Choices { get { return choices.AsReadOnly(); } }
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
    private const int TEXT_LEFT_PADDING = 4;
    private const int DEFAULT_PADDING = 8;
    private const int DEFAULT_BORDER_WIDTH = 2;
    private static readonly Color DEFAULT_BORDER_COLOR = Color.LightSteelBlue;
    private static readonly Color DEFAULT_FONT_COLOR = Color.White;
    private const float DEFAULT_OPACITY = 0.65f;

    public MessageBox(int x, int y, int w, int h, SpriteFont font, ref string text)
    {
        Padding = DEFAULT_PADDING;
        BorderWidth = DEFAULT_BORDER_WIDTH;
        BorderColor = DEFAULT_BORDER_COLOR;
        FontColor = DEFAULT_FONT_COLOR;
        Opacity = DEFAULT_OPACITY;
        BackgroundColor = Color.Lerp(Color.Transparent, Color.DarkBlue, MathHelper.Clamp(Opacity, 0, 1));
        choices = new List<MessageBoxChoice>();

        X = x;
        Y = y;
        Width = w;
        HeightInLines = h;
        Font = font;
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
            else if (token == "\n" || Font.MeasureString(sb.ToString() + token).X > Width - ((Padding + BorderWidth) * 2) - TEXT_LEFT_PADDING)
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
        if (firstDisplayedLineIndex + HeightInLines < lines.Count)
        {
            firstDisplayedLineIndex++;
        }
        else
        {
            //no more text, but maybe choices; scroll enough to display them all
            //TODO: how to handle when there are too many choices to fit in the box?
            firstDisplayedLineIndex += Choices.Count;
        }
    }

    public void ResetLines()
    {
        firstDisplayedLineIndex = 0;
    }

    public void Draw(SpriteBatch sb)
    {
        Util.DrawRectangle(sb, Rectangle, BackgroundColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X, Y), new Vector2(X + Width, Y), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X + Width, Y), new Vector2(X + Width, Y + Height), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X + Width, Y + Height), new Vector2(X, Y + Height), BorderColor);
        Util.DrawLine(sb, BorderWidth, new Vector2(X, Y + Height), new Vector2(X, Y), BorderColor);

        for (int i = firstDisplayedLineIndex; i < lines.Count; i++)
        {
            int localLineNumber = i - firstDisplayedLineIndex;
            int y = Y + Padding + (Font.LineSpacing * localLineNumber);
            if ((y + Font.LineSpacing) > (Y + Height)) break;
            int x = X + Padding + TEXT_LEFT_PADDING;
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
                    int x = X + Padding + (TEXT_LEFT_PADDING * 3);  //indent choices a little bit
                    int y = Y + Padding + ((choiceStartingLine + i) * Font.LineSpacing);

                    Color choiceColor = Choices[i] == SelectedChoice ? Color.Yellow : Color.White;
                    sb.DrawString(Font, Choices[i].Text, new Vector2(x, y), choiceColor);
                }
            }
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