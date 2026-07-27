"""Finalize the generated Black Knight map and top-bar artwork.

The image-generation workflow produces large alpha PNGs.  This script keeps the
source composition intact, scales it to the sizes used by the game UI, and
creates the silhouette-only outline textures expected by the native boss-node
and top-bar controls.
"""

import argparse
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter


ROOT = Path(__file__).resolve().parents[2]


def finalize(source: Path, size: int, outline_width: int) -> Path:
    image = Image.open(source).convert("RGBA")
    image = image.resize((size, size), Image.Resampling.LANCZOS)
    image.save(source, optimize=True)

    alpha = image.getchannel("A")
    expanded = alpha.filter(ImageFilter.MaxFilter(outline_width))
    outline_alpha = ImageChops.subtract(expanded, alpha)
    outline = Image.new("RGBA", image.size, (255, 255, 255, 0))
    outline.putalpha(outline_alpha)

    outline_path = source.with_name(f"{source.stem}_outline.png")
    outline.save(outline_path, optimize=True)
    return outline_path


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--asset",
        choices=("all", "map", "topbar"),
        default="all",
        help="Finalize all generated UI assets or only one asset family.",
    )
    args = parser.parse_args()

    assets = {
        "map": (
            ROOT / "NinjaMod" / "images" / "map" / "blackknight_map_node.png",
            768,
            15,
        ),
        "topbar": (
            ROOT / "NinjaMod" / "images" / "ui" / "blackknight_topbar.png",
            256,
            9,
        ),
    }

    selected = assets.items() if args.asset == "all" else ((args.asset, assets[args.asset]),)
    for _, (path, size, outline_width) in selected:
        if not path.exists():
            raise FileNotFoundError(path)
        outline_path = finalize(path, size, outline_width)
        print(path.relative_to(ROOT))
        print(outline_path.relative_to(ROOT))
