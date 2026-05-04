using System.Globalization;
using System.Text;

namespace Symulator.Application.Assembly;

public sealed class PseudoCpuAssembler
{
    // Mnemonic -> (opcode, operandCount, description)
    private static readonly Dictionary<string, (byte Opcode, int OperandBytes, string Format)> Mnemonics = new()
    {
        ["NOP"]        = (0x00, 0, "implied"),
        ["LDA"]        = (0x01, 1, "#value"),
        ["STA"]        = (0x02, 2, "address"),
        ["JMP"]        = (0x03, 2, "address"),
        ["XOR"]        = (0x04, 1, "#value"),
        ["DEC"]        = (0x05, 2, "address"),
        ["JNZ"]        = (0x06, 2, "address"),
        ["HLT"]        = (0x07, 0, "implied"),
        ["BRK"]        = (0x07, 0, "implied"),
        ["LDA_ABS"]    = (0x08, 2, "address"),
        ["STA_ZP"]     = (0x09, 1, "zp"),
        ["LDA_ZP"]     = (0x0A, 1, "zp"),
        ["AND"]        = (0x0B, 1, "#value"),
        ["BNE"]        = (0x0C, 1, "label"),
        ["JSR"]        = (0x0D, 2, "address"),
        ["RTS"]        = (0x0E, 0, "implied"),
        ["ADD"]        = (0x0F, 1, "#value"),
        ["SUB"]        = (0x10, 1, "#value"),
    };

    // Predefined symbols for MinimalBlink
    private static readonly Dictionary<string, ushort> PredefinedSymbols = new()
    {
        ["LED_PORT"]       = 0xFF00,
        ["LCD_CMD"]        = 0xFE00,
        ["LCD_DATA"]       = 0xFE01,
        ["I2C_CONTROL"]    = 0xFE30,
        ["I2C_ADDRESS"]    = 0xFE31,
        ["I2C_DATA"]       = 0xFE32,
        ["I2C_STATUS"]     = 0xFE33,
        ["UART_DATA"]      = 0xFE40,
        ["UART_STATUS"]    = 0xFE41,
        ["UART_CONTROL"]   = 0xFE42,
    };

