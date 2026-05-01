# Architecture

## Modules

| Module | Description |
|--------|-------------|
| CmosCpu.Core | Core interfaces (IBus, IBusDevice, ISimulator, IAssembler) and types (CpuRegisters, Opcode, CpuFlags, InstructionInfo, SimulatorSnapshot) |
| CmosCpu.Bus | System bus implementation that routes reads/writes to registered devices |
| CmosCpu.Memory | RAM and ROM memory devices implementing IBusDevice |
| CmosCpu.Cpu | CPU core with fetch-decode-execute cycle and minimal ISA |
| CmosCpu.Devices | I/O devices: LedDevice, TimerDevice, RtcDevice |
| CmosCpu.Assembler | Simple text assembler with label support |
| CmosCpu.Runtime | DI container setup and simulator orchestration |
| CmosCpu.ConsoleApp | CLI application to load and run programs |

## Dependencies

```
CmosCpu.Core (no deps)
CmosCpu.Bus -> CmosCpu.Core
CmosCpu.Memory -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Cpu -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Devices -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Assembler -> CmosCpu.Core
CmosCpu.Runtime -> CmosCpu.Core, CmosCpu.Bus, CmosCpu.Memory, CmosCpu.Cpu, CmosCpu.Devices, CmosCpu.Assembler
CmosCpu.ConsoleApp -> CmosCpu.Runtime
```

## Execution Flow

1. User provides assembly source code
2. SimpleAssembler compiles it to binary
3. Binary is loaded into ROM device
4. CPU reads reset vector (0xFFFC-0xFFFD) to find entry point
5. CPU enters fetch-decode-execute loop:
   - Fetch: read byte from memory at PC address via bus
   - Decode: map opcode to instruction
   - Execute: perform operation, update registers/flags
6. Bus routes reads/writes to appropriate device based on address range

## CPU Cycle

The CPU implements a simple fetch-decode-execute cycle:

1. **Fetch**: Reads the byte at the address in the PC register from the bus, increments PC and cycle count.
2. **Decode**: The opcode byte is interpreted as an instruction.
3. **Execute**: The instruction is executed. Multi-byte instructions fetch additional operands.

## Bus Architecture

The SystemBus maintains a list of IBusDevice instances. Each device declares its address range (StartAddress to EndAddress). When a read or write occurs, the bus iterates through devices and routes the operation to the first matching device. Unmapped addresses return 0 on read and log a warning on write.

## Interrupt Handling

- **IRQ**: Triggered when RequestInterrupt() is called and the InterruptDisable flag is clear. CPU pushes PC (hi then lo) and flags to stack, sets InterruptDisable, reads vector from 0xFFFA-0xFFFB, and jumps to the handler address.
- **NMI**: Similar to IRQ but reads vector from 0xFFFE-0xFFFF and is not masked by InterruptDisable.
- **RESET**: Reads vector from 0xFFFC-0xFFFD, clears all registers, sets SP to 0xFF and InterruptDisable flag.
