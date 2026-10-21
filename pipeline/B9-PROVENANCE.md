# B9 cursor provenance

## Corrected active cursor — installed SWTOR native resources

The first B9 pass selected a cyan engine-default cursor from the April archives.
The user correctly identified that it was not SWTOR's gold gameplay cursor.
That selection is retired. Its archive evidence is retained below as rejected
candidate research, not as proof of the game's active cursor.

The active gold cursor and hand variants were visually inspected and extracted
read-only from:

`C:/Program Files (x86)/Electronic Arts/BioWare/Star Wars - The Old Republic/swtor/retailclient/swtor.exe`

Executable SHA256: `d82746520dfeea3cdacd0b3f40bc46771b6fe62bc4b8873f23b50b49233df295`;
length 30,681,088 bytes. This is **installed-client provenance, not verified April
2012 TOR provenance**. The PE timestamp is September 18, 2026; no April-byte
equivalence is claimed. The executable is read as data and is never loaded,
launched, changed or copied into the game.

| Godot role | RT_GROUP_CURSOR / RT_CURSOR / language | Native hotspot | Output |
|---|---|---|---|
| Default gold arrow | 201 / 31 / 1033 | (8,3) | `gold_default.png` |
| Enabled button gold hand | 204 / 34 / 1033 | (13,1) | `gold_hover.png` |
| Disabled button blue hand | 205 / 35 / 1033 | (13,2) | `gold_disabled.png` |

All three are original 32×32, 32-bit cursor images. Hotspots are the first two
little-endian 16-bit values in each RT_CURSOR payload. Group entries establish
the exact linked cursor IDs. Pixel conversion preserves original BGRA colors,
alpha and canvas, with only row order/channel conversion to PNG. No recoloring,
redrawing, cropping, approximate artwork or guessed hotspot is used.

The native resources have numeric identities rather than semantic state names.
Mapping the gold hand to enabled buttons and the blue hand to disabled buttons
is an explicit Godot UI adaptation. No distinct pressed image has been identified;
clicking retains the hover/default artwork. Other native contextual artwork was
inspected but is not wired to unsupported gameplay interactions.

`scripts/prepare_b9_native_cursor.py` reproduces the bounded selection of three
groups and three images. Original payloads plus reconstructed `.cur` containers
are saved under ignored `source-assets/native-client-cursors/`.
`manifests/b9-native-cursor.json` records executable and individual payload/PNG
hashes, resource identities, offsets, dimensions and recovered hotspots.
`Assets/UI/Cursor/cursor_theme.tres` binds these assets in the shared controller.

## Retired cyan engine-default candidate — April archive evidence

**Client-derived:** read-only extraction from the operator-labelled April 2012
archives in `D:/SWTORClassic/swtoremu/Assets2012April`. Historical filenames are
discovery hints; each selected canonical path was independently hashed to a
unique archive entry and its decoded bytes inspected. The research MCP returned
matching decoded SHA256 values. No whole-archive hashing was repeated.

| Resource | Archive / TOR lookup hash | Decoded SHA256 |
|---|---|---|
| `/resources/art/defaultassets/cursor.dds` | `swtor_main_art_misc_1.tor` / `0x45942BC32FF63F54` | `5f20ede3a0c40371cad0070d498bb6a66120e877b33e89526811f1c9bcb0d33c` |
| `/resources/art/defaultassets/cursor.tex` | `swtor_main_art_misc_1.tor` / `0x84BBF2C99BF4551C` | `9e856adfd7177f4ec87568640ae86a8563870ea1482925d7043ef5bebdfdd8e1` |
| `/resources/guixml/_cursors.xml` | `swtor_main_gamedata_1.tor` / `0xB21D669DC918FD28` | `ea5e96441291e9bbff60bf69945444e435464cd3a1f1865c8ed6e7d2cf6a720b` |

The 4,224-byte DDS is a 32×32 RGBA cursor arrow with transparent padding and a
cyan illuminated edge. It was visually inspected at enlarged size. Its decoded
alpha bounds are [5,6,26,27] (XYXY). This is an actual cursor texture in the
April archive, not an unrelated atlas crop or generated substitute. Its presence
does not establish it as SWTOR's gameplay cursor; that earlier selection was
wrong and is no longer used.

The `.tex` contains linear texture filtering/addressing parameters, without a
hotspot or state images. `_cursors.xml` declares `_customCursor`, a generic
40×40 `GUIPanel` on layer `CURSOR`, ignoring mouse events. It contains no image
binding, hotspot, animation or pressed/hover/disabled mapping.

Bounded research included MCP resource discovery for `cursor`, `.cur` and
`mouse`; those indexed name searches returned no candidates. The existing local
filename map supplied the exact defaultassets and guixml names above, whose
bytes were then verified against the April archive. Test particle names were
not used as cursor art. GOM search found the April `Cursor` object
`0xE00031F4DD568B10` in `client.gom`, but no decoded value/hotspot. Other
cursor-related schema names were labelled historical fallback and did not
establish a texture mapping. No hover, pressed, disabled or animated/contextual
cursor variant was recovered in that initial archive-only investigation. Native
executable resources subsequently supplied the correct artwork above.

`scripts/prepare_b9_cursor.py` reproduces extraction and DDS → RGBA PNG without
cropping, scaling or recoloring. Generated `godot/Assets/UI/Cursor/default.png`
SHA256 is `f7d72c009f278bfebdc3bea746a334982b1537c5605226fcff2c14576a1007d4`.
Original extracted bytes remain under ignored `source-assets/april2012/`.
`manifests/b9-source.json` records archive offsets, lengths, exact hashes and
output paths; archive SHA256 fields reuse the existing inventory baseline.
`manifests/b9-cursor-conversion.json` records conversion, dimensions and gaps.

The retired candidate's (5,5) hotspot was adapted. It is no longer active. The
current theme uses native gold/hand cursor metadata above. The earlier
`prepare_b9_cursor.py`, `default.png`, source manifest and conversion manifest
remain available only as explicitly rejected engine-default candidate evidence.