    public AssemblyResult Assemble(string programId, string sourceText)
    {
        var lines = sourceText.Split('\n');
        var diags = new List<AssemblyDiagnostic>();
        var listing = new List<AssemblyListingLine>();
        var symbols = new Dictionary<string, ushort>(PredefinedSymbols);
        var forwardRefs = new Dictionary<string, List<(int Pass, int ByteIndex, int OperandIndex)>>();
        var bytes = new List<byte>();
        ushort origin = 0x0100;
        ushort startAddress = 0x0100;
        bool foundStart = false;

        // Pass 1: collect labels, compute addresses
        var labelAddresses = new Dictionary<string, ushort>();
        var labelLines = new Dictionary<string, int>();
        ushort currentAddr = origin;

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            string raw = lines[lineNum];
            string text = raw.TrimEnd('\r', '\n');
            int sourceLine = lineNum + 1;

            // Strip comments
            string code = text.Contains(';') ? text[..text.IndexOf(';')] : text;
            code = code.Trim();

            if (string.IsNullOrEmpty(code))
            {
                listing.Add(new AssemblyListingLine
                {
                    SourceLineNumber = sourceLine,
                    SourceText = text
                });
                continue;
            }

            // Check for label
            string? label = null;
            string rest = code;
            if (code.Contains(':'))
            {
                int colonIdx = code.IndexOf(':');
                string possibleLabel = code[..colonIdx].Trim();
                if (PossibleLabel(possibleLabel))
                {
                    label = possibleLabel;
                    rest = code[(colonIdx + 1)..].Trim();

                    if (labelAddresses.ContainsKey(label))
                    {
                        diags.Add(new AssemblyDiagnostic
                        {
                            Severity = "Error",
                            LineNumber = sourceLine,
                            Message = $"Duplicate label '{label}'"
                        });
                        continue;
                    }
                    labelAddresses[label] = currentAddr;
                    labelLines[label] = sourceLine;
                }
            }

            if (string.IsNullOrEmpty(rest))
            {
                listing.Add(new AssemblyListingLine
                {
                    SourceLineNumber = sourceLine,
                    SourceText = text,
                    Label = label,
                    Address = currentAddr
                });
                continue;
            }

            // Parse directive or instruction
            string firstToken = rest.Split(' ', '\t')[0].ToUpperInvariant();
            string operandPart = rest[firstToken.Length..].Trim();

            // Generate listing line
            var lineObj = new AssemblyListingLine
            {
                SourceLineNumber = sourceLine,
                SourceText = text,
                Label = label,
                Address = currentAddr
            };

            // Handle `SYMBOL = value` (without .equ)
            if (firstToken.Length > 0 && char.IsLetter(firstToken[0]) &&
                rest.Contains('=') && !Mnemonics.ContainsKey(firstToken))
            {
                var eqIdx = rest.IndexOf('=');
                string symName = rest[..eqIdx].Trim();
                string symValStr = rest[(eqIdx + 1)..].Trim();
                if (PossibleLabel(symName) && ParseWord(symValStr, symbols, sourceLine, diags, out ushort eqVal))
                {
                    if (symbols.ContainsKey(symName))
                        diags.Add(new AssemblyDiagnostic { Severity = "Warning", LineNumber = sourceLine, Message = $"Redefining symbol '{symName}'" });
                    symbols[symName] = eqVal;
                    lineObj.Mnemonic = ".equ";
                    lineObj.Operand = rest;
                    listing.Add(lineObj);
                    continue; // skip to next line
                }
            }

            switch (firstToken)
            {
                case ".ORG":
                {
                    if (ParseWord(operandPart, symbols, sourceLine, diags, out ushort orgAddr))
                    {
                        origin = orgAddr;
                        currentAddr = orgAddr;
                    }
                    lineObj.Mnemonic = ".org";
                    lineObj.Operand = operandPart;
                    listing.Add(lineObj);
                    break;
                }

                case ".EQU":
                {
                    var eqParts = operandPart.Split(',');
                    if (eqParts.Length == 2)
                    {
                        string symName = eqParts[0].Trim();
                        if (ParseWord(eqParts[1].Trim(), symbols, sourceLine, diags, out ushort symVal))
                        {
                            if (symbols.ContainsKey(symName))
                                diags.Add(new AssemblyDiagnostic { Severity = "Warning", LineNumber = sourceLine, Message = $"Redefining symbol '{symName}'" });
                            symbols[symName] = symVal;
                        }
                    }
                    lineObj.Mnemonic = ".equ";
                    lineObj.Operand = operandPart;
                    listing.Add(lineObj);
                    break;
                }

                case ".BYTE":
                {
                    var b = ParseByteList(operandPart, symbols, sourceLine, diags);
                    foreach (var val in b)
                    {
                        bytes.Add(val);
                        currentAddr++;
                    }
                    lineObj.Mnemonic = ".byte";
                    lineObj.Operand = operandPart;
                    lineObj.Bytes = b.ToArray();
                    listing.Add(lineObj);
                    break;
                }

                case ".TEXT":
                {
                    var t = ParseText(operandPart, sourceLine, diags);
                    foreach (var val in t)
                    {
                        bytes.Add(val);
                        currentAddr++;
                    }
                    lineObj.Mnemonic = ".text";
                    lineObj.Operand = operandPart;
                    lineObj.Bytes = t.ToArray();
                    listing.Add(lineObj);
                    break;
                }

                default:
                {
                    // Instruction
                    if (Mnemonics.TryGetValue(firstToken, out var info))
                    {
                        // Auto-detect LDA immediate vs absolute
                        if (firstToken == "LDA")
                        {
                            if (operandPart.StartsWith("#"))
                            {
                                // LDA #value → opcode 0x01
                            }
                            else
                            {
                                // LDA address → opcode 0x08 (LDA_ABS)
                                info = (0x08, 2, "address");
                            }
                        }
                        // Similarly for STA vs STA_ZP
                        if (firstToken == "STA" && info.OperandBytes == 2 && !operandPart.StartsWith("$"))
                        {
                            // Try to determine if zero-page (address < $0100)
                            // For now, STA is always absolute (2 bytes)
                        }

                        byte opcode = info.Opcode;
                        int operandBytes = info.OperandBytes;

                        bytes.Add(opcode);
                        int opcodeAddr = currentAddr;
                        currentAddr++;

                        lineObj.Mnemonic = firstToken;
                        lineObj.Operand = operandPart;

                        if (operandBytes == 1)
                        {
                            byte val = 0;
                            bool isImmediate = operandPart.StartsWith("#");
                            string opVal = isImmediate ? operandPart[1..].Trim() : operandPart.Trim();

                            // Char literal
                            if (opVal.StartsWith('\'') && opVal.EndsWith('\''))
                            {
                                val = (byte)opVal[1];
                            }
                            else if (opcode != 0x0C && ParseByte(opVal, symbols, sourceLine, diags, out byte bval))
                            {
                                // For non-BNE instructions, parse as byte
                                val = bval;
                            }
                            // For BNE, val stays 0 — resolved via forwardRefs pass 2

                            // BNE has relative offset — resolved later in dedicated BNE pass
                            // Still add to bytes for now (placeholder 0)

                            bytes.Add(val);
                            currentAddr++;

                            if (isImmediate)
                                lineObj.Operand = "#" + opVal;
                        }
                        else if (operandBytes == 2)
                        {
                            // Try label first
                            string opVal = operandPart.Trim();
                            ushort addrVal = 0;

                            // Try symbols first
                            if (symbols.TryGetValue(opVal, out ushort knownAddr))
                            {
                                addrVal = knownAddr;
                            }
                            else if (TryParseWordSilent(opVal, out ushort wordVal))
                            {
                                addrVal = wordVal;
                            }
                            else
                            {
                                // Forward reference — resolve in pass 2 via labelAddresses
                                if (!forwardRefs.TryGetValue(opVal, out var absRefs))
                                {
                                    absRefs = new List<(int, int, int)>();
                                    forwardRefs[opVal] = absRefs;
                                }
                                absRefs.Add((2, bytes.Count, 0));
                            }

                            bytes.Add((byte)(addrVal & 0xFF));
                            bytes.Add((byte)(addrVal >> 8));
                            currentAddr += 2;

                            if (!foundStart && firstToken == "JSR" && labelAddresses.ContainsKey(opVal))
                            {
                                // Not the start, just a subroutine call
                            }
                            if (label == "start")
                            {
                                startAddress = (ushort)opcodeAddr;
                                foundStart = true;
                            }
                        }

                        lineObj.Bytes = bytes.Skip(opcodeAddr - origin).Take(operandBytes + 1).ToArray();
                        if (lineObj.Bytes.Length == 0)
                            lineObj.Bytes = null;
                    }
                    else
                    {
                        diags.Add(new AssemblyDiagnostic
                        {
                            Severity = "Error",
                            LineNumber = sourceLine,
                            Message = $"Unknown mnemonic '{firstToken}'"
                        });
                    }
                    listing.Add(lineObj);
                    break;
                }
            }
        }

