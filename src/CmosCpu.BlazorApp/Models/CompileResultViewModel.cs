namespace CmosCpu.BlazorApp.Models;

public class CompileResultViewModel
{
    public bool Success { get; set; }
    public int ByteCount { get; set; }
    public ushort Origin { get; set; }
    public List<string> Errors { get; set; } = new();
    public string HexDump { get; set; } = "";
    public string StatusMessage { get; set; } = "";
}
