using Symulator.Application.Assembly;
using FluentAssertions;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class AssemblyProgramCatalogTests
{
    [TestMethod]
    public async Task LoadPrograms_FindsAsmFiles()
    {
        var catalog = new AssemblyProgramCatalog();
        var dir = Path.Combine(
            Path.GetDirectoryName(typeof(AssemblyProgramCatalogTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "programs", "asm");

        if (!Directory.Exists(dir))
            return; // skip if not available

        var programs = await catalog.LoadProgramsAsync(dir);

        programs.Should().NotBeEmpty();
        programs.Should().Contain(p => p.Id == "hello-uart");
        programs.Should().Contain(p => p.Id == "i2c-scan");
        programs.Should().Contain(p => p.Id == "blink");
    }

    [TestMethod]
    public async Task LoadProgram_ReadsSourceText()
    {
        var catalog = new AssemblyProgramCatalog();
        var dir = Path.Combine(
            Path.GetDirectoryName(typeof(AssemblyProgramCatalogTests).Assembly.Location)!,
            "..", "..", "..", "..", "..", "programs", "asm");

        if (!Directory.Exists(dir))
            return;

        var program = await catalog.LoadProgramAsync("hello-uart", dir);

        program.SourceText.Should().Contain("UART_DATA");
        program.SourceText.Should().Contain("Hello");
    }
}
