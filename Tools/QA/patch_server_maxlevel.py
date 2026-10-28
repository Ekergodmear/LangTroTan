"""Server-authoritative max building level 10 for the Unity port backend (tu-tien-port/backend).

Backup taken first: work/claude-audit/backend-before-maxlevel-2026-10-28.zip. Cost formulas unchanged.
"""
import io

B = "C:/Users/Admin/Documents/Codex/tu-tien-port/backend/"


def patch(path, pairs):
    raw = io.open(B + path, encoding="utf-8", newline="").read()
    for a, b in pairs:
        assert raw.count(a) == 1, (path, a[:80], raw.count(a))
        raw = raw.replace(a, b)
    io.open(B + path, "w", encoding="utf-8", newline="").write(raw)


patch("js/data/sectBuildings.js", [
    ("export function upgradeCost(building, currentLevel) {",
     """// Thanh Vân Tông design rule (2026-10-28): every upgradeable building tops out at level 10.
// Enforced by the authoritative server (startUpgrade, spatial_upgrade). Saves already above 10 are
// left untouched (no clamping); they simply cannot upgrade further.
export const MAX_BUILDING_LEVEL = 10;
export const isMaxBuildingLevel = level => Number(level) >= MAX_BUILDING_LEVEL;

export function upgradeCost(building, currentLevel) {"""),
])

patch("js/systems/sect.js", [
    ('import { BUILDINGS, BUILDING_LIST, upgradeCost, upgradeTimeMinutes } from "../data/sectBuildings.js";',
     'import { BUILDINGS, BUILDING_LIST, upgradeCost, upgradeTimeMinutes, MAX_BUILDING_LEVEL } from "../data/sectBuildings.js";'),
    ("""  const b = sect.buildings[buildingId];
  const def = BUILDINGS[buildingId];
  const cost = upgradeCost(def, b.level);
  return sect.resources.linhThach >= cost;""",
     """  const b = sect.buildings[buildingId];
  const def = BUILDINGS[buildingId];
  if (b.level >= MAX_BUILDING_LEVEL) return false;
  const cost = upgradeCost(def, b.level);
  return sect.resources.linhThach >= cost;"""),
    ("""  if (b.upgrading) return { ok: false, reason: "Đang nâng cấp" };
  const cost = upgradeCost(def, b.level);""",
     """  if (b.upgrading) return { ok: false, reason: "Đang nâng cấp" };
  // Checked before any cost is taken: an over-cap request never deducts resources.
  if (b.level >= MAX_BUILDING_LEVEL) return { ok: false, maxLevel: true, reason: `Đã đạt cấp tối đa (${MAX_BUILDING_LEVEL})` };
  const cost = upgradeCost(def, b.level);"""),
    ("""    if (b.upgrading && now >= b.upgrading.finishAt) {
      b.level += 1;
      b.upgrading = null;
      onComplete(id, b.level);
    }""",
     """    if (b.upgrading && now >= b.upgrading.finishAt) {
      if (b.level >= MAX_BUILDING_LEVEL) {
        // Migration rule: an upgrade started before the cap existed must not create level 11+.
        // Void it and refund its full cost; the level itself is never changed.
        const refund = upgradeCost(BUILDINGS[id], b.level);
        sect.resources.linhThach += refund;
        b.upgrading = null;
        onComplete(id, b.level, { voidedAtMaxLevel: true, refund });
        continue;
      }
      b.level += 1;
      b.upgrading = null;
      onComplete(id, b.level);
    }"""),
])

