# Reference test ROMs for MOS 6502

## Source
- Klaus Dormann 6502/65C02 functional test suite:
  https://github.com/Klaus2m5/6502_65C02_functional_tests

## Files not committed
ROM binaries are NOT included in this repository due to licensing and size.
Place them manually in this directory:

```
tests/CmosCpu.Cpu.Tests/Reference/roms/
```

## Expected filenames
- `6502_functional_test.bin` — Klaus Dormann's 6502 functional test
- `decimal_test.bin` — Klaus Dormann's decimal/BCD test

## How to build the ROMs locally
1. Clone https://github.com/Klaus2m5/6502_65C02_functional_tests
2. Assemble with your preferred 6502 assembler (e.g. `vasm`, `dasm`, `xa65`)
3. Copy the raw binary output to `roms/` with the expected name

## How to run tests
```sh
dotnet test tests/CmosCpu.Cpu.Tests/
```

When ROMs are absent, reference tests report `Inconclusive` instead of failing.
