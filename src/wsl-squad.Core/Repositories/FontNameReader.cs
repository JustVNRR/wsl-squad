using System.Buffers.Binary;
using System.Text;

namespace wsl_squad.Core.Repositories;

internal static class FontNameReader
{
    private const ushort _familyNameId = 1;
    private const ushort _typographicFamilyNameId = 16;

    public static string? ReadFamily(ReadOnlySpan<byte> data)
    {
        try
        {
            if (data.Length < 12)
            {
                return null;
            }

            uint tag = BinaryPrimitives.ReadUInt32BigEndian(data);
            bool known = tag is 0x00010000u or 0x74727565u or 0x4F54544Fu;

            if (!known)
            {
                return null;
            }

            int tableCount = BinaryPrimitives.ReadUInt16BigEndian(data[4..]);
            int nameOffset = -1;

            for (int index = 0; index < tableCount; index++)
            {
                int record = 12 + (index * 16);

                if (record + 16 > data.Length)
                {
                    return null;
                }

                if (data[record] == (byte)'n' &&
                    data[record + 1] == (byte)'a' &&
                    data[record + 2] == (byte)'m' &&
                    data[record + 3] == (byte)'e')
                {
                    nameOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(data[(record + 8)..]);

                    break;
                }
            }

            if (nameOffset < 0 || nameOffset + 6 > data.Length)
            {
                return null;
            }

            int count = BinaryPrimitives.ReadUInt16BigEndian(data[(nameOffset + 2)..]);
            int stringOffset = BinaryPrimitives.ReadUInt16BigEndian(data[(nameOffset + 4)..]);
            string? family = null;
            string? typographic = null;

            for (int index = 0; index < count; index++)
            {
                int record = nameOffset + 6 + (index * 12);

                if (record + 12 > data.Length)
                {
                    break;
                }

                ushort platform = BinaryPrimitives.ReadUInt16BigEndian(data[record..]);
                ushort encoding = BinaryPrimitives.ReadUInt16BigEndian(data[(record + 2)..]);
                ushort language = BinaryPrimitives.ReadUInt16BigEndian(data[(record + 4)..]);
                ushort nameId = BinaryPrimitives.ReadUInt16BigEndian(data[(record + 6)..]);
                int length = BinaryPrimitives.ReadUInt16BigEndian(data[(record + 8)..]);
                int offset = BinaryPrimitives.ReadUInt16BigEndian(data[(record + 10)..]);

                if (nameId is not _familyNameId and not _typographicFamilyNameId)
                {
                    continue;
                }

                if (!IsUnicodeRecord(platform, encoding, language))
                {
                    continue;
                }

                int start = nameOffset + stringOffset + offset;

                if (start < 0 || start + length > data.Length)
                {
                    continue;
                }

                string value = Encoding.BigEndianUnicode.GetString(data.Slice(start, length)).Trim();

                if (value.Length == 0)
                {
                    continue;
                }

                if (nameId == _typographicFamilyNameId)
                {
                    typographic ??= value;
                }
                else
                {
                    family ??= value;
                }
            }

            return typographic ?? family;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static bool IsUnicodeRecord(ushort platform, ushort encoding, ushort language)
    {
        return platform == 3
            ? encoding is 0 or 1 && language == 0x0409
            : platform == 0;
    }
}
