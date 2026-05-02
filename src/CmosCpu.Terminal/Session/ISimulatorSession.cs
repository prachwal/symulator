using CmosCpu.Computer;

namespace CmosCpu.Terminal.Session;

public interface ISimulatorSession
{
    ComputerMachine? Machine { get; }
    bool IsLoaded { get; }
    bool IsRunning { get; }
    long TotalInstructionsExecuted { get; }
    string? LastError { get; }

    bool LoadProfile(string jsonOrFilePath);
    bool LoadProfileFromFile(string path);
    void Reset();
    int Step();
    Task StartAsync(CancellationToken? externalToken = null);
    void Stop();
    void SetSpeed(int instructionsPerBatch, int renderDelayMs);
    (int batch, int delay) GetSpeed();

    byte[] ReadMemory(ushort start, int length);
    void WriteMemory(ushort start, byte[] data);

    bool LoadBinary(byte[] data, ushort origin);
    bool LoadBinaryToRom(byte[] data, ushort romStart);
    bool CompileAndLoad(string source);
}
