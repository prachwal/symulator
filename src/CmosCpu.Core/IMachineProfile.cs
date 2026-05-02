namespace CmosCpu.Core;

public interface IMachineProfile
{
    string Name { get; }
    void Configure(IMachineBuilder builder);
}
