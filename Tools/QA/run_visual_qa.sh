#!/usr/bin/env bash
# Reset the isolated visual-slice QA fixture, restart its server on 8901, copy the latest
# Builds/VisualSlice into work/claude-audit/<label>/a/b/Build and run the visual regression there,
# so outputs/VisualSlice evidence from earlier sessions is never overwritten.
set -u
label="$1"
ROOT="C:/Users/Admin/Documents/Codex/2026-10-25/referenced-chatgpt-conversation-this-is-an"
AUDIT="$ROOT/work/claude-audit/$label"
NODE="C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe"
BACKEND="C:/Users/Admin/Documents/Codex/tu-tien-port/backend"

if ! netstat -ano | grep -q "127.0.0.1:27029 .*LISTENING"; then  # isolated visual-QA Mongo only
  nohup "/c/Program Files/MongoDB/Server/8.2/bin/mongod.exe" --dbpath "$ROOT/work/visual-qa/mongo" --bind_ip 127.0.0.1 --port 27029 \
    --logpath "$ROOT/work/visual-qa/mongo.log" --logappend >/dev/null 2>&1 &
  for i in $(seq 1 60); do netstat -ano | grep -q "127.0.0.1:27029 .*LISTENING" && break; sleep 0.5; done
fi
pid=$(netstat -ano | grep "127.0.0.1:8901 .*LISTENING" | awk '{print $5}' | head -1)
[ -n "$pid" ] && taskkill //PID "$pid" //F >/dev/null
(cd "$BACKEND" && "$NODE" resetVisualQaFixture.mjs) || exit 1
(cd "$BACKEND" && nohup "$NODE" visualQaServer.mjs > "$ROOT/work/claude-audit/qa-server.log" 2>&1 &)
wait_server() {
  for i in $(seq 1 60); do
    curl -s -m 2 -o /dev/null -w "%{http_code}" http://127.0.0.1:8901/api/games/visual-slice/state | grep -q 200 && break
    sleep 0.5
  done
}
wait_server

# Optional: TIER_LEVEL=<n> seeds the isolated fixture's Tàng Kinh Các level, then restarts the QA server so
# the game loads it from the save (tests loading saved buildings at higher levels). TIER_UPGRADE_IN=<s> also
# leaves a real upgrade in flight that the server completes <s> seconds after seeding.
if [ -n "${TIER_LEVEL:-}" ]; then
  pid=$(netstat -ano | grep "127.0.0.1:8901 .*LISTENING" | awk '{print $5}' | head -1)
  [ -n "$pid" ] && taskkill //PID "$pid" //F >/dev/null
  sleep 1
  "$NODE" "$ROOT/work/claude-audit/tools/seedTierFixture.mjs" "$TIER_LEVEL" ${TIER_UPGRADE_IN:-} || exit 1
  (cd "$BACKEND" && nohup "$NODE" visualQaServer.mjs > "$ROOT/work/claude-audit/qa-server.log" 2>&1 &)
  wait_server
fi

rm -rf "$AUDIT" && mkdir -p "$AUDIT/a/b"
cp -r "$ROOT/outputs/LangTroTan/Builds/VisualSlice" "$AUDIT/a/b/Build"
rm -rf "$AUDIT/a/b/Build/VisualQaJournal"
(cd "$AUDIT/a/b/Build" && timeout 600 ./LangTroTan.exe -visual-qa -visual-regression ${PLAYER_ARGS:-} -force-d3d11 -logFile "$AUDIT/player.log" >/dev/null 2>&1)
echo "player exit=$?"
cat "$AUDIT/VisualSlice/After/regression.txt"
grep -h "VISUAL_QA_RAYCAST" "$AUDIT/player.log"
