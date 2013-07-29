using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.IO.Compression;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// This file contains miscellaneous utility methods that don't really belong anywhere else.
/// </summary>
public static class Util
{
    private static Random rng = new Random();
    private static Texture2D dummyTexture;

    public static float Random()
    {
        return (float)rng.NextDouble();
    }

    //returns a random int within the given range
    //min is inclusive, max is exclusive
    public static int RandomRange(int min, int max)
    {
        return rng.Next(min, max);
    }

    public static Vector2 ToVector2(this Point p)
    {
        return new Vector2(p.X, p.Y);
    }

    public static Vector2 Position(this MouseState ms)
    {
        return new Vector2(ms.X, ms.Y);
    }

    public static Vector2 Round(this Vector2 v)
    {
        return new Vector2((float)Math.Round(v.X), (float)Math.Round(v.Y));
    }

    public static Rectangle Scale(this Rectangle rect, float scale)
    {
        return new Rectangle(rect.X, rect.Y, (int)(rect.Width * scale), (int)(rect.Height * scale));
    }

    public static bool Contains(this Rectangle rect, Vector2 pos)
    {
        return pos.X >= rect.Left && pos.X <= rect.Right &&
               pos.Y >= rect.Top && pos.Y <= rect.Bottom;
    }

    //rounds f up to the nearest multiple of m
    public static float NearestMultiple(float f, float m)
    {
        return (float)Math.Ceiling(f / m) * m;
    }

    ////get the top-left offset of innerRect such that it is centered within outerRect
    //public static Vector2 GetCenteredOffset(Rectangle outerRect, Rectangle innerRect)
    //{
    //    return outerRect.Center.ToVector2() - new Vector2(innerRect.Width / 2, innerRect.Height / 2);
    //}

    public static Color ColorFromHexString(string hexColor)
    {
        if (hexColor.StartsWith("#"))
            hexColor = hexColor.Substring(1);

        uint hex = uint.Parse(hexColor, System.Globalization.NumberStyles.HexNumber);

        Color color = Color.White;
        if (hexColor.Length == 8)
        {
            color.A = (byte)(hex >> 24);
            color.R = (byte)(hex >> 16);
            color.G = (byte)(hex >> 8);
            color.B = (byte)(hex);
        }
        else if (hexColor.Length == 6)
        {
            color.R = (byte)(hex >> 16);
            color.G = (byte)(hex >> 8);
            color.B = (byte)(hex);
        }
        else
        {
            throw new FormatException("Hex color string must be in RRGGBB or RRGGBBAA format.");
        }

        return color;
    }

    public static Texture2D ApplyColorKeyTransparency(Texture2D tex, Color trans)
    {
        Color[] pixels = new Color[tex.Height * tex.Width];
        tex.GetData<Color>(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].R == trans.R && pixels[i].G == trans.G && pixels[i].B == trans.B)
            {
                pixels[i].R = 0;
                pixels[i].G = 0;
                pixels[i].B = 0;
                pixels[i].A = 0;
            }
        }
        tex.SetData<Color>(pixels);
        return tex;
    }

    public static void DrawRectangle(SpriteBatch sb, Rectangle rect, Color color)
    {
        if (dummyTexture == null)
        {
            dummyTexture = new Texture2D(sb.GraphicsDevice, 1, 1);
            dummyTexture.SetData(new Color[] { Color.White });
        }

        sb.Draw(dummyTexture, rect, color);
    }

    public static void DrawLine(SpriteBatch sb, float width, Vector2 p1, Vector2 p2, Color color)
    {
        if (dummyTexture == null)
        {
            dummyTexture = new Texture2D(sb.GraphicsDevice, 1, 1);
            dummyTexture.SetData(new Color[] { Color.White });
        }

        float rotation = (float)Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        float length = Vector2.Distance(p1, p2);

        sb.Draw(dummyTexture, p1, null, color, rotation, Vector2.Zero, new Vector2(length, width), SpriteEffects.None, 0);
    }

    //taken from http://www.dotnetperls.com/decompress
    public static byte[] DecompressGzip(byte[] gzip)
    {
        using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memory = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return memory.ToArray();
            }
        }
    }

    public static byte[] DecompressZlib(byte[] zlib)
    {
        throw new NotImplementedException("zlib compression is not currently supported.");
    }

    public static uint[] ConvertByteArrayToLittleEndianUIntArray(byte[] bytes)
    {
        uint[] uints = new uint[bytes.Length / 4];

        for (int i = 0; i < uints.Length; i++)
        {
            uints[i] = (uint)((bytes[i * 4]) |
                              (bytes[(i * 4) + 1] << 8) |
                              (bytes[(i * 4) + 2] << 16) |
                              (bytes[(i * 4) + 3] << 24));
        }

        return uints;
    }
}