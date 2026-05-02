# MOS 6502 reference tests

## Goal
Run well-known 6502 functional test suites against the `Mos6502Cpu` to validate correctness beyond unit tests.

## Recommended test suites

### 6502_functional_test
- Author: Klaus Dormann
- Format: raw 6502 binary loaded at `$0000`, reset vector at `$FFFC` → `$0400`
- Runs a series of self-checking tests; success signaled by writing `$00` to `$8000`
- Covers: all official opcodes, all addressing modes, flag behavior, decimal mode, interrupts

### decimal_test
- Author: Klaus Dormann (or extracted from functional test)
- Focuses specifically on BCD (decimal) ADC and SBC
- Validates all nibble combinations

### interrupt_test
- Validates NMI, IRQ, BRK, RTI handling
- Ensures stack behavior, status flag preservation, and cycle timing

### illegal_opcodes_test (future)
- For C64 compatibility, once undocumented opcodes are implemented
- Tests the most common illegal opcodes (LAX, SAX, DCP, ISC, SLO, RLA, SRE, RRA, etc.)

## Test runner

The `Reference6502TestRunner` class provides a generic runner:

```csharp
ReferenceTestResult result = Reference6502TestRunner.Run(
    romPath,                // path to .bin file
    loadAddress: 0x0000,    // where to load the binary
    resetVector: 0x0400,    // reset vector target
    maxSteps: 100_000_000,  // timeout
    probe: (ram, cpu) =>    // callback to detect success/failure
    {
        byte status = ram.ReadByte(0x8000);
        if (status == 0x00) return ReferenceTestState.Success;
        if (status != 0xFF) return ReferenceTestState.Failed;
        return ReferenceTestState.Continue;
    });
```

## Integration status
- [x] Reference runner helper (`Reference6502TestRunner`)
- [x] Functional reference test (`Mos6502FunctionalReferenceTests`)
- [x] Decimal reference test (`Mos6502DecimalReferenceTests`)
- [ ] Actual ROM binaries (not committed — manual download needed)

## How to run reference tests locally
1. Build the ROM binary from https://github.com/Klaus2m5/6502_65C02_functional_tests
2. Copy to `tests/CmosCpu.Cpu.Tests/Reference/roms/6502_functional_test.bin`
3. Run `dotnet test tests/CmosCpu.Cpu.Tests/`
4. If ROM is missing, test reports `Inconclusive`

## Constraints
- Reference ROMs are not included in this repository due to licensing.
- Integration requires downloading the binary separately.
- Test runner must exist before C64 integration, but ROM execution can be deferred.
