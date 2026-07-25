"""
Extract a clean, transparent-background Black Knight battle sprite from the
concept sheet (BlackKnight/design.png).

Approach:
  1. Crop the large 3/4 hero pose (body + axe) from the concept sheet.
  2. Remove the warm parchment background with a border-connected flood fill
     (PIL.ImageDraw.floodfill) so only background reachable from the image
     border is erased -- interior silver armour stays intact.
  3. Feather the alpha edge by 1px, trim to the content bounding box.
  4. Save to NinjaMod/images/character/blackknight_battle.png.

Re-runnable: keeps the source concept art untouched. Adjust CROP / SEED_STEP /
THRESH below if the concept sheet changes.
"""
from __future__ import annotations

import os
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "BlackKnight", "design.png")
OUT_DIR = os.path.join(ROOT, "NinjaMod", "images", "character")
OUT = os.path.join(OUT_DIR, "blackknight_battle.png")

# Hero-pose crop box in the original 1448x1086 concept sheet (body + axe).
CROP = (12, 82, 722, 520)
SENTINEL = (255, 0, 255)   # magenta marker, absent from the palette
THRESH = 46                # flood-fill colour tolerance
SEED_STEP = 16             # seed every N px along the border
FLIP_HORIZONTAL = True     # flip so the knight's front faces left (toward the player)


def largest_component(mask: np.ndarray) -> np.ndarray:
    """Return a boolean mask of the largest 4-connected True region in `mask`."""
    h, w = mask.shape
    visited = np.zeros_like(mask, dtype=bool)
    best: list[tuple[int, int]] = []
    ys, xs = np.nonzero(mask)
    for sy, sx in zip(ys.tolist(), xs.tolist()):
        if visited[sy, sx]:
            continue
        stack = [(sy, sx)]
        visited[sy, sx] = True
        comp: list[tuple[int, int]] = []
        while stack:
            y, x = stack.pop()
            comp.append((y, x))
            if y > 0 and mask[y - 1, x] and not visited[y - 1, x]:
                visited[y - 1, x] = True
                stack.append((y - 1, x))
            if y < h - 1 and mask[y + 1, x] and not visited[y + 1, x]:
                visited[y + 1, x] = True
                stack.append((y + 1, x))
            if x > 0 and mask[y, x - 1] and not visited[y, x - 1]:
                visited[y, x - 1] = True
                stack.append((y, x - 1))
            if x < w - 1 and mask[y, x + 1] and not visited[y, x + 1]:
                visited[y, x + 1] = True
                stack.append((y, x + 1))
        if len(comp) > len(best):
            best = comp
    out = np.zeros_like(mask, dtype=bool)
    if best:
        idx = np.array(best)
        out[idx[:, 0], idx[:, 1]] = True
    return out


def main() -> None:
    os.makedirs(OUT_DIR, exist_ok=True)
    sheet = Image.open(SRC).convert("RGB")
    fig = sheet.crop(CROP)
    w, h = fig.size

    flood = fig.copy()
    seeds = []
    for x in range(0, w, SEED_STEP):
        seeds.append((x, 0))
        seeds.append((x, h - 1))
    for y in range(0, h, SEED_STEP):
        seeds.append((0, y))
        seeds.append((w - 1, y))
    for s in seeds:
        ImageDraw.floodfill(flood, s, SENTINEL, thresh=THRESH)

    arr = np.asarray(flood, dtype=np.int16)
    is_bg = (
        (arr[:, :, 0] == SENTINEL[0])
        & (arr[:, :, 1] == SENTINEL[1])
        & (arr[:, :, 2] == SENTINEL[2])
    )

    # Keep only the largest foreground blob (the knight + axe are one connected
    # region), dropping stray concept-sheet text/glyph islands.
    fg = ~is_bg
    keep = largest_component(fg)

    # Erode 2px: the flood fill stops at the dark character outline, leaving a
    # thin light anti-aliased ring between the beige background and the outline.
    # Eroding the mask inward removes that ugly light/white halo.
    mask = Image.fromarray(np.where(keep, 255, 0).astype(np.uint8), "L")
    mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MinFilter(3))

    # Extra de-fringe: drop any remaining very-light warm pixels that sit right on
    # the cut edge (never touches interior silver, which is protected by `inner`).
    rgb = np.asarray(fig, dtype=np.int16)
    light = (rgb.min(axis=2) > 190) & ((rgb[:, :, 0] - rgb[:, :, 2]) > -6)
    m = np.asarray(mask)
    inner = np.asarray(mask.filter(ImageFilter.MinFilter(5)))
    edge_light = light & (m > 0) & (inner == 0)
    m = np.where(edge_light, 0, m).astype(np.uint8)

    a_img = Image.fromarray(m, "L").filter(ImageFilter.GaussianBlur(0.4))
    out = fig.convert("RGBA")
    out.putalpha(a_img)

    # Face left toward the player (enemies sit on the right of the battlefield).
    if FLIP_HORIZONTAL:
        out = out.transpose(Image.FLIP_LEFT_RIGHT)

    # Trim to content bounding box.
    bbox = out.getbbox()
    if bbox:
        out = out.crop(bbox)

    out.save(OUT)
    print(f"saved {OUT}  size={out.size}")


if __name__ == "__main__":
    main()
