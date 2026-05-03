using CmosCpu.Computer.Devices;
using CmosCpu.Computer.Devices.I2c;
using CmosCpu.Computer.Devices.Serial;
using Symulator.Application.Solutions;
using Symulator.Application.Terminal;
using Symulator.Machines.MinimalBlink.Cpu;
using Symulator.Machines.MinimalBlink.Memory;
using Symulator.Machines.MinimalBlink.Models;
using NLog;

namespace Symulator.Machines.MinimalBlink.Hardware;

public sealed class MinimalBlinkHardwareSolutionBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public MinimalBlinkHardwareRuntime Build(SolutionDefinition solution)
    {
        var memory = new MinimalBlinkMemory();

        Hd44780Lcd? lcd = null;
        Hd44780DirectBusAdapter? lcdBus = null;
        MinimalBlinkLcdBuffer? lcdBuffer = null;
        I2cBus? i2cBus = null;
        MemoryMappedI2cController? i2cCtrl = null;
        UartDevice? uart = null;
        MemoryMappedUartAdapter? uartAdapter = null;
        TerminalBuffer? terminal = null;

        foreach (var device in solution.Devices)
        {
            switch (device.Type)
            {
                case "cpu":
                    // no hardware to build, CPU is created below
                    break;

                case "led-mmio":
                    if (!string.IsNullOrEmpty(device.EffectiveAddress))
                    {
                        var addr = AddressParser.Parse16(device.EffectiveAddress);
                        if (addr != MinimalBlinkMemory.LedPort)
                            throw new InvalidOperationException(
                                $"LED MMIO address must be 0x{MinimalBlinkMemory.LedPort:X4}, got 0x{addr:X4}");
                    }
                    Logger.Debug("Builder: LED attached");
                    break;

                case "hd44780-mmio":
                {
                    string baseAddrStr = device.BaseAddress ?? device.EffectiveAddress;
                    ushort baseAddr = string.IsNullOrEmpty(baseAddrStr)
                        ? MinimalBlinkMemory.LcdCommandPort
                        : AddressParser.Parse16(baseAddrStr);

                    if (baseAddr != MinimalBlinkMemory.LcdCommandPort)
                        throw new InvalidOperationException(
                            $"LCD MMIO base address must be 0x{MinimalBlinkMemory.LcdCommandPort:X4}, got 0x{baseAddr:X4}");

                    lcd = new Hd44780Lcd();
                    lcdBus = new Hd44780DirectBusAdapter(lcd, baseAddr);
                    lcdBuffer = new MinimalBlinkLcdBuffer();
                    memory.AttachLcd(lcd, lcdBus);
                    Logger.Debug("Builder: LCD attached at 0x{Addr:X4}", baseAddr);
                    break;
                }

                case "i2c-controller-mmio":
                {
                    string baseAddrStr = device.BaseAddress ?? device.EffectiveAddress;
                    ushort baseAddr = string.IsNullOrEmpty(baseAddrStr)
                        ? MinimalBlinkMemory.I2cBase
                        : AddressParser.Parse16(baseAddrStr);

                    if (baseAddr != MinimalBlinkMemory.I2cBase)
                        throw new InvalidOperationException(
                            $"I2C MMIO base address must be 0x{MinimalBlinkMemory.I2cBase:X4}, got 0x{baseAddr:X4}");

                    i2cBus = new I2cBus();
                    i2cCtrl = new MemoryMappedI2cController(i2cBus);
                    memory.AttachI2c(i2cCtrl);

                    // Attach PCF8574 backpack if also declared
                    var pcfDevice = solution.Devices.FirstOrDefault(d => d.Type == "hd44780-pcf8574");
                    if (pcfDevice is not null)
                    {
                        byte pcfAddr = (byte)AddressParser.Parse16(pcfDevice.Address ?? "0x27");
                        var lcd4Bit = new Hd44780Parallel4BitAdapter(lcd ?? new Hd44780Lcd());
                        var backpack = new Pcf8574Hd44780Backpack(pcfAddr, lcd4Bit);
                        i2cBus.Attach(backpack);
                        Logger.Debug("Builder: PCF8574 attached at 0x{Addr:X2}", pcfAddr);
                    }

                    Logger.Debug("Builder: I2C controller attached at 0x{Addr:X4}", baseAddr);
                    break;
                }

                case "uart-mmio":
                {
                    string baseAddrStr = device.BaseAddress ?? device.EffectiveAddress;
                    ushort baseAddr = string.IsNullOrEmpty(baseAddrStr)
                        ? MinimalBlinkMemory.UartBase
                        : AddressParser.Parse16(baseAddrStr);

                    if (baseAddr != MinimalBlinkMemory.UartBase)
                        throw new InvalidOperationException(
                            $"UART MMIO base address must be 0x{MinimalBlinkMemory.UartBase:X4}, got 0x{baseAddr:X4}");

                    uart = new UartDevice();
                    uartAdapter = new MemoryMappedUartAdapter(uart);
                    terminal = new TerminalBuffer();
                    uart.ByteTransmitted += (_, args) => terminal.WriteByte(args.Value);
                    memory.AttachUart(uartAdapter);
                    Logger.Debug("Builder: UART attached at 0x{Addr:X4}", baseAddr);
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unsupported device type: {device.Type}");
            }
        }

        var cpu = new MinimalBlinkCpu(memory);

        return new MinimalBlinkHardwareRuntime
        {
            Cpu = cpu,
            Memory = memory,
            Lcd = lcd,
            LcdBus = lcdBus,
            LcdBuffer = lcdBuffer,
            I2cBus = i2cBus,
            I2cController = i2cCtrl,
            Uart = uart,
            UartAdapter = uartAdapter,
            Terminal = terminal,
        };
    }
}
