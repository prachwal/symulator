using System.Globalization;
using System.Text.RegularExpressions;
using CmosCpu.Core;
using NLog;

namespace CmosCpu.Assembler;

public class SimpleAssembler : IAssembler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, byte> _opcodeMap = new()
    {
        ["NOP"] = (byte)Opcode.NOP,
        ["LDA"] = 0, // resolved by operand format
        ["STA"] = (byte)Opcode.STA_ABS,
        ["ADD"] = 0,
        ["SUB"] = 0,
        ["JMP"] = (byte)Opcode.JMP,
        ["JZ"] = (byte)Opcode.JZ,
        ["JNZ"] = (byte)Opcode.JNZ,
        ["OUT"] = (byte)Opcode.OUT,
        ["IN"] = (byte)Opcode.IN,
        ["CLI"] = (byte)Opcode.CLI,
        ["SEI"] = (byte)Opcode.SEI,
        ["PUSH_A"] = (byte)Opcode.PUSH_A,
        ["POP_A"] = (byte)Opcode.POP_A,
        ["CALL"] = (byte)Opcode.CALL,
        ["RET"] = (byte)Opcode.RET,
        ["HLT"] = (byte)Opcode.HLT,
    };

    public (byte[] binary, IReadOnlyList<string> errors) Assemble(string source, ushort origin = 0x8000)
    {
        var errors = new List<string>();
        var output = new List<byte>();
        var labels = new Dictionary<string, ushort>();
        var pendingLabels = new Dictionary<string, List<int>>();
        ushort address = origin;
        string[] lines = source.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string rawLine = lines[i].Trim();
            int commentIdx = rawLine.IndexOf(';');
            if (commentIdx >= 0)
                rawLine = rawLine[..commentIdx].Trim();

            if (string.IsNullOrEmpty(rawLine))
                continue;

            if (rawLine.StartsWith(".org", StringComparison.OrdinalIgnoreCase))
            {
                var parts = rawLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && TryParseNumber(parts[1], out ushort orgAddr))
                {
                    address = orgAddr;
                }
                else
                {
                    errors.Add($"Line {i + 1}: Invalid .org directive");
                }
                continue;
            }

            if (rawLine.EndsWith(':'))
            {
                string label = rawLine.TrimEnd(':');
                labels[label] = address;
                continue;
            }

            if (rawLine.Contains(':'))
            {
                int colonIdx = rawLine.IndexOf(':');
                string label = rawLine[..colonIdx].Trim();
                labels[label] = address;
                rawLine = rawLine[(colonIdx + 1)..].Trim();
                if (string.IsNullOrEmpty(rawLine))
                    continue;
            }

            var parts2 = rawLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts2.Length == 0) continue;

            string mnemonic = parts2[0].ToUpperInvariant();
            string? operand = parts2.Length > 1 ? parts2[1] : null;

            if (!_opcodeMap.ContainsKey(mnemonic))
            {
                errors.Add($"Line {i + 1}: Unknown mnemonic '{mnemonic}'");
                continue;
            }

            switch (mnemonic)
            {
                case "NOP":
                case "CLI":
                case "SEI":
                case "PUSH_A":
                case "POP_A":
                case "RET":
                case "HLT":
                    output.Add(_opcodeMap[mnemonic]);
                    address++;
                    break;

                case "LDA":
                    if (operand != null && operand.StartsWith('#'))
                    {
                        output.Add((byte)Opcode.LDA_IMM);
                        if (TryParseNumber(operand[1..], out byte immVal))
                        {
                            output.Add(immVal);
                            address += 2;
                        }
                        else
                        {
                            errors.Add($"Line {i + 1}: Invalid immediate value '{operand}'");
                        }
                    }
                    else if (operand != null)
                    {
                        output.Add((byte)Opcode.LDA_ABS);
                        EmitAddressOperand(operand, labels, ref address, output, errors, i, pendingLabels);
                    }
                    else
                    {
                        errors.Add($"Line {i + 1}: LDA requires operand");
                    }
                    break;

                case "STA":
                    output.Add((byte)Opcode.STA_ABS);
                    EmitAddressOperand(operand!, labels, ref address, output, errors, i, pendingLabels);
                    break;

                case "ADD":
                    if (operand != null && operand.StartsWith('#'))
                    {
                        output.Add((byte)Opcode.ADD_IMM);
                        if (TryParseNumber(operand[1..], out byte immVal))
                        {
                            output.Add(immVal);
                            address += 2;
                        }
                        else
                        {
                            errors.Add($"Line {i + 1}: Invalid immediate value '{operand}'");
                        }
                    }
                    else
                    {
                        errors.Add($"Line {i + 1}: ADD requires immediate operand (#value)");
                    }
                    break;

                case "SUB":
                    if (operand != null && operand.StartsWith('#'))
                    {
                        output.Add((byte)Opcode.SUB_IMM);
                        if (TryParseNumber(operand[1..], out byte immVal))
                        {
                            output.Add(immVal);
                            address += 2;
                        }
                        else
                        {
                            errors.Add($"Line {i + 1}: Invalid immediate value '{operand}'");
                        }
                    }
                    else
                    {
                        errors.Add($"Line {i + 1}: SUB requires immediate operand (#value)");
                    }
                    break;

                case "JMP":
                case "JZ":
                case "JNZ":
                case "CALL":
                    output.Add(_opcodeMap[mnemonic]);
                    EmitAddressOperand(operand!, labels, ref address, output, errors, i, pendingLabels);
                    break;

                case "OUT":
                    output.Add((byte)Opcode.OUT);
                    if (operand != null && TryParseNumber(operand, out byte portVal))
                    {
                        output.Add(portVal);
                        address += 2;
                    }
                    else
                    {
                        errors.Add($"Line {i + 1}: OUT requires port number");
                    }
                    break;

                case "IN":
                    output.Add((byte)Opcode.IN);
                    if (operand != null && TryParseNumber(operand, out byte inPortVal))
                    {
                        output.Add(inPortVal);
                        address += 2;
                    }
                    else
                    {
                        errors.Add($"Line {i + 1}: IN requires port number");
                    }
                    break;

                default:
                    errors.Add($"Line {i + 1}: Unsupported mnemonic '{mnemonic}'");
                    break;
            }
        }

        foreach (var pending in pendingLabels)
        {
            if (!labels.TryGetValue(pending.Key, out ushort labelAddr))
            {
                errors.Add($"Undefined label '{pending.Key}' referenced at positions {string.Join(", ", pending.Value.Select(p => $"0x{p:X4}"))}");
            }
            else
            {
                foreach (int pos in pending.Value)
                {
                    if (pos >= 0 && pos + 1 < output.Count)
                    {
                        output[pos] = (byte)(labelAddr & 0xFF);
                        output[pos + 1] = (byte)((labelAddr >> 8) & 0xFF);
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            foreach (var err in errors)
                Logger.Error(err);
        }

        return (output.ToArray(), errors.AsReadOnly());
    }

    private void EmitAddressOperand(string operand, Dictionary<string, ushort> labels, ref ushort address, List<byte> output, List<string> errors, int lineIndex, Dictionary<string, List<int>> pendingLabels)
    {
        if (TryParseNumber(operand, out ushort addrVal))
        {
            output.Add((byte)(addrVal & 0xFF));
            output.Add((byte)((addrVal >> 8) & 0xFF));
            address += 3;
        }
        else if (labels.TryGetValue(operand, out ushort labelAddr))
        {
            output.Add((byte)(labelAddr & 0xFF));
            output.Add((byte)((labelAddr >> 8) & 0xFF));
            address += 3;
        }
        else
        {
            output.Add(0);
            output.Add(0);
            address += 3;
            if (!pendingLabels.ContainsKey(operand))
                pendingLabels[operand] = new List<int>();
            pendingLabels[operand].Add(output.Count - 2);
        }
    }

    private static bool TryParseNumber(string s, out byte result)
    {
        result = 0;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return byte.TryParse(s[2..], NumberStyles.HexNumber, null, out result);
        }
        return byte.TryParse(s, out result);
    }

    private static bool TryParseNumber(string s, out ushort result)
    {
        result = 0;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ushort.TryParse(s[2..], NumberStyles.HexNumber, null, out result);
        }
        return ushort.TryParse(s, out result);
    }
}
