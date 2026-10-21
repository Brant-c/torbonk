# April 2012 flytext investigation — 2026-10-20

Inputs are the operator-labelled April archive set under
`D:/SWTORClassic/swtoremu/Assets2012April`. TOR files were opened read-only.
Existing extraction code (`source_pipeline.py`, `character_source.py`,
`ui_formats.py`) and the extracTOR historical filename map supplied bounded
lookups. No whole-archive hashing or bulk asset conversion was performed.
Archive hashes carried in manifests are inherited from the existing baseline.

## Directly recovered assets and definitions

`manifests/flytext-source.json` records selected entry offsets, sizes, compression,
lookup hashes and decoded-resource identities; `flytext-assets.json` records
font inventory, conversion and the exact negative movie lookups.

| Resource | Deciding evidence |
|---|---|
| `/resources/gfx/drawtext_fonts.swf` | `swtor_main_gfx_1.tor`, lookup `0xEC874439DE9F49C2`, 157,934 bytes; decoded SHA256 `481024b3ac47ff70e2345bd9cead332636d4bfd89e5fff6850a89b12aa8ebb45` |
| `/resources/gfx/fonts/swtor_fonts.gfx` | Same archive, lookup `0x994442C9A98C7211`, 331,224 bytes; SHA256 `82e7323febc37b75ad4832377869323cd647402f1a1cb7875c94d5dd5d2177fe` |
| `/resources/en-us/str/gui/flytext.stb` | `swtor_en-us_global_1.tor`, lookup `0xB9487F72BD8AE9C7`, 1,295 bytes; SHA256 `51670fa0506bceb2844da8efe13c6066919e6973de5b28246688583a1cb16eb9` |
| `/resources/systemgenerated/compilednative/14988080127247755753` | Flytext manager, SDEF key `0xBA5EE47B`, lookup `0xA978992513956A4B`, 42,766 bytes; SHA256 `afbaaef7dd4c490bbc71a3fad2ecfe25052b6a07786b6f99725bb5fdc37731c4` |
| `/resources/systemgenerated/compilednative/14988125348975866669` | Flytext node, SDEF key `0x7FB862B8`, lookup `0x4C3EFDDC7E65CD65`, 3,316 bytes; SHA256 `ea1e09982583eb11547fb549e7ad01550231dd12735dd5924379a552b20bf8d9` |

The STB contains literal “Critical”, “Absorbed” and “Resist” strings with IDs
`836075513708570`, `836075513708571` and `836075513708572`. It also contains
Evade, Deflect, Parry, Immune, Miss, Dodge, Cover, Glance and Interrupted.
These establish available labels, not which combat circumstances trigger them.
Only Absorbed/Resist have reserved presentation branches here; normal gameplay
does not produce them yet.

The generic GUI defaults and StyleSet editor definitions were inspected. They
did not establish a flytext font binding. All nineteen historical/production
`ui_flytext` SWF/GFX candidate paths in `flytext-assets.json` had no matching
April entry-table hash. This is a bounded negative finding, not proof that no
unnamed movie or native-engine font mapping exists. No flytext digit atlas,
critical texture or original shadow/outline filter was recovered.

## Font evidence and selection

The existing HUD uses converted Eurostile and Eurostile Bold from
`swtor_fonts.gfx`. That bundle also contains R6D8 Mod, italic Eurostile,
Aurek-Besh and two Franklin Gothic faces. The localized `fonts_en-us.swf`
contains the first five faces. Merely being the HUD font does not prove a
flytext role.

The separately recovered `drawtext_fonts.swf` contains FrizQuaReg (ID 1),
Eurostile Next LT Pro (ID 4, flags 141), Slate Mobile (ID 6) and italic Slate
Mobile (ID 8). ID 4's actual 346 glyph outlines/advances are converted to
`Assets/UI/Flytext/EurostileNextLTPro.ttf`, SHA256
`2b58e5c004af4a5c0f274e52115ad0c4ed90a0ce758442552a661c5963b42e23`.
The existing DefineFont3 converter preserves contours and bearings, divides
coordinates/advances by 20, inverts Y and uses 1024 UPM. Native hint instructions
and kerning are not restored. Godot reads this generated TTF into a shared
FontFile directly, so launching does not require a new editor import pass.

