// Server regression: max building level 10 (core sect halls and player-built spatial buildings).
// Pure in-memory test against the Unity port backend's real rule modules; touches no database.
// Usage: node Tools/QA/testBuildingMaxLevel.mjs [backendDir]
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';

const backend = (process.argv[2] || 'C:/Users/Admin/Documents/Codex/tu-tien-port/backend').replace(/\\/g, '/') + '/';
const load = p => import(pathToFileURL(backend + p).href);
const {BUILDINGS, upgradeCost, MAX_BUILDING_LEVEL} = await load('js/data/sectBuildings.js');
const {newSect, startUpgrade, checkUpgradesComplete} = await load('js/systems/sect.js');
const {ensureSpatial, tickSpatial, SPATIAL_ACTIONS, SpatialError} = await load('server/unity/spatial.js');

const results = [];
const check = (ok, name) => { results.push(`${ok ? 'PASS' : 'FAIL'} ${name}`); if (!ok) process.exitCode = 1; };

check(MAX_BUILDING_LEVEL === 10, 'MAX_BUILDING_LEVEL is 10');

// ---- core sect hall (Tàng Kinh Các) via startUpgrade / checkUpgradesComplete
function coreAt(level, stones = 1e9) {
  const sect = newSect('QA');
  sect.resources.linhThach = stones;
  sect.buildings.tang_kinh_cac.level = level;
  sect.buildings.tang_kinh_cac.upgrading = null;
  return sect;
}
for (const level of [1, 9]) {
  const sect = coreAt(level);
  const before = sect.resources.linhThach;
  const r = startUpgrade(sect, 'tang_kinh_cac', 0);
  check(r.ok && before - sect.resources.linhThach === upgradeCost(BUILDINGS.tang_kinh_cac, level),
    `core level ${level}: upgrade accepted and charges the unchanged formula cost`);
  checkUpgradesComplete(sect, r.finishAt + 1);
  check(sect.buildings.tang_kinh_cac.level === level + 1, `core level ${level}: completes to level ${level + 1}`);
}
{
  const sect = coreAt(10);
  const before = sect.resources.linhThach;
  const r = startUpgrade(sect, 'tang_kinh_cac', 0);
  check(!r.ok && r.maxLevel === true, 'core level 10: upgrade to 11 rejected as max level');
  check(sect.resources.linhThach === before, 'core level 10: rejected upgrade deducts nothing');
  check(sect.buildings.tang_kinh_cac.level === 10 && !sect.buildings.tang_kinh_cac.upgrading, 'core level 10: level and state unchanged');
}
{
  const sect = coreAt(12);  // legacy save above the new cap
  const before = sect.resources.linhThach;
  const r = startUpgrade(sect, 'tang_kinh_cac', 0);
  check(!r.ok && sect.resources.linhThach === before && sect.buildings.tang_kinh_cac.level === 12,
    'core legacy level 12: not clamped, cannot upgrade, nothing deducted');
}
{
  const sect = coreAt(10);  // legacy in-flight upgrade started before the cap existed
  sect.buildings.tang_kinh_cac.upgrading = {finishAt: 100};
  const before = sect.resources.linhThach;
  let info = null;
  checkUpgradesComplete(sect, 200, (id, level, extra) => { info = extra; });
  check(sect.buildings.tang_kinh_cac.level === 10 && !sect.buildings.tang_kinh_cac.upgrading,
    'core legacy in-flight upgrade at level 10: voided, level stays 10');
  check(sect.resources.linhThach - before === upgradeCost(BUILDINGS.tang_kinh_cac, 10) && info?.voidedAtMaxLevel === true,
    'core legacy in-flight upgrade at level 10: full cost refunded and reported');
}

// ---- player-built spatial building via SPATIAL_ACTIONS.spatial_upgrade
function spatialWith(level) {
  const state = {sect: {...newSect('QA'), name: 'QA'}};
  const w = ensureSpatial(state, 0);
  w.wood = 1e6; w.stone = 1e6; w.people = 40;
  const house = {id: 'building:qa', type: 'house', x: 20, z: 20, rotation: 0, level, hp: 350, complete: true, workers: 0, finishAt: 0, upgradeAt: 0};
  w.buildings.push(house);
  return {state, w, house};
}
for (const level of [1, 9]) {
  const {state, w, house} = spatialWith(level);
  const wood = w.wood;
  const r = SPATIAL_ACTIONS.spatial_upgrade(state, {buildingId: house.id});
  check(r.ok && wood - w.wood === Math.max(20, 40) * level, `spatial level ${level}: upgrade accepted with the unchanged wood cost`);
  tickSpatial(state, house.upgradeAt + 1);
  check(house.level === level + 1, `spatial level ${level}: completes to level ${level + 1}`);
}
{
  const {state, w, house} = spatialWith(10);
  const wood = w.wood, stone = w.stone;
  let error = null;
  try { SPATIAL_ACTIONS.spatial_upgrade(state, {buildingId: house.id}); } catch (e) { error = e; }
  check(error instanceof SpatialError && /tối đa/.test(error.message), 'spatial level 10: upgrade to 11 rejected as max level');
  check(w.wood === wood && w.stone === stone && house.level === 10 && house.upgradeAt === 0, 'spatial level 10: nothing deducted, level unchanged');
}

console.log(results.join('\n'));
console.log(process.exitCode ? 'BUILDING_MAX_LEVEL_FAILED' : 'BUILDING_MAX_LEVEL_OK');
