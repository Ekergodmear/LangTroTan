// Delete only the isolated full-port QA game so unityMongoQa.mjs reseeds it on its next start.
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
const backend = 'C:/Users/Admin/Documents/Codex/tu-tien-port/backend/';
const {MongoClient} = await import(pathToFileURL(backend + 'node_modules/mongodb/lib/index.js').href);
const {createRepositories} = await import(pathToFileURL(backend + 'server/repositories/index.js').href);
const {deleteEntireGame} = await import(pathToFileURL(backend + 'server/state/deleteGame.js').href);
const uri = 'mongodb://127.0.0.1:27028';
assert.ok(uri.endsWith(':27028'));
const client = new MongoClient(uri, {serverSelectionTimeoutMS: 5000});
await client.connect();
const db = client.db('tu_tien_unity_port_qa');
assert.equal(db.databaseName, 'tu_tien_unity_port_qa');
await deleteEntireGame(createRepositories(db), 'unity-port-qa');
console.log('Only unity-port-qa QA game reset on 27028/tu_tien_unity_port_qa. No player database accessed.');
await client.close();
