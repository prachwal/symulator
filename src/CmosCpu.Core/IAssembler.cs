namespace CmosCpu.Core;

public interface IAssembler
{
    (byte[] binary, IReadOnlyList<string> errors) Assemble(string source, ushort origin = 0x8000);
}
