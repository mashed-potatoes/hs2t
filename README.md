# hs2t
Proprietary HiSilicon research tool lol

Tested on EMUI 8, may not work on newer firmwares.

```
USAGE:
    hs2t [OPTIONS] <COMMAND>

EXAMPLES:
    hs2t dump specified oeminfo
    hs2t dump secure --output ./secure_dump
    hs2t dump all --userparts
    hs2t list partitions

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    dump
    list
```

Unlocked FBLOCK is required.

Btw, assembly was obfuscated with ASAF to protect code from PrO gSm ToOlS. Actually, dotPeek shows some raw strings, but who cares.

Have a fun.
