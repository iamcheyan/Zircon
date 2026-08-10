# EI vs ZL map rendering comparison

Tool: `Tools/render_map_comparison.py` — renders the *same* `.map` file through
the authoritative renderer (`mapviewer`, rect layout = Mir3.exe projection)
with two library sources and stitches side-by-side PNGs:

- **EI 3.0 client** data: WIL theme folders (`Wood/`, `Sand/`, ...)
- **ZL 2017** data: `Debug/Client/Data/Map Data` ZL libraries

Output: `comparisons/<stem>__ei_vs_zl_z<z>.png` (labelled panels, vertical divider).

## How to run

```bash
python3 Tools/render_map_comparison.py '<EI Map dir>' \
    --data-ei '<EI Data dir>' \
    --data-zl '<Zircon>/Debug/Client/Data/Map Data' \
    --maps 3,0,41 --z 4 \
    --out docs/research/mir3-map-reconstruction/comparisons
```

Note the ZL data dir must be the real 2017 client `Map Data` (root ZLs +
`Wood/`/`Sand/`/`Snow/`/`Forest/` subdirs).  Passing the EI mirror data
(`mir3ei/Data`) produces identical panels — a useless comparison.

## Findings (z4, 3.map / 41.map / 0.map)

1. **No systematic hole in either panel.**  Exact canvas-background pixels are
   ~0 in both panels for 3.map and 41.map (0/0 exact at z4; 13 px in ZL panel
   of 3.map = 0.0009%).  Every map renders fully under both data sets.

2. **The ZL "dark" tiles are artwork differences, not missing frames.**  In
   the z5 side-by-side, the six darkest ZL tiles had mean RGB 78–100 vs EI
   118–149 — brighter than the canvas (16,16,20) background, i.e. ZL sprites
   exist but are darker than EI's for the same frame number.  Per-cell
   statistics on 3.map z4: 87% of sampled cells have matching bright/dark
   status; where they differ, mid=15/19 cells (housesc roofs etc.) are
   covered by an EI sprite but show sparse/transparent ZL sprite — same frame
   id, different artwork.

3. **Real resource shortfall is in the EI *data*, hidden by layering.**
   `3.map` mid file 25 -> `wood_smobjectsc`: EI `Wood/SmObjectsc.wil` has 969
   frames while the map's `frame_max` is 2531 — 2575 mid + 500 front cells
   decode to `None` under EI data (audit `map-audit.json`, anomaly total
   3255).  ZL `Wood/SmObjectsc.Zl` has 12586 frames, so the ZL panel renders
   those cells.  Visually the missing EI sprites are masked because the cells
   sit above ground/neighbour sprites (sparse per-cell stats: only 289 of
   2575 OOB cells are measurably brighter under EI).  Similarly 41.map:
   file 34 `sand_housesc` (EI 1274 vs map frame_max 1752) and file 40
   `sand_smobjectsc` (EI 631 vs map frame_max 3618) — 1619 OOB cells.

4. **Library-frame table** (`ei-vs-zl-libraries.json`): EI is the *smaller*
   side for every differing lib except wood_smobjectsc/wood_wallsc where ZL
   is vastly larger (12586/7531 vs 969/3791).  For housesc/cliffsc etc. the
   counts are close (9010 vs 14607, 7619 vs 7915) and EI renders fully.

## Interpretation for map reconstruction

- EI client data + EI maps: objects with frame > EI lib count silently
  vanish (decode None) but are covered by ground/other sprites — the map
  "renders" without obvious holes.
- ZL data is a *superset* for object libs (smobjectsc 12586 vs 969,
  wallsc 7531 vs 3791) but its frames are a different, often darker,
  artwork generation.
- Neither data set alone is "the original look": EI is the authentic frame
  numbering/art for 3.map's objects within frame range; ZL fills the
  out-of-range cells with different art.  Cross-referencing both is required
  to reconstruct a map faithfully (see audit + catalog stages).
