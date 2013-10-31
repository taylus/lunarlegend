using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//the UI element that implements the power meter used for determining action strength
//stores an arbitrary number of "profiles" comprised of "slices," and a cursor that moves across them
//the slice under the cursor is be inspected to determine if an action should miss, hit, or crit
public class PowerMeter : Box
{
    //power meter bar layouts; parse from strings
    //can be of any length, but should be a number that divides evenly
    //into the PowerMeter's Width for best results
    //- is miss, = is hit, X is crit
    private const string LAYOUT_0 = "==========";
    private const string LAYOUT_1 = "========XX";
    private const string LAYOUT_2 = "====XX====";
    private const string LAYOUT_3 = "==--XX--==";
    private const string LAYOUT_4 = "=----X----=";

    //current X coord of the cursor (relative, this meter's X coord = 0); moves from 0 to Width and back again based on speed
    private float cursorPosition;
    private int curProfileIndex = 0;

    public bool IsActive { get; set; }
    public Color HitColor { get; set; }
    public Color CritColor { get; set; }
    public float CursorWidth { get; set; }
    public float LineWidth { get; set; }
    public List<PowerMeterProfile> Profiles { get; set; }
    public PowerMeterProfile CurrentProfile { get { return Profiles[curProfileIndex]; } }
    public float DamageModifier { get; set; }

    public PowerMeter(int x, int y, int w, int h) : base(x, y, w, h)
    {
        HitColor = BorderColor;
        CritColor = Color.Red;
        IsActive = true;
        LineWidth = 3.0f;
        CursorWidth = 2.0f;
        cursorPosition = 0;
        DamageModifier = 1.0f;
        Profiles = new List<PowerMeterProfile>();
        Reset();
    }

    public override void Draw(SpriteBatch sb)
    {
        if (!Visible) return;

        DrawBackground(sb);

        //draw current profile's slices
        float sliceWidth = (float)Math.Round((float)Width / CurrentProfile.Slices.Count);
        for (int i = 0; i < CurrentProfile.Slices.Count; i++)
        {
            PowerMeterResult slice = CurrentProfile.Slices[i];
            if (slice == PowerMeterResult.MISS) continue;

            float sliceX = sliceWidth * i;
            Color sliceColor = (slice == PowerMeterResult.CRIT ? CritColor : HitColor);

            float lineY = Y + (Height / 2) - (LineWidth / 2);
            Vector2 leftPoint = new Vector2(X + sliceX, lineY);
            Vector2 rightPoint = new Vector2(X + sliceX + sliceWidth, lineY);
            if (rightPoint.X > X + Width) rightPoint.X = X + Width; //handles iffyness if Width not evenly divisible by # of slices

            Util.DrawLine(sb, LineWidth, leftPoint, rightPoint, sliceColor);
        }

        //draw cursor
        float cursorX = X + cursorPosition + (CursorWidth / 2);
        Util.DrawLine(sb, CursorWidth, new Vector2(cursorX, Y), new Vector2(cursorX, Y + Height), BorderColor);

        DrawBorders(sb);
    }

    public void Update()
    {
        if (!IsActive) return;

        //move the cursor, bouncing if it reaches either end
        cursorPosition += CurrentProfile.CursorSpeed;
        if (cursorPosition >= Width)
        {
            //bounce off the right end
            cursorPosition = Width;
            CurrentProfile.CursorSpeed = -CurrentProfile.CursorSpeed;
        }
        else if (cursorPosition < 0)
        {
            //bounce off the left end
            cursorPosition = 0;
            CurrentProfile.CursorSpeed = -CurrentProfile.CursorSpeed;
        }
    }

    public bool Advance()
    {
        float prevCursorSpeed = CurrentProfile.CursorSpeed;

        if (curProfileIndex < Profiles.Count - 1)
        {
            curProfileIndex++;

            //keep the sign on the speed the same between transitions, so if
            //we were moving left before we keep moving left now, and vice versa
            if (prevCursorSpeed < 0)
            {
                CurrentProfile.CursorSpeed = -Math.Abs(CurrentProfile.CursorSpeed);
            }
            else
            {
                CurrentProfile.CursorSpeed = Math.Abs(CurrentProfile.CursorSpeed);
            }
            return true;
        }

        return false;
    }

    public bool HasNextProfile()
    {
        return curProfileIndex < Profiles.Count - 1;
    }

    public void Reset()
    {
        curProfileIndex = 0;
        DamageModifier = 1.0f;

        //randomize the starting position?
        //cursorPosition = Util.RandomRange(0, Width);
        cursorPosition = 0;
    }

    //determines if the cursor's current position is above a hit, miss, or crit
    //and multiplies the current damage modifier accordingly
    public PowerMeterResult ConfirmCursor()
    {
        int sliceWidth = Width / CurrentProfile.Slices.Count;
        int sliceIndex = (int)cursorPosition / sliceWidth;
        if (sliceIndex >= CurrentProfile.Slices.Count) sliceIndex = CurrentProfile.Slices.Count - 1;
        PowerMeterResult result =  CurrentProfile.Slices[sliceIndex];
        bool isFirstProfile = curProfileIndex <= 0;
        switch (result)
        {
            case PowerMeterResult.MISS:
                //be more forgiving for misses on later levels
                //don't make the whole attack do zero damage, just don't add any extra
                if (isFirstProfile)
                    DamageModifier = 0;
                break;
            case PowerMeterResult.HIT:
                if (!isFirstProfile)
                    DamageModifier *= 1.25f;
                break;
            case PowerMeterResult.CRIT:
                DamageModifier *= 1.5f;
                break;
        }
        return result;
    }
}

//represents a power bar layout, and the speed of the cursor moving across it
//each technique will have a different one or more of these
//the PowerMeter UI element above handles drawing and processing them
public class PowerMeterProfile
{
    public float CursorSpeed { get; set; }
    public List<PowerMeterResult> Slices { get; set; }

    public PowerMeterProfile(string layout, float cursorSpeed)
    {
        Slices = ParseSlices(layout);
        CursorSpeed = cursorSpeed;
    }

    private List<PowerMeterResult> ParseSlices(string barLayout)
    {
        List<PowerMeterResult> slices = new List<PowerMeterResult>();

        foreach (char c in barLayout)
        {
            switch (c)
            {
                case '-':
                    slices.Add(PowerMeterResult.MISS);
                    break;
                case '=':
                    slices.Add(PowerMeterResult.HIT);
                    break;
                case 'x':
                case 'X':
                    slices.Add(PowerMeterResult.CRIT);
                    break;
            }
        }

        if (slices.Count > 0)
            return slices;
        else
            throw new ArgumentException("Error parsing PowerMeter layout.");
    }
}

public enum PowerMeterResult { MISS, HIT, CRIT };