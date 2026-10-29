"""Cycle QA for the Level 6 spirit VFX captures: builds looping GIFs from frame folders and measures temporal
smoothness (frame-to-frame mean absolute difference, its largest spike relative to the median, and the seam between
the last frame and the first frame of the loop, which must look like any other step).

Usage: python Tools/ShaderGraph/cycle_report.py <frames dir> <gif out> <fps> [seam]
  seam: the folder holds frame 0 .. N where frame N is frame 0 + one full loop (compare N with 0, loop on 0..N-1)
"""
import os
import sys

import numpy as np
from PIL import Image

src, gif, fps = sys.argv[1], sys.argv[2], float(sys.argv[3])
seam = len(sys.argv) > 4 and sys.argv[4] == "seam"
files = sorted(f for f in os.listdir(src) if f.endswith(".png"))
frames = [np.asarray(Image.open(os.path.join(src, f)).convert("RGB"), np.float32) for f in files]
diffs = [float(np.abs(frames[i + 1] - frames[i]).mean()) for i in range(len(frames) - 1)]
loop = frames[:-1] if seam else frames
med = float(np.median(diffs))
print(f"frames={len(frames)} step_mean_abs_diff median={med:.3f} max={max(diffs):.3f} at={int(np.argmax(diffs))} "
      f"max/median={max(diffs) / max(med, 1e-6):.2f}")
if seam:
    s = float(np.abs(frames[-1] - frames[0]).mean())
    wrap = float(np.abs(frames[-2] - frames[0]).mean())
    print(f"seam: frame_last_vs_frame0={s:.3f} (identical phase, should be ~ particle randomness only) "
          f"wrap_step(frame_{len(frames) - 2}->frame0)={wrap:.3f} vs median step {med:.3f}")
imgs = [Image.fromarray(f.astype(np.uint8)).resize((640, 360), Image.LANCZOS) for f in loop]
imgs[0].save(gif, save_all=True, append_images=imgs[1:], duration=int(1000 / fps), loop=0, optimize=True)
print("wrote", gif, os.path.getsize(gif) // 1024, "KB")
