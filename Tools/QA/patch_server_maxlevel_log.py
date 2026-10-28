"""Follow-up to patch_server_maxlevel.py: the game loop log must not claim "nâng cấp lên cấp 10" when a legacy
in-flight upgrade at the cap was voided and refunded (checkUpgradesComplete passes {voidedAtMaxLevel, refund})."""
import io

p = "C:/Users/Admin/Documents/Codex/tu-tien-port/backend/js/systems/gameLoop.js"
raw = io.open(p, encoding="utf-8", newline="").read()
a = """    checkUpgradesComplete(state.sect, now, (buildingId, level) => {
      pushLog(state, `Công trình đã nâng cấp lên cấp ${level}.`);
      events.push({ type: "building_upgraded", buildingId, level });
    });"""
b = """    checkUpgradesComplete(state.sect, now, (buildingId, level, extra) => {
      if (extra?.voidedAtMaxLevel) {
        pushLog(state, `Công trình đã ở cấp tối đa (${level}); hoàn ${extra.refund} linh thạch của lượt nâng cấp cũ.`);
        events.push({ type: "building_upgrade_voided", buildingId, level, refund: extra.refund });
        return;
      }
      pushLog(state, `Công trình đã nâng cấp lên cấp ${level}.`);
      events.push({ type: "building_upgraded", buildingId, level });
    });"""
assert raw.count(a) == 1
io.open(p, "w", encoding="utf-8", newline="").write(raw.replace(a, b))
print("patched")
