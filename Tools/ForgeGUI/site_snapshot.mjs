// Fresh-world spatial snapshot for site-layout planning. Calls the server's own ensureSpatial on an in-memory state
// object (no database, no save is loaded). Usage: node Tools/ForgeGUI/site_snapshot.mjs <backendDir> <out.json>
import {pathToFileURL} from 'node:url';
import {writeFileSync} from 'node:fs';
const [backend, out] = process.argv.slice(2);
const {ensureSpatial} = await import(pathToFileURL(backend + '/server/unity/spatial.js'));
const {SPATIAL_CATALOG: C} = await import(pathToFileURL(backend + '/server/unity/spatialCatalog.js'));
const w = ensureSpatial({createdAt: 0, sect: {buildings: {}}}, 0);
const buildings = w.buildings.map(b => ({id: b.id, type: b.type, x: b.x, z: b.z, w: C[b.type].width, d: C[b.type].depth}));
writeFileSync(out, JSON.stringify({buildings, nodes: w.nodes.map(n => ({kind: n.kind, x: n.x, z: n.z})), buildRadius: 24, exitCorridor: 'two cells wide from (-1,-4) to |x| or |z| >= 23'}, null, 1));
console.log(buildings.length, 'buildings', w.nodes.length, 'nodes');
