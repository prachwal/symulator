using CmosCpu.Computer.Devices;
using CmosCpu.Computer.Devices.I2c;
using CmosCpu.Computer.Devices.Serial;
using Symulator.Application.Terminal;
using Symulator.Machines.MinimalBlink.Cpu;
using Symulator.Machines.MinimalBlink.Memory;
using Symulator.Machines.MinimalBlink.Models;

namespace Symulator.Machines.MinimalBlink.Hardware;

public sealed class MinimalBlinkHardwareRuntime
{
    public MinimalBlinkCpu Cpu { get; init; } = default!;
    public MinimalBlinkMemory Memory { get; init; } = default!;

    public Hd44780Lcd? Lcd { get; init; }
    public Hd44780DirectBusAdapter? LcdBus { get; init; }
    public MinimalBlinkLcdBuffer? LcdBuffer { get; init; }

    public I2cBus? I2cBus { get; init; }
    public MemoryMappedI2cController? I2cController { get; init; }

    public UartDevice? Uart { get; init; }
    public MemoryMappedUartAdapter? UartAdapter { get; init; }
    public TerminalBuffer? Terminal { get; init; }

    public IReadOnlySet<string> DeviceTypes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasDevice(string type) => DeviceTypes.Contains(type);

    public bool HasRuntimeInstance(string type) => type switch
    {
        "cpu" => true,
        "led-mmio" => true, // LED is always built with MinimalBlinkMemory
        "uart-mmio" => Uart is not null,
        "hd44780-mmio" => Lcd is not null,
        "i2c-controller-mmio" => I2cController is not null,
        _ => DeviceTypes.Contains(type)
    };
}
