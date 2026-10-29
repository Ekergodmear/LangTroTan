"""Create Tools/ForgeGUI/asset_manifest.json: 6 building families x 10 levels.

Run once to scaffold. Afterwards the manifest is the editable source of truth; re-running refuses to overwrite it
unless --force is given, so recorded jobs, credits and review statuses are never lost.
Usage: python Tools/ForgeGUI/build_manifest.py [--force]
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "asset_manifest.json")

# Server-verified (js/data/sectBuildings.js, server/unity/spatial.js core list, spatialCatalog.js).
FAMILIES = [
    # order = production order agreed with the user
    ("scripture", "tang_kinh_cac", "Tàng Kinh Các", "Scripture Pavilion", [3, 3], "ScripturePavilion"),
    ("hall", "chanh_dien", "Chánh Điện", "Main Hall", [4, 4], "MainHall"),
    ("alchemy", "luyen_dan_duong", "Luyện Đan Đường", "Alchemy Hall", [3, 3], "AlchemyHall"),
    ("forge", "luyen_khi_dien", "Luyện Khí Điện", "Artifact Forging Hall", [3, 3], "ForgingHall"),
    ("barracks", "dien_vo_truong", "Diễn Võ Trường", "Martial Training Ground", [3, 3], "TrainingGround"),
    ("spiritmine", "linh_dien_linh_mo", "Linh Điền & Linh Mỏ", "Spirit Fields & Mines", [2, 2], "SpiritFieldsMines"),
]

ART_BIBLE_STYLE = (
    "Premium stylized anime Xianxia Chinese fantasy architecture, game-ready 3D building, full 360-degree finished "
    "geometry, jade-teal glazed curved tile roofs with deep upswept eaves, deep red lacquered round timber pillars, "
    "carved dougong brackets, ivory-white stone foundation with steps, restrained antique gold ornaments, turquoise "
    "jade details, elegant cloud and dragon relief motifs, detailed lattice doors and windows, carved balcony railings, "
    "believable human-scale proportions"
)
TECH_SUFFIX = (
    "single standalone building centred on a square footprint, entrance and stairs facing the front and unobstructed, "
    "static architecture only, no particles, no light trails, no characters, no terrain, no text"
)

LEVEL5_EXISTING = {
    "sourceGLB": "C:/Users/Admin/Downloads/a-highly-detailed-premium-stylized-anime-xianxia-3d-architec.glb",
    "sha256": "7a2469849b0b82fb9a56098b78788858c428b0f7dfb91f7269205d15dc757017",
    "convertedFBX": "Assets/ThirdParty/ForgeGUI/ScripturePavilion/ForgeGUI_ScripturePavilion.fbx",
    "textureFiles": [
        "Assets/ThirdParty/ForgeGUI/ScripturePavilion/ForgeGUI_ScripturePavilion_albedo.png",
        "Assets/ThirdParty/ForgeGUI/ScripturePavilion/ForgeGUI_ScripturePavilion_normal.png",
        "Assets/ThirdParty/ForgeGUI/ScripturePavilion/ForgeGUI_ScripturePavilion_mask.png",
    ],
    "triangleCount": 8703,
    "unityPrefabPath": "Assets/Art/SonMon/Prefabs/Level_scripture_L05_forgegui.prefab",
}


def band(level):
    return "minimal" if level <= 3 else "moderate" if level <= 6 else "rich" if level <= 9 else "signature"


def entry(fam, level):
    utype, server_id, vi, en, footprint, folder = fam
    e = {
        "id": f"{utype}_L{level:02d}",
        "buildingType": utype,
        "serverBuildingId": server_id,
        "familyName": f"{vi} / {en}",
        "level": level,
        "approvedArchitectureDescription": None,   # paste from the user's approved table; generation is blocked while null
        "specStatus": "missing_from_repo",
        "generationPrompt": None,
        "promptStatus": "not_written",
        "referenceImages": [],
        "forgeSettings": {"tool": "Text to 3D Assets", "mode": "3D Model", "style": "Anime", "automaticallyRig": False, "generations": 1},
        "forgeJobId": None,
        "creditsUsed": None,
        "sourceGLB": None,
        "sha256": None,
        "convertedFBX": None,
        "textureFiles": [],
        "triangleCount": None,
        "bounds": None,
        "serverFootprint": footprint,
        "footprintValidation": None,
        "visualReviewStatus": "planned",
        "reviewNotes": [],
        "unityPrefabPath": None,
        "ambientVfxProfile": f"{utype}_ambient_{band(level)}" + ("_azure_dragon" if utype == "hall" and level == 10 else ""),
        "upgradeVfxProfile": f"{utype}_upgrade",
        "unityAssetFolder": f"Assets/ThirdParty/ForgeGUI/{folder}_L{level:02d}",
    }
    return e


def main():
    if os.path.exists(OUT) and "--force" not in sys.argv:
        sys.exit(f"{OUT} exists; refusing to overwrite recorded production data (use --force to rescaffold).")
    entries = [entry(f, level) for f in FAMILIES for level in range(1, 11)]
    manifest = {
        "schema": 1,
        "artBible": "docs/ART_BIBLE.md",
        "statuses": ["planned", "generating", "generated", "validation_failed", "candidate", "approved", "integrated"],
        "promptTemplate": {"style": ART_BIBLE_STYLE, "suffix": TECH_SUFFIX,
                           "compose": "<style>. <family identity>, level <n> of 10: <approvedArchitectureDescription>. <suffix>."},
        "productionOrder": [f[0] for f in FAMILIES],
        "rules": [
            "Generation is blocked while approvedArchitectureDescription is null.",
            "One level per approved generation; never batch a family.",
            "Original GLB kept unmodified with sha256; only the user sets approved.",
        ],
        "assets": entries,
    }
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)
    print(f"wrote {len(entries)} assets to {OUT}")


if __name__ == "__main__":
    main()
