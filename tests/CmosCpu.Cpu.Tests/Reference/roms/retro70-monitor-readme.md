# Retro70 monitor ROM

File: `retro70-monitor.bin`

## Layout

- ROM size: 4096 bytes
- Intended mapping: `$F000-$FFFF`
- Screen memory: `$D000`
- Reset vector: `$FFFC-$FFFD -> $F000`
- NMI vector: `$FFFA-$FFFB -> $F000`
- IRQ/BRK vector: `$FFFE-$FFFF -> $F000`

## Behavior

After reset, the ROM writes this text to the text screen memory at `$D000`:

```text
RETRO70 READY
> 
```

Then it enters an infinite loop.

## Checksum

- SHA-256: `309a3132d450d428db8ce63d853f255989a271b467f1aa184ce5715bb9c2e849`
- Size: `4096` bytes