**Recovered:** original outlines and advances from a separate draw-text bundle.
**Reconstructed choice:** using this face for flytext. Neither the native font
enum nor the recovered manager script establishes the underlying face mapping.
This feature does not claim verified original combat typography.

## Client-derived script evidence

The local read-only research service located the April manager and node scripts,
native metadata and export tables. The integrated
[Jedipedia File Reader](https://swtor.jedipedia.net/reader) opened the manager
from the exact scriptdef.list row in its loaded 1.2.0 archive set. Its original
download was 42,766 bytes with SHA256
`afbaaef7dd4c490bbc71a3fad2ecfe25052b6a07786b6f99725bb5fdc37731c4`,
matching the local April entry. The selected representation was Code →
HeroScript Language. This is a reconstruction of compiled logic, with visible
unknowns/casts/goto labels, not recovered original HSL source.

Deciding exports: `PushBehaviors` hash `1495F134`, `LoadFontColors` hash
`24F1D9F8`, `addFlyTextNonlocalized` hash `C2212CEE`. The reader's script/class
names and public name-hash matches remain external labels; exact April script
identity comes from its SDEF/resource bytes. No live original-client behavior
was asserted from static decoding.

The manager's reconstructed `LoadFontColors` shows:

| Category ordinal | Reconstructed RGBA | Use here |
|---|---|---|
| 2, heal from self | `(0, 1, 0, 1)` | Green healing |
| 3, damage from self | `(1, 1, 1, 1)` | Basic-attack distinction is not currently available in the presentation event |
| 8, damage on self | `(1, 0, 0, 1)` | Basic-attack distinction remains unresolved |
| 21, ability damage from self | `(1, 1, 0, 1)` | Yellow outgoing damage |
| 23, ability damage on self | `(1, 0.2, 0.2, 1)` | Red incoming damage |

The HSL reconstruction displays some alpha values as integer float bit patterns:
`1065353216` is float32 `1.0`, and `1056964608` is `0.5`. The GOM category enum
`0x4000000572B94996` provides the ordinal/category labels. Enum/class/field
names supplied by historical `gom_type_names.xml` are labelled fallback names,
not assumed April-authored names. The actual `client.gom` source identity is
SHA256 `aa05f532724fb2e95359fda7f440bf78aa6c73dfe876c13583c60427fc82d450`.

The `flytext_behavior` enum `0x40000005C7F6E1B3` includes FLYUP, FLYDOWN,
POPANDSTAY, ARC, POPANDFLYDOWN, POPANDMOVE and follow/stationary variants.
The reader's FLYUP branch (ordinal 3) contains this reconstructed call:

```text
EF.AddFlyTextBehavior(node2, 0.0, 1.5, 0.075, GUIFLYTEXT_MIN_SCALE, 1.0, false, vec4, vec3)
```

Its motion direction is `(0,1,0)`; `GUIFLYTEXT_MIN_SCALE` is `0.5`. The manager
also exports named lifetime/speed/fade/transition/offset controls. This provides
evidence for upward movement and a 1.5-second lifetime. It does not recover the
engine's full animation/easing implementation or establish that every combat
category uses FLYUP. The manager contains a 500 ms queue-delay mechanism; this
feature deliberately omits its queuing policy to meet the requested immediate,
unlimited per-event display.

## Reconstructed Godot presentation

One camera-projected canvas uses the recovered font outlines, derived category
colors and 1.5-second lifetime. Font-role selection, 23/29-pixel sizes, black
3-pixel outline, offset shadow, 48-design-pixel ease-out rise, final smooth fade,
small positional/drift variation and brief critical pop are Godot reconstruction.
Critical prominence is supported by the event contract; the original critical
scale/animation was not recovered. Damage type is preserved without inventing
unsupported type-specific colors or mitigation rules.

All actual numbers come from Health's resolved applied values. No ability art,
impact timing or particle hit is treated as damage evidence. Runtime verification
is limited to `manifests/flytext-smoke.json`; visual fidelity awaits manual review.

Reproduce the bounded asset lookup/conversion with the same Python environment
used by `Convert-UI.ps1`, running `scripts/research_flytext.py`. The existing
vendored fontTools package is used; no new dependency was installed.
