using System;

public struct Tile
{
    public const uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
    public const uint FLIPPED_VERTICALLY_FLAG = 0x40000000;
    public const uint FLIPPED_DIAGONALLY_FLAG = 0x20000000;

    public bool FlippedHorizontally;
    public bool FlippedVertically;
    public bool FlippedDiagonally;
    public uint GID;

    public Tile(uint rawGID)
    {
        //a "raw GID" still has the flipped flags in its most significant bits
        //read those out and clear them to get the tile's "actual" GID
        //see the psuedocode on https://github.com/bjorn/tiled/wiki/TMX-Map-Format
        FlippedHorizontally = ((rawGID & Tile.FLIPPED_HORIZONTALLY_FLAG) != 0);
        FlippedVertically = ((rawGID & Tile.FLIPPED_VERTICALLY_FLAG) != 0);
        FlippedDiagonally = ((rawGID & Tile.FLIPPED_DIAGONALLY_FLAG) != 0);

        rawGID &= ~(Tile.FLIPPED_HORIZONTALLY_FLAG |
                    Tile.FLIPPED_VERTICALLY_FLAG |
                    Tile.FLIPPED_DIAGONALLY_FLAG);

        GID = rawGID;
    }

    public static Tile[] FromRawGIDs(uint[] rawGIDs)
    {
        Tile[] tiles = new Tile[rawGIDs.Length];
        for (int i = 0; i < rawGIDs.Length; i++)
            tiles[i] = new Tile(rawGIDs[i]);

        return tiles;
    }

    public override string ToString()
    {
        return String.Format("H:{0} V:{1} D:{2} G:{3}", Convert.ToInt32(FlippedHorizontally),
            Convert.ToInt32(FlippedVertically), Convert.ToInt32(FlippedDiagonally), GID);
    }
}