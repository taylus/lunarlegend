using System;
using System.Linq;
using System.Collections.Generic;

public class Layer
{
    public string Name { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public float Opacity { get; private set; }
    public Grid<Tile> Tiles { get; private set; }

    public Layer(Tiled.layer layer)
    {
        Name = layer.name;
        Width = int.Parse(layer.width);
        Height = int.Parse(layer.height);
        Opacity = layer.opacitySpecified ? (float)layer.opacity : 1.0f;

        LayerDataFormat format = DetermineLayerDataFormat(layer.data);
        Tiles = ReadTiles(layer.data, format);
    }

    private Grid<Tile> ReadTiles(Tiled.layerData layerData, LayerDataFormat format)
    {
        uint[] rawGIDs;

        //based on the format, the tile GIDs will be stored in vastly different ways
        switch (format)
        {
            case LayerDataFormat.XML:
                rawGIDs = ReadBasicRawGIDs(layerData.Items);
                break;
            case LayerDataFormat.CSV:
                rawGIDs = ReadCSVRawGIDs(layerData.Text[0]);
                break;
            case LayerDataFormat.Base64:
                rawGIDs = ReadEncodedUncompressedRawGIDs(layerData.Text[0]);
                break;
            case LayerDataFormat.Base64Gzip:
                rawGIDs = ReadEncodedGzippedRawGIDs(layerData.Text[0]);
                break;
            case LayerDataFormat.Base64Zlib:
                rawGIDs = ReadEncodedZlibedRawGIDs(layerData.Text[0]);
                break;
            default:
                throw new Exception(String.Format("Unsupported layer data format \"{0}\" for layer \"{1}\".", format.ToString(), Name));
        }

        //regardless of format, the int -> Tile mapping logic is the same (see Tile ctor)
        Tile[] tiles = Tile.FromRawGIDs(rawGIDs);
        return new Grid<Tile>(Width, Height, tiles);
    }

    /// <summary>
    /// Read the given array of XML tiles into a uint array.
    /// This is the easiest format to parse, but the most space inefficient.
    /// </summary>
    private uint[] ReadBasicRawGIDs(Tiled.layerDataTile[] xmlTiles)
    {
        return (from Tiled.layerDataTile xmlTile in xmlTiles select uint.Parse(xmlTile.gid)).ToArray();
    }

    /// <summary>
    /// Read the given string of plaintext CSV tile GIDs into a uint array.
    /// This is slightly more involved to parse, but still pretty space inefficient.
    /// </summary>
    private uint[] ReadCSVRawGIDs(string csvLayerData)
    {
        string[] tokens = csvLayerData.Trim().Split(',');
        return (from string t in tokens select uint.Parse(t)).ToArray();
    }

    /// <summary>
    /// Decode the given base64 encoded string of raw tile GIDs into a little-endian uint array.
    /// </summary>
    private uint[] ReadEncodedUncompressedRawGIDs(string encodedLayerData)
    {
        byte[] decodedLayerData = Convert.FromBase64String(encodedLayerData);
        if (decodedLayerData.Length != (Width * Height * 4))
            throw new Exception(String.Format("Layer data length does not conform to map size! Expected {0} bytes but got {1}.", Width * Height, decodedLayerData.Length));

        return Util.ConvertByteArrayToLittleEndianUIntArray(decodedLayerData);
    }

    /// <summary>
    /// Decode and decompress the given base64ed, gzip compressed string of raw tile GIDs into a little endian uint array.
    /// </summary>
    private uint[] ReadEncodedGzippedRawGIDs(string encodedCompressedLayerData)
    {
        byte[] decodedLayerData = Convert.FromBase64String(encodedCompressedLayerData);
        byte[] decompressedLayerData = Util.DecompressGzip(decodedLayerData);
        if (decompressedLayerData.Length != (Width * Height * 4))
            throw new Exception(String.Format("Layer data length does not conform to map size! Expected {0} bytes but got {1}.", Width * Height, decompressedLayerData.Length));

        return Util.ConvertByteArrayToLittleEndianUIntArray(decompressedLayerData);
    }

    /// <summary>
    /// Decode and decompress the given base64ed, zlib compressed string of raw tile GIDs into a little endian uint array.
    /// </summary>
    private uint[] ReadEncodedZlibedRawGIDs(string encodedCompressedLayerData)
    {
        byte[] decodedLayerData = Convert.FromBase64String(encodedCompressedLayerData);
        byte[] decompressedLayerData = Util.DecompressZlib(decodedLayerData);
        if (decompressedLayerData.Length != (Width * Height * 4))
            throw new Exception(String.Format("Layer data length does not conform to map size! Expected {0} bytes but got {1}.", Width * Height, decompressedLayerData.Length));

        return Util.ConvertByteArrayToLittleEndianUIntArray(decompressedLayerData);
    }

    /// <summary>
    /// Determine the means by which this layer's data is stored (encoding and compression settings).
    /// </summary>
    private LayerDataFormat DetermineLayerDataFormat(Tiled.layerData layerData)
    {
        if (!layerData.encodingSpecified && !layerData.compressionSpecified)
            return LayerDataFormat.XML;
        if (layerData.encodingSpecified && layerData.encoding == Tiled.encodingT.csv)
            return LayerDataFormat.CSV;
        if (layerData.encodingSpecified && layerData.encoding == Tiled.encodingT.base64)
        {
            if (!layerData.compressionSpecified) return LayerDataFormat.Base64;
            if (layerData.compression == Tiled.compressionT.gzip) return LayerDataFormat.Base64Gzip;
            if (layerData.compression == Tiled.compressionT.zlib) return LayerDataFormat.Base64Zlib;
        }

        throw new Exception(String.Format("Unable to determine layer data format for layer \"{0}\".", Name));
    }

    public override string ToString()
    {
        return Name;
    }
}

internal enum LayerDataFormat
{
    XML,                //individual <tile gid="x"/> elements
    CSV,                //single block of plaintext GIDs separated by commas
    Base64,             //base64 encoded, uncompressed data
    Base64Gzip,         //base64 encoded, gzip compressed data
    Base64Zlib          //base64 encoded, zlib compressed data
}

public static class LayerExtensions
{
    /// <summary>
    /// Returns the (first) layer with the given name.
    /// </summary>
    public static Layer GetByName(this List<Layer> layers, string layerName)
    {
        return (from Layer l in layers where l.Name == layerName select l).First();
    }
}