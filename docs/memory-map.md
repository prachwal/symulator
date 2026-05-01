# Memory Map

| Range | Description |
|-------|-------------|
| 0x0000 - 0x7FFF | RAM (32 KB) |
| 0x8000 - 0xBFFF | ROM (16 KB) |
| 0xC000 - 0xC0FF | I/O mapped devices |
| 0xFF00 - 0xFFFF | System vectors and reserved |

## I/O Device Map

| Address | Device | Register |
|---------|--------|----------|
| 0xC000 | LED | LED_STATE (0=OFF, 1=ON) |
| 0xC010 | Timer | TIMER_COUNTER (low byte) |
| 0xC011 | Timer | TIMER_CONTROL (bit 7 = enable) |
| 0xC012 | Timer | TIMER_STATUS (bit 0 = fired) |
| 0xC020 | RTC | SECOND |
| 0xC021 | RTC | MINUTE |
| 0xC022 | RTC | HOUR |
| 0xC023 | RTC | DAY |
| 0xC024 | RTC | MONTH |
| 0xC025 | RTC | YEAR |

## System Vectors (0xFF00-0xFFFF)

| Address | Description |
|---------|-------------|
| 0xFFFA (lo) / 0xFFFB (hi) | IRQ vector |
| 0xFFFC (lo) / 0xFFFD (hi) | RESET vector |
| 0xFFFE (lo) / 0xFFFF (hi) | NMI vector |

## Stack

The stack is located at 0x0100-0x01FF (256 bytes). The SP register (8-bit) is used as an offset from 0x0100. SP starts at 0xFF (top of stack) and decrements on push.
