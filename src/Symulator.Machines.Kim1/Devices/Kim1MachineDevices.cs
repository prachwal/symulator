using CmosCpu.Computer;
using CmosCpu.Computer.Abstractions;

namespace Symulator.Machines.Kim1.Devices;

public sealed class Kim1MachineDevices : IMachineStepHook
{
    public Kim1Riot6530IoDevice Riot002 { get; }
    public Kim1Riot6530IoDevice Riot003 { get; }
    public Kim1KeypadState Keypad { get; }
    public Kim1LedDisplayState LedDisplay { get; }

    public Kim1MachineDevices(
        Kim1Riot6530IoDevice riot002,
        Kim1Riot6530IoDevice riot003,
        Kim1KeypadState keypad,
        Kim1LedDisplayState ledDisplay)
    {
        Riot002 = riot002;
        Riot003 = riot003;
        Keypad = keypad;
        LedDisplay = ledDisplay;

        Riot002.Keypad = Keypad;
        riot002.LedDisplay = LedDisplay;
    }

    public void AttachTo(ComputerMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        machine.MapDevice(Riot002);
        machine.MapDevice(Riot003);
        machine.AddStepHook(this);
    }

    public void AfterStep(int cpuCycles)
    {
        UpdateKeypadMatrix();
    }

    private void UpdateKeypadMatrix()
    {
        byte columnOutput = Riot002.PortAData;
        byte columnDdr = Riot002.PortADdr;
        byte rowInput = Keypad.GetRowState(columnOutput, columnDdr);

        byte existingInput = Riot003.PortAInputValue;
        byte mergedInput = (byte)((existingInput & 0xC0) | (rowInput & 0x3F));
        Riot003.SetPortAInput(mergedInput);
    }
}
