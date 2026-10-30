"""Compact Level 6 v4 QA previews for review without downloading the full artifact.

Run by Tools/QA/unity_qa.ps1 (l6-visual) with the QA venv (numpy, Pillow; Tools/QA/requirements-qa.txt):

  python -I Tools/QA/make_previews.py <shots dir> <cycle frames dir> <out dir> [measurements.csv]
                                      [--prefix v4|v5] [--baseline <v4 capture dir>]

Writes to <out dir> (a few MB in total):
  contact_sheet.jpg   every captured view, labelled, 480x270 each
  cycle_phases.jpg    8 moments of the 10 s cycle (ambient, gathering, convergence, release)
  cycle_10s.gif       the whole cycle at 5 fps, 400x225
  key/*.jpg           full-resolution JPEGs of the views needed for visual review
  cycle_stats.txt     frame-to-frame smoothness and loop seam (as Tools/ShaderGraph/cycle_report.py)
  compare_v4_v5.jpg   with --baseline: per view, v4 full | candidate off | candidate full (same cameras)
"""
import os
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw

THUMB = (480, 270)
GIF_SIZE = (400, 225)
KEY_VIEWS = ["full_hero", "full_gameplay_zoom10", "full_gameplay_zoom21_default",
             "full_close_mist", "full_close_formation", "full_close_ornament"]
COMPARE_VIEWS = ["hero", "gameplay_zoom10", "gameplay_zoom21_default", "gameplay_zoom30",
                 "close_mist", "close_formation", "close_ornament", "close_glyphs"]
# cycle frames are captured at 10 FPS: frame index = seconds * 10
PHASES = [(0.0, "0.0 s ambient"), (1.5, "1.5 s ambient"), (3.5, "3.5 s gathering"), (5.0, "5.0 s gathering"),
          (6.5, "6.5 s convergence"), (7.5, "7.5 s convergence"), (8.5, "8.5 s release"), (9.5, "9.5 s release")]


def labelled(img, text):
    img = img.convert("RGB").resize(THUMB, Image.LANCZOS)
    draw = ImageDraw.Draw(img)
    width = int(draw.textlength(text)) + 10
    draw.rectangle([0, 0, width, 16], fill=(0, 0, 0))
    draw.text((5, 2), text, fill=(255, 255, 255))
    return img


def grid(tiles, cols):
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * THUMB[0], rows * THUMB[1]), (32, 32, 32))
    for i, tile in enumerate(tiles):
        sheet.paste(tile, ((i % cols) * THUMB[0], (i // cols) * THUMB[1]))
    return sheet


def option(args, name, default=None):
    if name in args:
        i = args.index(name)
        value = args[i + 1]
        del args[i:i + 2]
        return value
    return default


def compare_sheet(shots, baseline, prefix, out):
    # rows: views; columns: v4 full (committed baseline) | candidate off | candidate full
    tiles = []
    for view in COMPARE_VIEWS:
        for path, label in ((os.path.join(baseline, f"v4_full_{view}.png"), f"v4 full {view}"),
                            (os.path.join(shots, f"{prefix}_off_{view}.png"), f"{prefix} off {view}"),
                            (os.path.join(shots, f"{prefix}_full_{view}.png"), f"{prefix} full {view}")):
            if os.path.exists(path):
                tiles.append(labelled(Image.open(path), label))
            else:
                tiles.append(labelled(Image.new("RGB", THUMB, (32, 32, 32)), label + " (not captured)"))
    grid(tiles, 3).save(os.path.join(out, "compare_v4_v5.jpg"), quality=85)


def main():
    args = sys.argv[1:]
    prefix = option(args, "--prefix", "v4")
    baseline = option(args, "--baseline")
    shots, cycle, out = args[0], args[1], args[2]
    measurements = args[3] if len(args) > 3 else None
    os.makedirs(os.path.join(out, "key"), exist_ok=True)

    names = sorted(f[:-4] for f in os.listdir(shots) if f.endswith(".png"))
    grid([labelled(Image.open(os.path.join(shots, n + ".png")), n) for n in names], 4).save(
        os.path.join(out, "contact_sheet.jpg"), quality=85)
    for n in (f"{prefix}_{v}" for v in KEY_VIEWS):
        if n in names:
            Image.open(os.path.join(shots, n + ".png")).convert("RGB").save(os.path.join(out, "key", n + ".jpg"), quality=88)

    files = sorted(f for f in os.listdir(cycle) if f.startswith("frame_") and f.endswith(".png"))
    frames = [Image.open(os.path.join(cycle, f)).convert("RGB") for f in files]
    if frames:
        tiles = [labelled(frames[min(int(t * 10), len(frames) - 1)], label) for t, label in PHASES]
        grid(tiles, 4).save(os.path.join(out, "cycle_phases.jpg"), quality=85)
        # the last frame is frame 0 one loop later, so the GIF loops on frames 0..N-2
        # one shared 128-colour palette keeps the GIF small and free of per-frame palette flicker
        loop = [f.resize(GIF_SIZE, Image.LANCZOS) for f in frames[:-1:2]]
        palette = loop[len(loop) // 2].quantize(colors=128, method=Image.Quantize.MEDIANCUT)
        loop = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in loop]
        loop[0].save(os.path.join(out, "cycle_10s.gif"), save_all=True, append_images=loop[1:], duration=200, loop=0,
                     optimize=True)
        arrays = [np.asarray(f, np.float32) for f in frames]
        steps = [float(np.abs(arrays[i + 1] - arrays[i]).mean()) for i in range(len(arrays) - 1)]
        median = float(np.median(steps))
        with open(os.path.join(out, "cycle_stats.txt"), "w") as f:
            f.write(f"frames={len(frames)} step_median={median:.3f} step_max={max(steps):.3f} "
                    f"at_frame={int(np.argmax(steps))} max_over_median={max(steps) / max(median, 1e-6):.2f}\n")
            f.write(f"seam last_vs_first={float(np.abs(arrays[-1] - arrays[0]).mean()):.3f}\n")

    if baseline and os.path.isdir(baseline):
        compare_sheet(shots, baseline, prefix, out)

    if measurements and os.path.exists(measurements):
        shutil.copy(measurements, os.path.join(out, "measurements.csv"))
    print("previews written to", out)


if __name__ == "__main__":
    main()
