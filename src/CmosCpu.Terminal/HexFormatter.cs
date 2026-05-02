using System.Text;

namespace CmosCpu.Terminal;

public static class HexFormatter
{
    public static string FormatHexDump(byte[] data, ushort startAddress, int bytesPerRow = 16)
    {
        if (data.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        int length = data.Length;

        for (int offset = 0; offset < length; offset += bytesPerRow)
        {
            ushort addr = (ushort)(startAddress + offset);
            sb.Append($"{addr:X4}: ");

            int rowEnd = Math.Min(offset + bytesPerRow, length);
            int midPoint = bytesPerRow / 2;

            for (int i = offset; i < rowEnd; i++)
            {
                sb.Append($"{data[i]:X2}");
                bool afterMid = i - offset + 1 == midPoint;
                bool lastInRow = i == rowEnd - 1;
                if (afterMid)
                    sb.Append("  ");
                else if (!lastInRow)
                    sb.Append(' ');
            }

            if (rowEnd - offset < bytesPerRow)
            {
                int remaining = bytesPerRow - (rowEnd - offset);
                bool needsMidSpace = rowEnd - offset <= midPoint;
                for (int i = 0; i < remaining; i++)
                    sb.Append("   ");
                if (needsMidSpace)
                    sb.Append(' ');
            }

            sb.Append("  |");

            for (int i = offset; i < rowEnd; i++)
            {
                byte b = data[i];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }

            sb.AppendLine("|");
        }

        return sb.ToString();
    }

    public static string FormatStack(byte[] stackData, ushort stackBase = 0x0100)
    {
        return FormatHexDump(stackData, stackBase, 16);
    }
}
