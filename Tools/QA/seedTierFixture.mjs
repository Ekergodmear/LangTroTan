// Set the Tàng Kinh Các level in the isolated visual-slice QA game only (port 27029, tu_tien_visual_slice_qa),
// to test loading a saved building at a higher level. Never touches player databases.
// Usage: node seedTierFixture.mjs <level> [upgradeFinishesInSeconds]
// The optional second argument leaves a real server upgrade in flight (finishAt = now + seconds), so the
// server clock completes it while the Unity QA run is watching.
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
const backend = 'C:/Users/Admin/Documents/Codex/tu-tien-port/backend/';
const load = p => import(pathToFileURL(backend + p).href);
const {MongoClient} = await load('node_modules/mongodb/lib/index.js');
const {createRepositories} = await load('server/repositories/index.js');
const {hydrateState} = await load('server/state/hydrate.js');
const {createPersister} = await load('server/state/persist.js');
const level = Number(process.argv[2]);
assert.ok(Number.isInteger(level) && level >= 1 && level <= 20, 'level must be 1..20');
const uri = 'mongodb://127.0.0.1:27029';
assert.ok(uri.endsWith(':27029'));
const client = new MongoClient(uri, {serverSelectionTimeoutMS: 5000});
await client.connect();
const db = client.db('tu_tien_visual_slice_qa');
assert.equal(db.databaseName, 'tu_tien_visual_slice_qa');
const repos = createRepositories(db);
const gameId = 'visual-slice';
const persister = createPersister(repos, gameId);
await persister.seedFromState(await hydrateState(repos, gameId));
const state = await hydrateState(repos, gameId);
assert.ok(state.sect.buildings.tang_kinh_cac, 'fixture has Tàng Kinh Các');
state.sect.buildings.tang_kinh_cac.level = level;
const upgradeIn = Number(process.argv[3] || 0);
state.sect.buildings.tang_kinh_cac.upgrading = upgradeIn > 0 ? {finishAt: Date.now() + upgradeIn * 1000} : null;
await persister.flush(state);
const check = await hydrateState(repos, gameId);
assert.equal(check.sect.buildings.tang_kinh_cac.level, level);
console.log(`QA fixture visual-slice: tang_kinh_cac level ${level}${upgradeIn > 0 ? `, upgrade finishing in ${upgradeIn}s` : ''} (isolated 27029 only)`);
await client.close();