patch("server/unity/spatial.js", [
    ("import {SPATIAL_CATALOG as C} from './spatialCatalog.js';",
     "import {SPATIAL_CATALOG as C} from './spatialCatalog.js';\nimport {MAX_BUILDING_LEVEL} from '../../js/data/sectBuildings.js';"),
    ("if(b.level>=3)fail('Khu phục dịch hiện nâng tối đa cấp 3.');",
     "if(b.level>=MAX_BUILDING_LEVEL)fail('Công trình đã đạt cấp tối đa '+MAX_BUILDING_LEVEL+'.');"),
    ("if(b.upgradeAt&&end>=b.upgradeAt){const before=spatialHp(b);b.level++;",
     "if(b.upgradeAt&&end>=b.upgradeAt&&b.level>=MAX_BUILDING_LEVEL){add(w,'wood',Math.max(20,C[b.type].wood)*b.level);add(w,'stone',Math.max(10,C[b.type].stone)*b.level);b.upgradeAt=0;addLog(w,C[b.type].name+' đã ở cấp tối đa; hoàn vật tư nâng cấp',end);}\n  if(b.upgradeAt&&end>=b.upgradeAt){const before=spatialHp(b);b.level++;"),
])

patch("server/api/routes.js", [
    (":w);}catch(err){next(err);}", ":{...w,maxBuildingLevel:MAX_BUILDING_LEVEL});}catch(err){next(err);}"),
])
raw = io.open(B + "server/api/routes.js", encoding="utf-8", newline="").read()
first_import_end = raw.index("\n", raw.index("import "))
raw = raw[:first_import_end + 1] + 'import { MAX_BUILDING_LEVEL } from "../../js/data/sectBuildings.js";\n' + raw[first_import_end + 1:]
io.open(B + "server/api/routes.js", "w", encoding="utf-8", newline="").write(raw)

# Projected Đại Điện UI (server renders the original web panels for Unity): show MAX instead of an upgrade button.
patch("js/ui/renderPanels.js", [
    ("""  const action = st.upgrading
    ? `<p class="flavor">Đang nâng cấp... còn ${timeLeft(st.upgrading.finishAt)}</p>`""",
     """  const action = st.level >= MAX_BUILDING_LEVEL
    ? `<p class="flavor">Đã đạt cấp tối đa (Cấp ${MAX_BUILDING_LEVEL})</p>`
    : st.upgrading
    ? `<p class="flavor">Đang nâng cấp... còn ${timeLeft(st.upgrading.finishAt)}</p>`"""),
    ("""    <div class="panel-subtitle">Cấp ${st.level} · ${b.name}</div>""",
     """    <div class="panel-subtitle">Cấp ${st.level} / ${MAX_BUILDING_LEVEL} · ${b.name}</div>"""),
])
patch("js/ui/renderOperations.js", [
    ("const buildingReason = (s,id) => s.sect.buildings[id].upgrading ? 'Đang nâng cấp' :",
     "const buildingReason = (s,id) => s.sect.buildings[id].level >= MAX_BUILDING_LEVEL ? 'Đã đạt cấp tối đa' : s.sect.buildings[id].upgrading ? 'Đang nâng cấp' :"),
    ("return `<h2>${e(b.name)}</h2>${note(`Cấp hiện tại ${st.level} → cấp ${st.level+1}`)}",
     "if(st.level >= MAX_BUILDING_LEVEL) return `<h2>${e(b.name)}</h2>${note(`Cấp ${st.level} / ${MAX_BUILDING_LEVEL} · Đã đạt cấp tối đa`)}${note(buildingEffects[id])}`;\n  return `<h2>${e(b.name)}</h2>${note(`Cấp hiện tại ${st.level} → cấp ${st.level+1}`)}"),
])
for ui in ("js/ui/renderPanels.js", "js/ui/renderOperations.js"):
    raw = io.open(B + ui, encoding="utf-8", newline="").read()
    if "MAX_BUILDING_LEVEL }" not in raw and "MAX_BUILDING_LEVEL}" not in raw:
        idx = raw.index("upgradeCost")
        line_start = raw.rfind("import", 0, idx)
        line_end = raw.index(";", idx)
        stmt = raw[line_start:line_end]
        assert "sectBuildings" in stmt, (ui, stmt[:120])
        new = stmt.replace("upgradeCost", "upgradeCost, MAX_BUILDING_LEVEL", 1)
        raw = raw[:line_start] + new + raw[line_end:]
        io.open(B + ui, "w", encoding="utf-8", newline="").write(raw)
print("patched")
