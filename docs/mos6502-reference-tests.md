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

## Integration plan

1. Place test binaries in a well-known directory:
   ```
   tests/CmosCpu.Cpu.Tests/Reference/roms/
   ```

2. Write a test helper that:
   - Loads the binary into RAM at the expected address
   - Sets reset vector accordingly
   - Runs `Step()` until a sentinel value appears at a success address
   - Fails if the test times out or writes an error code

3. Example skeleton:

   ```csharp
   public static void RunFunctionalTest(IMemoryBus ram, byte[] rom, int maxSteps)
   {
       // Load ROM at $0000
       // Set reset vector
       var cpu = new Mos6502Cpu(ram);
       cpu.Reset();

       for (int i = 0; i < maxSteps; i++)
       {
           cpu.Step();
           byte status = ram.ReadByte(0x8000);
           if (status == 0x00) return; // success
           if (status != 0xFF) Assert.Fail($"Test failed with code 0x{status:X2}");
       }
       Assert.Fail("Timeout");
   }
   ```

4. Reference ROM source:
   - https://github.com/Klaus2m5/6502_65C02_functional_tests

## Constraints
- Reference ROMs are not included in this repository due to licensing.
- Integration requires downloading the binary separately.
- Test runner must exist before C64 integration, but ROM execution can be deferred.