        // Pass 2: resolve forward references
        bool hasUnresolved = false;
        foreach (var fwd in forwardRefs)
        {
            if (labelAddresses.TryGetValue(fwd.Key, out ushort addr))
            {
                foreach (var (pass, byteIdx, _) in fwd.Value)
                {
                    if (pass == 2)
                    {
                        bytes[byteIdx] = (byte)(addr & 0xFF);      // lo
                        bytes[byteIdx + 1] = (byte)(addr >> 8);    // hi
                    }
                }
            }
            else if (!symbols.ContainsKey(fwd.Key) && !ushort.TryParse(fwd.Key.StartsWith("$") ? fwd.Key[1..] : fwd.Key,
                         NumberStyles.HexNumber, null, out _))
            {
                diags.Add(new AssemblyDiagnostic
                {
                    Severity = "Error",
                    LineNumber = 0,
                    Message = $"Unresolved label '{fwd.Key}'"
                });
                hasUnresolved = true;
            }
        }

        // Resolve BNE relative offsets (pass 2)
        // Re-scan listing for BNE addresses
        for (int i = 0; i < listing.Count; i++)
        {
            var line = listing[i];
            if (line.Mnemonic == "BNE" && line.Address.HasValue && line.Operand is not null)
            {
                string target = line.Operand.Trim();
                if (labelAddresses.TryGetValue(target, out ushort targetAddr))
                {
                    int bneAddr = line.Address.Value + 2; // PC after BNE
                    int offset = targetAddr - bneAddr;
                    if (offset < -128 || offset > 127)
                    {
                        diags.Add(new AssemblyDiagnostic
                        {
                            Severity = "Error",
                            LineNumber = line.SourceLineNumber,
                            Message = $"BNE offset {offset} out of range (-128..127)"
                        });
                    }
                    else
                    {
                        // Find the BNE byte position in the byte stream
                        int bytePos = line.Address.Value - origin;
                        if (bytePos >= 0 && bytePos + 1 < bytes.Count)
                        {
                            bytes[bytePos + 1] = (byte)(sbyte)offset;
                        }
                    }
                }
            }
        }

