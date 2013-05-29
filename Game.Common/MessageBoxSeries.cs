using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MessageBoxSeries
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
        set
        {
            curMsgBoxIndex = MessageBoxes.IndexOf(value);
        }
    }

    public MessageBoxSeries(MessageBox template, string text = null)
    {
        TemplateMessageBox = template;
        MessageBoxes = WrapTextIntoMessageBoxes(text);
    }

    //create a template box on the fly
    public MessageBoxSeries(int x, int y, int w, int h, SpriteFont font, string text = null) :
        this(new MessageBox(x, y, w, h, font), text)
    {

    }

    //fill the whole bounds rect
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
    //mbs.MessageBoxes.AddRange(mbs.WrapText("This will span multiple MessageBoxes...\fBecause I put a line feed character in it."))
    public List<MessageBox> WrapTextIntoMessageBoxes(string text)
    {
        List<MessageBox> messageBoxes = new List<MessageBox>();

        while (text != null && text.Length > 0)
        {
            //keep passing a running ref param copy of the text, so that any page breaks span multiple boxes
            messageBoxes.Add(new MessageBox(TemplateMessageBox, ref text));
        }

        return messageBoxes;
    }

    //add a new MessageBox containing the given text to this series, and return a reference to it
    public MessageBox Add(string messageBoxText)
    {
        MessageBox mb = new MessageBox(TemplateMessageBox, messageBoxText);
        MessageBoxes.Add(mb);
        return mb;
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

    //scroll down in the current MessageBox if it has more lines to display
    //if it doesn't, jump to the target of its selected choice
    //if it doesn't have any choices, then go to the next MessageBox after it
    //if this is the last one, then don't do anything (TODO: signal to close)
    public void Advance()
    {
        if (Active.HasMoreLinesToDisplay)
        {
            Active.AdvanceLines();
        }
        else if (Active.Choices.Count > 0)
        {
            Active = Active.SelectedChoice.Next;
            Active.ResetLines();
        }
        else if (HasNextMessageBox())
        {
            curMsgBoxIndex++;
            Active.ResetLines();
        }
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
}