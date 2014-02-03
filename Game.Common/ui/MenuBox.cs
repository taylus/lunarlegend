using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//allows for a variable to refer to a menu without knowing its generic type
//useful in a situation when you want to track the "active" menu among several of different types
public abstract class BaseMenuBox : Box
{
    public BaseMenuBox(int x, int y, int w, int h) : base(x, y, w, h) { }
    public bool IsActive { get; set; }
    public abstract void SelectBelowChoice();
    public abstract void SelectAboveChoice();
    public abstract void SelectLeftChoice();
    public abstract void SelectRightChoice();
    public abstract void ResetSelection();
    public abstract void RemoveSelection();
}

//represents a graphical menu box with a number of choices, where each choice is an object T
//if T derives from UIElement, it will be drawn
public class MenuBox<T> : BaseMenuBox
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public SpriteFont Font { get; set; }
    public Color FontColor { get; set; }
    public IList<MenuBoxChoice<T>> Choices { get { return choices.AsReadOnly(); } }
    private List<MenuBoxChoice<T>> choices;
    private int selectedChoiceIndex = 0;
    private static readonly Color DEFAULT_FONT_COLOR = Color.White;

    private MenuBoxChoice<T> SelectedChoice
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

    public T SelectedValue
    {
        get
        {
            if (SelectedChoice == null) return default(T);
            return SelectedChoice.Payload;
        }
    }

    public MenuBox(int x, int y, int w, int h, int columns, SpriteFont font, params T[] choices) : 
        base(x, y, w, h)
    {
        Rows = h;
        Columns = columns;
        Font = font;
        FontColor = DEFAULT_FONT_COLOR;
        Height = CalculateMinimumBoxHeight(choices);
        PositionChoices(choices);
        IsActive = true;
    }

    public MenuBox(MenuBox<T> template, params T[] choices) :
        this(template.X, template.Y, template.Width, template.Rows, template.Columns, template.Font, choices)
    {

    }

    private int CalculateMinimumBoxHeight(T[] choices)
    {
        if (typeof(UIElement).IsAssignableFrom(typeof(T)))
        {
            return Rows * (choices as UIElement[]).Max(c => c.Height) + Font.LineSpacing + (Padding * 2);
        }
        else
        {
            return Rows * Font.LineSpacing + (Padding * 2);
        }
    }

    private void PositionChoices(T[] choices)
    {
        this.choices = new List<MenuBoxChoice<T>>();
        int columnWidth = Width / Columns;
        int curChoice = 0;

        int rowHeight = Font.LineSpacing;
        if (typeof(UIElement).IsAssignableFrom(typeof(T)))
        {
            //add height of graphical choices (use the maximum)
            rowHeight += (choices as UIElement[]).Max(c => c.Height);
        }

        for (int x = X + Padding; x <= X + Width; x += columnWidth)
        {
            for (int y = Y + Padding; y <= Y + Height - Font.LineSpacing; y += rowHeight)
            {
                if (curChoice >= choices.Length) return;
                this.choices.Add(new MenuBoxChoice<T>(x, y, choices[curChoice]));
                if (typeof(UIElement).IsAssignableFrom(typeof(T)))
                {
                    (choices[curChoice] as UIElement).MoveTo(x, y);
                }
                curChoice++;
            }
        }
    }

    private void RepositionChoices()
    {
        if (choices.Count <= 0) return;
        PositionChoices(choices.Select(c => c.Payload).ToArray());
    }

    public override void Draw(SpriteBatch sb)
    {
        if (!Visible) return;
        base.Draw(sb);

        foreach (MenuBoxChoice<T> choice in choices)
        {
            Color choiceColor;
            if (!IsActive || !choice.Enabled) choiceColor = Color.Gray;
            else choiceColor = (choice == SelectedChoice) ? Color.Yellow : Color.White;

            if (typeof(UIElement).IsAssignableFrom(typeof(T)))
            {
                //TODO: sprite effects on selected item?
                UIElement uie = choice.Payload as UIElement;
                uie.Draw(sb);

                //draw the choice's text centered under its image
                Vector2 textSize = Font.MeasureString(choice.Text);
                Vector2 textPosition = new Vector2(uie.X + (uie.Width / 2) - (textSize.X / 2), 
                                                   uie.Y + uie.Height + (Padding / 2));
                sb.DrawString(Font, choice.Text, textPosition.Round(), choiceColor);
            }
            else
            {
                sb.DrawString(Font, choice.Text, new Vector2(choice.X, choice.Y), choiceColor);
            }
        }
    }

    //TODO: skip over disabled choices
    public override void SelectBelowChoice()
    {
        if (!IsActive) return;
        if ((selectedChoiceIndex + 1) < Choices.Count && (selectedChoiceIndex + 1) % Rows != 0) selectedChoiceIndex++;
    }

    public override void SelectAboveChoice()
    {
        if (!IsActive) return;
        if (selectedChoiceIndex % Rows > 0) selectedChoiceIndex--;
    }

    public override void SelectLeftChoice()
    {
        if (!IsActive) return;
        if(selectedChoiceIndex - Rows >= 0) selectedChoiceIndex -= Rows;
    }

    public override void SelectRightChoice()
    {
        if (!IsActive) return;
        if (selectedChoiceIndex + Rows < Choices.Count) selectedChoiceIndex += Rows;
    }

    public override void ResetSelection()
    {
        if (choices != null && choices.Count > 0) selectedChoiceIndex = 0;
    }

    public override void RemoveSelection()
    {
        choices.Remove(SelectedChoice);
        RepositionChoices();
        ResetSelection();
    }

    public override string ToString()
    {
        return string.Join("\n", choices.Select(c => c.Text));
    }
}

public class MenuBoxChoice<T>
{
    public int X;
    public int Y;
    public T Payload;
    public bool Enabled;
    public string Text { get { return Payload == null? "null" : Payload.ToString(); } }

    public MenuBoxChoice(int x, int y, T payload, bool enabled = true)
    {
        if (payload == null) throw new ArgumentException("MenuBoxChoice payload cannot be null.");

        X = x;
        Y = y;
        Payload = payload;
        Enabled = enabled;
    }
}