using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MenuBox : Box
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public SpriteFont Font { get; set; }
    public Color FontColor { get; set; }
    public bool IsActive { get; set; }
    public IList<MenuBoxChoice> Choices { get { return choices.AsReadOnly(); } }
    private List<MenuBoxChoice> choices { get; set; }
    private int selectedChoiceIndex = 0;
    private static readonly Color DEFAULT_FONT_COLOR = Color.White;

    private MenuBoxChoice SelectedChoice
    {
        get
        {
            if (choices == null || selectedChoiceIndex < 0 || selectedChoiceIndex >= choices.Count) return null;
            return Choices[selectedChoiceIndex];
        }
    }

    public string SelectedText
    {
        get
        {
            if (SelectedChoice == null) return null;
            return SelectedChoice.Text;
        }
    }

    public MenuBox(int x, int y, int w, int h, int columns, SpriteFont font, params string[] choices) : 
        base(x, y, w, h)
    {
        Rows = h;
        Height = Rows * font.LineSpacing + (Padding * 2);
        Columns = columns;
        Font = font;
        FontColor = DEFAULT_FONT_COLOR;
        this.choices = new List<MenuBoxChoice>();
        IsActive = true;
        PositionChoices(choices);
    }

    public MenuBox(MenuBox template, params string[] choices) :
        this(template.X, template.Y, template.Width, template.Rows, template.Columns, template.Font, choices)
    {

    }

    private void PositionChoices(string[] choices)
    {
        int columnWidth = Width / Columns;
        int rowHeight = Font.LineSpacing;
        int curChoice = 0;

        for (int x = X + Padding; x <= X + Width; x += columnWidth)
        {
            for (int y = Y + Padding; y <= Y + Height - Font.LineSpacing; y += rowHeight)
            {
                if (curChoice >= choices.Length) return;
                this.choices.Add(new MenuBoxChoice(x, y, choices[curChoice]));
                curChoice++;
            }
        }
    }

    public override void Draw(SpriteBatch sb)
    {
        if (!Visible) return;
        base.Draw(sb);

        foreach (MenuBoxChoice choice in choices)
        {
            Color choiceColor = choice == SelectedChoice && IsActive ? Color.Yellow : Color.White;
            sb.DrawString(Font, choice.Text, new Vector2(choice.X, choice.Y), choiceColor);
        }
    }

    public void SelectBelowChoice()
    {
        if (!IsActive) return;
        if ((selectedChoiceIndex + 1) % Rows != 0) selectedChoiceIndex++;
    }

    public void SelectAboveChoice()
    {
        if (!IsActive) return;
        if (selectedChoiceIndex % Rows > 0) selectedChoiceIndex--;
    }

    public void SelectLeftChoice()
    {
        if (!IsActive) return;
        if(selectedChoiceIndex - Rows >= 0) selectedChoiceIndex -= Rows;
    }

    public void SelectRightChoice()
    {
        if (!IsActive) return;
        if (selectedChoiceIndex + Rows < Choices.Count) selectedChoiceIndex += Rows;
    }

    public void ResetSelection()
    {
        if (choices != null && choices.Count > 0) selectedChoiceIndex = 0;
    }

    public override string ToString()
    {
        return string.Join("\n", choices.Select(c => c.Text));
    }
}

public class MenuBoxChoice
{
    public int X;
    public int Y;
    public string Text;
    //public object Payload;  //an item represented by this choice; e.g. the actual spell, item, etc (if applicable)

    public MenuBoxChoice(int x, int y, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("MenuBoxChoice text cannot be empty.");

        X = x;
        Y = y;
        Text = text;
    }
}