        if (hasUnresolved || diags.Any(d => d.Severity == "Error"))
        {
            return new AssemblyResult
            {
                Success = false,
                Diagnostics = diags
            };
        }

        // Update listing addresses based on actual byte positions
        UpdateListingAddresses(listing, origin);

        // Check for start label
        if (labelAddresses.TryGetValue("start", out ushort startLabelAddr))
        {
            startAddress = startLabelAddr;
        }

        // Generate hex dump
        string hexDump = GenerateHexDump(bytes, origin);

        return new AssemblyResult
        {
            Success = true,
            Image = new AssemblyProgramImage
            {
                ProgramId = programId,
                LoadAddress = origin,
                StartAddress = startAddress,
                Bytes = bytes.ToArray(),
                HexDump = hexDump,
                Listing = listing
            },
            Diagnostics = diags
        };
    }

    private static bool PossibleLabel(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (char.IsDigit(s[0])) return false;
        return s.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static bool ParseByte(string text, Dictionary<string, ushort> symbols, int line, List<AssemblyDiagnostic> diags, out byte result)
    {
        result = 0;
        text = text.Trim();

        if (text.StartsWith("'") && text.EndsWith("'") && text.Length >= 2)
        {
            string inner = text[1..^1];
            result = inner switch
            {
                "\\n" => (byte)'\n',
                "\\r" => (byte)'\r',
                "\\t" => (byte)'\t',
                "\\\\" => (byte)'\\',
                "\\\"" => (byte)'"',
                "\\'" => (byte)'\'',
                _ => inner.Length > 0 ? (byte)inner[0] : (byte)0
            };
            return true;
        }

        if (symbols.TryGetValue(text, out ushort symVal))
        {
            if (symVal > 255)
            {
                diags.Add(new AssemblyDiagnostic { Severity = "Error", LineNumber = line, Message = $"Symbol '{text}' value ${symVal:X4} exceeds byte" });
                return false;
            }
            result = (byte)symVal;
            return true;
        }

        if (int.TryParse(text, out int decVal))
        {
            if (decVal < 0 || decVal > 255)
            {
                diags.Add(new AssemblyDiagnostic { Severity = "Error", LineNumber = line, Message = $"Value {decVal} out of byte range" });
                return false;
            }
            result = (byte)decVal;
            return true;
        }

        if (text.StartsWith("$") && ushort.TryParse(text[1..], NumberStyles.HexNumber, null, out ushort hexVal))
        {
            if (hexVal > 255)
            {
                diags.Add(new AssemblyDiagnostic { Severity = "Error", LineNumber = line, Message = $"Value ${hexVal:X4} exceeds byte" });
                return false;
            }
            result = (byte)hexVal;
            return true;
        }

        diags.Add(new AssemblyDiagnostic { Severity = "Error", LineNumber = line, Message = $"Invalid byte value '{text}'" });
        return false;
    }

    private static bool ParseWord(string text, Dictionary<string, ushort> symbols, int line, List<AssemblyDiagnostic> diags, out ushort result)
    {
        result = 0;
        text = text.Trim();

        if (symbols.TryGetValue(text, out ushort symVal))
        {
            result = symVal;
            return true;
        }

        if (text.StartsWith("$") && ushort.TryParse(text[1..], NumberStyles.HexNumber, null, out ushort hexVal))
        {
            result = hexVal;
            return true;
        }

        if (ushort.TryParse(text, out ushort decVal))
        {
            result = decVal;
            return true;
        }

        diags.Add(new AssemblyDiagnostic { Severity = "Error", LineNumber = line, Message = $"Invalid value '{text}'" });
        return false;
    }

    private static bool TryParseWordSilent(string text, out ushort result)
    {
        result = 0;
        text = text.Trim();
        if (text.StartsWith("$") && ushort.TryParse(text[1..], NumberStyles.HexNumber, null, out ushort hexVal))
        { result = hexVal; return true; }
        if (ushort.TryParse(text, NumberStyles.Integer, null, out ushort decVal))
        { result = decVal; return true; }
        return false;
    }

    private static List<byte> ParseByteList(string text, Dictionary<string, ushort> symbols, int line, List<AssemblyDiagnostic> diags)
    {
        var result = new List<byte>();
        var parts = text.Split(',');
        foreach (var part in parts)
        {
            string p = part.Trim();
            if (p.StartsWith("'") && p.EndsWith("'") && p.Length >= 2)
            {
                string inner = p[1..^1];
                result.Add(inner switch
                {
                    "\\n" => (byte)'\n',
                    "\\r" => (byte)'\r',
                    _ => inner.Length > 0 ? (byte)inner[0] : (byte)0
                });
            }
            else if (ParseByte(p, symbols, line, diags, out byte b))
            {
                result.Add(b);
            }
        }
        return result;
    }

    private static List<byte> ParseText(string text, int line, List<AssemblyDiagnostic> diags)
    {
        var result = new List<byte>();
        if (text.StartsWith("\"") && text.EndsWith("\""))
        {
            string inner = text[1..^1];
            for (int i = 0; i < inner.Length; i++)
            {
                if (inner[i] == '\\' && i + 1 < inner.Length)
                {
                    result.Add(inner[i + 1] switch
                    {
                        'n' => (byte)'\n',
                        'r' => (byte)'\r',
                        't' => (byte)'\t',
                        '\\' => (byte)'\\',
                        '\"' => (byte)'"',
                        _ => (byte)inner[i + 1]
                    });
                    i++;
                }
                else
                {
                    result.Add((byte)inner[i]);
                }
            }
        }
        return result;
    }

    private static string GenerateHexDump(List<byte> bytes, ushort origin)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Count; i += 16)
        {
            sb.AppendFormat("${0:X4}: ", (ushort)(origin + i));
            for (int j = 0; j < 16 && i + j < bytes.Count; j++)
                sb.AppendFormat("{0:X2} ", bytes[i + j]);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static void UpdateListingAddresses(List<AssemblyListingLine> listing, ushort origin)
    {
        ushort addr = origin;
        foreach (var line in listing)
        {
            if (line.Mnemonic is ".org" or ".equ")
                continue;
            if (line.Bytes is not null && line.Bytes.Length > 0)
            {
                line.Address = addr;
                addr += (ushort)line.Bytes.Length;
            }
        }
    }
}
