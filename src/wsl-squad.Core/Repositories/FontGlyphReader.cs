using System.Buffers.Binary;

namespace wsl_squad.Core.Repositories;

/// <summary>Lit la table cmap d'une police pour savoir si un caractère est présent.</summary>
internal static class FontGlyphReader
{
    private const uint _cmapTag = 0x636D6170; // "cmap"

    public static bool HasGlyph(string path, int codePoint)
    {
        try
        {
            return HasGlyph(File.ReadAllBytes(path), codePoint);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static bool HasIconGlyph(string path)
    {
        return HasGlyph(path, 0xF07B) || HasGlyph(path, 0xE0B0);
    }

    private static bool HasGlyph(byte[] data, int codePoint)
    {
        if (codePoint < 0 || data.Length < 12)
        {
            return false;
        }

        int tableCount = ReadU16(data, 4);
        int cmap = -1;

        for (int index = 0; index < tableCount; index++)
        {
            int record = 12 + (index * 16);

            if (record + 16 > data.Length)
            {
                break;
            }

            if (ReadU32(data, record) == _cmapTag)
            {
                cmap = (int)ReadU32(data, record + 8);
                break;
            }
        }

        if (cmap < 0 || cmap + 4 > data.Length)
        {
            return false;
        }

        int subtableCount = ReadU16(data, cmap + 2);
        int format12 = -1;
        int format4 = -1;

        for (int index = 0; index < subtableCount; index++)
        {
            int record = cmap + 4 + (index * 8);

            if (record + 8 > data.Length)
            {
                break;
            }

            int offset = cmap + (int)ReadU32(data, record + 4);

            if (offset < 0 || offset + 2 > data.Length)
            {
                continue;
            }

            int format = ReadU16(data, offset);

            if (format == 12)
            {
                format12 = offset;
            }
            else if (format == 4)
            {
                format4 = offset;
            }
        }

        if (format12 >= 0)
        {
            return Format12Contains(data, format12, codePoint);
        }

        return format4 >= 0 && Format4Contains(data, format4, codePoint);
    }

    private static bool Format12Contains(byte[] data, int offset, int codePoint)
    {
        if (offset + 16 > data.Length)
        {
            return false;
        }

        int groupCount = (int)ReadU32(data, offset + 12);
        int groups = offset + 16;

        for (int index = 0; index < groupCount; index++)
        {
            int group = groups + (index * 12);

            if (group + 12 > data.Length)
            {
                return false;
            }

            uint start = ReadU32(data, group);
            uint end = ReadU32(data, group + 4);
            uint glyph = ReadU32(data, group + 8);

            if (codePoint >= start && codePoint <= end)
            {
                return glyph + ((uint)codePoint - start) != 0;
            }
        }

        return false;
    }

    private static bool Format4Contains(byte[] data, int offset, int codePoint)
    {
        if (codePoint > 0xFFFF || offset + 14 > data.Length)
        {
            return false;
        }

        int segmentCount = ReadU16(data, offset + 6) / 2;

        if (segmentCount == 0)
        {
            return false;
        }

        int endCodes = offset + 14;
        int startCodes = endCodes + (segmentCount * 2) + 2;
        int deltas = startCodes + (segmentCount * 2);
        int rangeOffsets = deltas + (segmentCount * 2);

        if (rangeOffsets + (segmentCount * 2) > data.Length)
        {
            return false;
        }

        for (int index = 0; index < segmentCount; index++)
        {
            int end = ReadU16(data, endCodes + (index * 2));
            int start = ReadU16(data, startCodes + (index * 2));

            if (codePoint < start || codePoint > end)
            {
                continue;
            }

            int rangeOffset = ReadU16(data, rangeOffsets + (index * 2));
            short delta = (short)ReadU16(data, deltas + (index * 2));

            if (rangeOffset == 0)
            {
                return ((codePoint + delta) & 0xFFFF) != 0;
            }

            int glyphAddress = rangeOffsets + (index * 2) + rangeOffset + ((codePoint - start) * 2);

            if (glyphAddress + 2 > data.Length)
            {
                return false;
            }

            int glyph = ReadU16(data, glyphAddress);

            return glyph != 0 && ((glyph + delta) & 0xFFFF) != 0;
        }

        return false;
    }

    private static ushort ReadU16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
    }

    private static uint ReadU32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
    }
}
