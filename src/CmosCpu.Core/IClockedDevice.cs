namespace CmosCpu.Core;

public interface IClockedDevice
{
    void Tick(ulong cycle);
}
