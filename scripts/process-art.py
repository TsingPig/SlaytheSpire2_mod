"""Prepare the supplied Ninja artwork for the mod.

Run from the repository root with ``py scripts/process-art.py``.
The crop centers are chosen per illustration so that the action remains visible in
Slay the Spire 2's landscape card-portrait window.
"""

from pathlib import Path
from typing import List

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageOps


ROOT = Path(__file__).resolve().parents[1]
TEMP = ROOT / "temp"
NEW_ART = ROOT / "tmp"
MOD_IMAGES = ROOT / "NinjaMod" / "images"

# New card sheets supplied in tmp/.  The ordering matches the panels from left
# to right.  AfterimageAttack intentionally reuses the Afterimage Art panel.
NEW_CARD_SHEETS = (
    (
        "1.png",
        (
            "detonation",
            "lihuo",
            "earth_talisman",
            "light_snow",
            "soul_chase",
        ),
    ),
    (
        "2.png",
        (
            "soul_reap",
            "crane_shield",
            "rashomon",
            "afterimage_art",
            "burning_heart",
        ),
    ),
    (
        "3.png",
        (
            "musashi_thrust",
            "musashi_godspeed",
            "musashi_void_slash",
            "musashi_inheritance",
            "musashi_enmei_style",
        ),
    ),
    (
        "4.png",
        (
            "musashi_swift_triangle",
            "musashi_advancing_fountain",
            "musashi_two_heavens",
            "musashi_seven_star",
        ),
    ),
)

NEW_POWER_ART = (
    ("image copy.png", "afterimage_power"),
    ("image.png", "enmei_power"),
)

# (output slug, sheet column, sheet row, vertical focus within the panel)
CARD_CROPS = (
    ("ninjastrike", 0, 0, 278),
    ("ninjadefend", 1, 0, 270),
    ("shuriken", 2, 0, 285),
    ("assassination", 3, 0, 292),
    ("kunai", 4, 0, 225),
    ("flamebarrage", 0, 1, 265),
    ("demonflameburst", 1, 1, 270),
    ("shadowclone", 2, 1, 288),
    ("earthescape", 3, 1, 300),
    ("earthwall", 4, 1, 300),
)

PANEL_X = (8, 295, 581, 868, 1154)
PANEL_Y = (12, 554)
PANEL_SIZE = (277, 527)


