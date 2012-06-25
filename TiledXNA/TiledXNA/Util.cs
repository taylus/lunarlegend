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
    private static Texture2D dummyTexture;

    public static Vector2 ToVector2(this Point p)
    {
        return new Vector2(p.X, p.Y);
    }

    public static Vector2 Position(this MouseState ms)
    {
        return new Vector2(ms.X, ms.Y);
    }

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