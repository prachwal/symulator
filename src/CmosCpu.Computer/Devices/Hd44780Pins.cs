namespace CmosCpu.Computer.Devices;

public readonly record struct Hd44780Pins(bool Rs, bool Rw, bool E, byte Data);
