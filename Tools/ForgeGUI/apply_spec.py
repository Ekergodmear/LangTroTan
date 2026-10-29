"""Copy the approved architecture (docs/APPROVED_ARCHITECTURE_SPEC.md) verbatim into Tools/ForgeGUI/asset_manifest.json
and compose each level's ForgeGUI prompt from the Art Bible template.

Only spec-derived fields are written (names, themes, approved description, prompt). Production fields (job ids,
credits, files, review status) are left untouched, so the script is safe to re-run after the spec changes.
Usage: python Tools/ForgeGUI/apply_spec.py
"""
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPEC = os.path.join(ROOT, "docs", "APPROVED_ARCHITECTURE_SPEC.md")
MANIFEST = os.path.join(ROOT, "Tools", "ForgeGUI", "asset_manifest.json")
# Notes inside a level row that are production remarks, not architecture; kept verbatim in the manifest but not
# sent to the generator.
PROMPT_EXCLUDE = ["The existing ForgeGUI model is a candidate for this level."]
# Size hints in door widths (generators ignore units). Other families are added when their scale plans are approved.
SIZE_HINTS = {}
# Tàng Kinh Các follows the three-stage size policy (docs/SCRIPTURE_THREE_STAGE_POLICY.md). Door-width hints were not
# obeyed (Level 2 asked for 6, produced about 11), so its prompts describe size relative to the previous level instead.
STAGE_SIZE = {"tang_kinh_cac": {
    **{n: "Same overall width as the previous level; grows in height, floors and detail, not in width" for n in (3, 4, 5, 7, 8, 10)},
    2: "A wide hall about twice as wide as level 1",
    6: "Dramatically larger than level 5: about one and a half times as wide, a much bigger complex",
    9: "Dramatically larger than level 8: about one and a half times as wide, a monumental palace complex"}}

spec = open(SPEC, encoding="utf-8").read()
manifest = json.load(open(MANIFEST, encoding="utf-8"))
template = manifest["promptTemplate"]

families = {}
for block in re.split(r"\n# \d+\. ", spec)[1:]:
    server_id = re.search(r"\*\*Server ID:\*\* `([a-z_]+)`", block).group(1)
    footprint = [int(v) for v in re.search(r"\*\*Footprint:\*\* (\d+)×(\d+)", block).groups()]
    rows = re.findall(r"^\| (\d+) \| ([^|]+?) \| ([^|]+?) \|$", block, re.M)
    families[server_id] = {
        "title": block.split("\n", 1)[0].strip(),
        "footprint": footprint,
        "architectureTheme": re.search(r"\*\*Architecture theme:\*\* (.+)", block).group(1).strip(),
        "animationTheme": re.search(r"\*\*Animation theme:\*\* (.+)", block).group(1).strip(),
        "levels": {int(n): (name.strip(), text.strip()) for n, name, text in rows},
    }
assert len(families) == 6, families.keys()
for sid, fam in families.items():
    assert sorted(fam["levels"]) == list(range(1, 11)), (sid, sorted(fam["levels"]))

def size_hint(asset):
    hint = SIZE_HINTS.get(asset["serverBuildingId"], {}).get(asset["level"])
    return f", about {hint[0]:g} main-door widths wide and {hint[1]:g} deep" if hint else ""


def layout(asset, footprint):
    stage = STAGE_SIZE.get(asset["serverBuildingId"], {}).get(asset["level"])
    if stage:
        return f"{stage}. Same door and step size as the previous level"
    return f"Compact layout for a {footprint} square game footprint{size_hint(asset)}"


def continuity(asset):
    # Levels after the first keep the previous level's identity (its approved render is attached as a reference).
    if asset["level"] == 1:
        return ""
    prev = asset["level"] - 1
    return (f"Continuity: same architectural identity, materials, glazed roof tiles, ornament language and door and "
            f"step size as the Level {prev} reference image, clearly larger and more developed than Level {prev}, never smaller. ")


filled = 0
mismatches = []
for asset in manifest["assets"]:
    fam = families[asset["serverBuildingId"]]
    if fam["footprint"] != asset["serverFootprint"]:
        mismatches.append(f'{asset["id"]}: spec footprint {fam["footprint"]} vs server {asset["serverFootprint"]}')
    name, text = fam["levels"][asset["level"]]
    assert text in spec  # verbatim
    prompt_text = text
    for note in PROMPT_EXCLUDE:
        prompt_text = prompt_text.replace(note, "").strip()
    footprint = "x".join(map(str, asset["serverFootprint"]))
    prompt = (
        f'{template["style"]}. {fam["title"]} ({fam["architectureTheme"].rstrip(".").lower()}), '
        f'level {asset["level"]} of 10, "{name}": {prompt_text.rstrip(".")}. '
        f'{layout(asset, footprint)}. '
        f'{continuity(asset)}{template["suffix"]}.'
    )
    if asset.get("promptOverride"):  # user-approved revision (see promptHistory); never overwritten here
        prompt = asset["generationPrompt"]
    asset.update({
        "familyName": fam["title"],
        "levelName": name,
        "architectureTheme": fam["architectureTheme"],
        "animationTheme": fam["animationTheme"],
        "approvedArchitectureDescription": text,
        "specStatus": "approved_2026-10-28",
        "generationPrompt": prompt,
        "promptStatus": asset["promptStatus"] if asset.get("promptOverride") else "composed_from_approved_spec",
    })
    filled += 1

json.dump(manifest, open(MANIFEST, "w", encoding="utf-8"), ensure_ascii=False, indent=2)
print(f"filled {filled} assets; footprint mismatches: {mismatches or 'none'}")