def prepare_cards() -> List[Path]:
    sheet = Image.open(TEMP / "image copy 2.png").convert("RGB")
    big_dir = MOD_IMAGES / "card_portraits" / "big"
    small_dir = MOD_IMAGES / "card_portraits"
    big_dir.mkdir(parents=True, exist_ok=True)
    small_dir.mkdir(parents=True, exist_ok=True)

    outputs: List[Path] = []
    crop_height = round(PANEL_SIZE[0] * 760 / 1000)
    for slug, col, row, focus_y in CARD_CROPS:
        panel = sheet.crop(
            (
                PANEL_X[col],
                PANEL_Y[row],
                PANEL_X[col] + PANEL_SIZE[0],
                PANEL_Y[row] + PANEL_SIZE[1],
            )
        )
        top = max(0, min(PANEL_SIZE[1] - crop_height, focus_y - crop_height // 2))
        art = panel.crop((0, top, PANEL_SIZE[0], top + crop_height))
        big = art.resize((1000, 760), Image.Resampling.LANCZOS)
        small = big.resize((250, 190), Image.Resampling.LANCZOS)

        big_path = big_dir / f"{slug}.png"
        small_path = small_dir / f"{slug}.png"
        big.save(big_path, optimize=True)
        small.save(small_path, optimize=True)
        outputs.extend((big_path, small_path))

    return outputs


def split_card_sheet(sheet: Image.Image, panel_count: int) -> List[Image.Image]:
    """Split an AI contact sheet at its near-white vertical separators."""

    source = np.array(sheet.convert("RGB"))
    minimum = source.min(axis=2)
    chroma = source.max(axis=2) - minimum
    near_white = (minimum > 240) & (chroma < 18)
    white_score = near_white.mean(axis=0)
    smooth_score = np.convolve(white_score, np.ones(7) / 7, mode="same")

    boundaries = [0]
    expected_width = sheet.width / panel_count
    search_radius = max(24, round(expected_width * 0.16))
    for index in range(1, panel_count):
        expected = round(index * expected_width)
        left = max(boundaries[-1] + 8, expected - search_radius)
        right = min(sheet.width - 8, expected + search_radius)
        boundary = left + int(np.argmax(smooth_score[left:right]))
        boundaries.append(boundary)
    boundaries.append(sheet.width)

    panels: List[Image.Image] = []
    for left, right in zip(boundaries, boundaries[1:]):
        segment = source[:, left:right]
        seg_min = segment.min(axis=2)
        seg_chroma = segment.max(axis=2) - seg_min
        content = ~((seg_min > 240) & (seg_chroma < 18))
        x_density = content.mean(axis=0)
        y_density = content.mean(axis=1)
        xs = np.where(x_density > 0.04)[0]
        ys = np.where(y_density > 0.04)[0]
        if not len(xs) or not len(ys):
            raise RuntimeError("A card sheet panel contains no artwork")
        panels.append(
            sheet.crop(
                (
                    left + int(xs[0]),
                    int(ys[0]),
                    left + int(xs[-1]) + 1,
                    int(ys[-1]) + 1,
                )
            )
        )
    return panels


def save_card_art(panel: Image.Image, slug: str) -> List[Path]:
    """Create the full-card and thumbnail sizes consumed by NinjaModCard."""

    big_dir = MOD_IMAGES / "card_portraits" / "big"
    small_dir = MOD_IMAGES / "card_portraits"
    big_dir.mkdir(parents=True, exist_ok=True)
    small_dir.mkdir(parents=True, exist_ok=True)

    big = ImageOps.fit(
        panel.convert("RGB"),
        (606, 852),
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.5),
    )
    small = big.resize((250, 350), Image.Resampling.LANCZOS)
    big_path = big_dir / f"{slug}.png"
    small_path = small_dir / f"{slug}.png"
    big.save(big_path, optimize=True)
    small.save(small_path, optimize=True)
    return [big_path, small_path]


def prepare_new_cards() -> List[Path]:
    outputs: List[Path] = []
    afterimage_panel: Image.Image | None = None
    for sheet_name, slugs in NEW_CARD_SHEETS:
        source_path = NEW_ART / sheet_name
        if not source_path.exists():
            raise FileNotFoundError(f"Missing new card sheet: {source_path}")
        sheet = Image.open(source_path).convert("RGB")
        panels = split_card_sheet(sheet, len(slugs))
        for slug, panel in zip(slugs, panels):
            outputs.extend(save_card_art(panel, slug))
            if slug == "afterimage_art":
                afterimage_panel = panel.copy()

    if afterimage_panel is None:
        raise RuntimeError("Afterimage Art panel was not found")
    outputs.extend(save_card_art(afterimage_panel, "afterimage_attack"))
    return outputs


def remove_light_checkerboard(image: Image.Image) -> Image.Image:
    """Remove the baked white/gray checkerboard from a supplied power icon."""

    source = np.array(image.convert("RGB"))
    minimum = source.min(axis=2)
    chroma = source.max(axis=2) - minimum
    background_candidate = ((minimum > 218) & (chroma < 24)).astype(np.uint8)

    count, labels = cv2.connectedComponents(background_candidate, 8)
    border_labels = np.unique(
        np.concatenate((labels[0], labels[-1], labels[:, 0], labels[:, -1]))
    )
    border_labels = border_labels[border_labels != 0]
    background = np.isin(labels, border_labels).astype(np.uint8)
    background = cv2.dilate(background, np.ones((3, 3), np.uint8), iterations=1)
    alpha = 255 - cv2.GaussianBlur(background * 255, (0, 0), 0.7)
    alpha[background == 1] = 0
    return Image.fromarray(np.dstack((source, alpha.astype(np.uint8))))


def prepare_new_powers() -> List[Path]:
    big_dir = MOD_IMAGES / "powers" / "big"
    small_dir = MOD_IMAGES / "powers"
    big_dir.mkdir(parents=True, exist_ok=True)
    small_dir.mkdir(parents=True, exist_ok=True)

    outputs: List[Path] = []
    for source_name, slug in NEW_POWER_ART:
        source_path = NEW_ART / source_name
        if not source_path.exists():
            raise FileNotFoundError(f"Missing new power art: {source_path}")
        extracted = remove_light_checkerboard(Image.open(source_path))
        big = extracted.resize((256, 256), Image.Resampling.LANCZOS)
        small = big.resize((64, 64), Image.Resampling.LANCZOS)
        big_path = big_dir / f"{slug}.png"
        small_path = small_dir / f"{slug}.png"
        big.save(big_path, optimize=True)
        small.save(small_path, optimize=True)
        outputs.extend((big_path, small_path))
    return outputs


def remove_checkerboard() -> Image.Image:
    """Extract the character without damaging pale weapon details.

    The source contains a baked white/gray checkerboard, not real alpha. GrabCut
    uses the bright neutral checks as background evidence while the large connected
    illustrated subject remains foreground. A one-pixel contraction removes the
    baked checker fringe from antialiased edges.
    """

    source = np.array(Image.open(TEMP / "image copy.png").convert("RGB"))
    height, width = source.shape[:2]
    mask = np.full((height, width), cv2.GC_BGD, np.uint8)
    mask[80 : height - 40, 55 : width - 55] = cv2.GC_PR_FGD

    maximum = source.max(axis=2)
    minimum = source.min(axis=2)
    chroma = maximum - minimum
    mask[(minimum > 218) & (chroma < 16)] = cv2.GC_PR_BGD

    mask[:50, :] = cv2.GC_BGD
    mask[-50:, :] = cv2.GC_BGD
    mask[:, :40] = cv2.GC_BGD
    mask[:, -40:] = cv2.GC_BGD

    inside = np.zeros((height, width), dtype=bool)
    inside[100:1320, 70:1040] = True
    mask[inside & ((maximum < 175) | ((chroma > 35) & (minimum < 215)))] = cv2.GC_FGD

    background_model = np.zeros((1, 65), np.float64)
    foreground_model = np.zeros((1, 65), np.float64)
    cv2.grabCut(
        source,
        mask,
        None,
        background_model,
        foreground_model,
        8,
        cv2.GC_INIT_WITH_MASK,
    )

    foreground = np.isin(mask, (cv2.GC_FGD, cv2.GC_PR_FGD)).astype(np.uint8)
    count, labels, stats, _ = cv2.connectedComponentsWithStats(foreground, 8)
    if count <= 1:
        raise RuntimeError("Character extraction found no foreground subject")
    subject_label = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    subject = np.where(labels == subject_label, 255, 0).astype(np.uint8)

    contours, _ = cv2.findContours(subject, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    filled = np.zeros_like(subject)
    cv2.drawContours(filled, contours, -1, 255, cv2.FILLED)
    holes = ((filled > 0) & (subject == 0)).astype(np.uint8)
    hole_count, hole_labels, hole_stats, _ = cv2.connectedComponentsWithStats(holes, 8)
    for index in range(1, hole_count):
        if hole_stats[index, cv2.CC_STAT_AREA] < 80:
            subject[hole_labels == index] = 255

    core = cv2.erode(subject, np.ones((3, 3), np.uint8), iterations=1)
    alpha = cv2.GaussianBlur(core, (0, 0), 0.65)
    alpha[core == 255] = 255
    rgba = np.dstack((source, alpha))

    ys, xs = np.where(alpha > 5)
    padding = 24
    left = max(0, int(xs.min()) - padding)
    right = min(width, int(xs.max()) + padding + 1)
    top = max(0, int(ys.min()) - padding)
    bottom = min(height, int(ys.max()) + padding + 1)
    return Image.fromarray(rgba[top:bottom, left:right])


def crop_square(image: Image.Image, center_x: float = 0.5) -> Image.Image:
    """Crop a landscape image to a square around a normalized horizontal focus."""

    side = min(image.size)
    left = round(image.width * center_x - side / 2)
    left = max(0, min(image.width - side, left))
    top = (image.height - side) // 2
    return image.crop((left, top, left + side, top + side))


def prepare_character() -> List[Path]:
    character_dir = MOD_IMAGES / "character"
    charui_dir = MOD_IMAGES / "charui"
    character_dir.mkdir(parents=True, exist_ok=True)
    charui_dir.mkdir(parents=True, exist_ok=True)

    combat = remove_checkerboard()
    combat_path = character_dir / "ninja_battle.png"
    combat.save(combat_path, optimize=True)

    # Use a close upper-body crop for small transparent UI icons.  Cropping from
    # the extracted character avoids baking the source's checkerboard into them.
    width, _ = combat.size
    icon_art = combat.crop(
        (
            round(width * 0.14),
            0,
            round(width * 0.86),
            round(width * 0.72),
        )
    )
    icon_art = icon_art.resize((128, 128), Image.Resampling.LANCZOS)
    icon_path = charui_dir / "character_icon_char_name.png"
    marker_path = charui_dir / "map_marker_char_name.png"
    icon_art.save(icon_path, optimize=True)
    icon_art.save(marker_path, optimize=True)

    poster = Image.open(TEMP / "image.png").convert("RGB")
    poster_path = charui_dir / "ninja_poster.png"
    poster.save(poster_path, optimize=True)

    # The character-select button is a narrow portrait.  This crop keeps the
    # full combat pose while excluding most of the empty city background.
    select_source = poster.crop((230, 0, 867, 941))
    select_icon = ImageOps.fit(
        select_source,
        (132, 195),
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.5),
    )
    select_path = charui_dir / "char_select_char_name.png"
    select_icon.save(select_path, optimize=True)

    locked = ImageOps.grayscale(select_icon).convert("RGB")
    locked = ImageEnhance.Brightness(locked).enhance(0.58)
    locked_path = charui_dir / "char_select_char_name_locked.png"
    locked.save(locked_path, optimize=True)

    # project.godot uses this 420x420 image as the mod icon.  Focus left of
    # center so the Ninja remains readable at thumbnail size.
    mod_icon = crop_square(poster, center_x=0.38).resize(
        (420, 420), Image.Resampling.LANCZOS
    )
    mod_icon_path = ROOT / "NinjaMod" / "mod_image.png"
    mod_icon.save(mod_icon_path, optimize=True)

    return [
        combat_path,
        poster_path,
        icon_path,
        marker_path,
        select_path,
        locked_path,
        mod_icon_path,
    ]


def make_card_contact_sheet() -> Path:
    """Create a lightweight visual-QA sheet in temp (not consumed by the mod)."""

    cards = []
    for slug, *_ in CARD_CROPS:
        cards.append((slug, Image.open(MOD_IMAGES / "card_portraits" / f"{slug}.png")))

    sheet = Image.new("RGB", (750, 456), "#20242b")
    draw = ImageDraw.Draw(sheet)
    for index, (slug, card) in enumerate(cards):
        x = (index % 3) * 250
        y = (index // 3) * 114
        preview = card.resize((150, 114), Image.Resampling.LANCZOS)
        sheet.paste(preview, (x, y))
        draw.text((x + 154, y + 48), slug, fill="white")
    path = TEMP / "card_contact_sheet.png"
    sheet.save(path)
    return path


if __name__ == "__main__":
    written = prepare_new_cards() + prepare_new_powers()

    # Keep the original import workflow available when its legacy sources are
    # present, but do not require those deleted raw files to process new art.
    if (TEMP / "image copy 2.png").exists():
        written.extend(prepare_cards())
        written.append(make_card_contact_sheet())
    if (TEMP / "image copy.png").exists() and (TEMP / "image.png").exists():
        written.extend(prepare_character())

    for path in written:
        print(path.relative_to(ROOT))
