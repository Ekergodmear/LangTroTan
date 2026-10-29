# ForgeGUI integration: investigation and plan (Tàng Kinh Các levels 1–10)

Status (2026-10-28): **investigation only. No asset has been generated and no credits have been spent.** Nothing in the Unity project, gameplay, saves or QA setup was changed.

## 1. What ForgeGUI officially offers (checked on forgegui.com, logged out)

| Question | Finding | Source |
|---|---|---|
| ForgeMCP exists? | Yes, an official product: "ForgeMCP lets you build anywhere. Check out ForgeMCP, it's free!" | forgegui.com home; `/forge-mcp` |
| ForgeMCP supports Claude Code? | **Not confirmed.** The setup page `/forge-mcp` redirects to sign-in. Public pages name only Roblox Studio as an MCP target. | `/forge-mcp` redirects to `/?next=/forge-mcp` |
| ForgeMCP does Text-to-3D with GLB export? | **Not confirmed** (behind sign-in). | — |
| Official API? | The Plans page lists **"MCP & API access"** as an included feature (checkmark) from the Free tier upward. **No public API documentation** (endpoints, auth, job status, download) is linked from `/docs`. | `/plans`, `/docs` |
| GLB export for 3D | Yes: "3D props → glTF (.glb) → Unity, Unreal, UEFN" | `/docs/engine-workflows` |
| 3D generation on the Free plan | No: the Free tier says "Image generations only". 3D needs a paid tier. | `/plans` |
| Plans | Creator 150K credits/mo; Pro 350K credits/mo ($50, or $41.50/mo yearly); Studio 1.25M. Credit cost per generation type is not published ("different generation types cost different amounts"). | `/plans`, `/create` FAQ |
| Licence | "Assets created using ForgeGUI are fully yours." The Free tier lists commercial use as **not included**; Creator and above list "Commercial License". | home FAQ, `/plans` |
| Privacy | Free generations may be added to the public Asset Catalog. Paid-plan generations are private by default. | `/terms` §3 |

**Terms of Service** (last updated 7 Oct 2026), Acceptable Use: prohibits "Engaging in any automated data collection, including scraping, data mining, or harvesting, without our prior written consent", and interfering with the Service's security or performance. The terms say nothing either way about driving the web UI with automation to create your own assets. Account holders are responsible for all activity under their account. Subscription fees are non-refundable.

## 1b. Signed in (your account, Pro plan, 354,891 credits)

- **ForgeMCP is an official remote MCP server: `https://forgegui.com/mcp`.** The "MCP & CLI" page offers:
  - **Claude:** a connector added through claude.ai with OAuth ("No terminal or ForgeGUI API key needed"). It shows "Authorized · awaiting first connection", so permission was already granted from your account.
  - **Codex:** `codex mcp add forge-mcp --url https://forgegui.com/mcp`, then sign in through the link it provides.
  - Roblox Studio steps (not needed here).
- The Claude Code equivalent of the Codex line is `claude mcp add --transport http forge-mcp https://forgegui.com/mcp`, then `/mcp` to sign in through ForgeGUI's own OAuth page. No key ever goes into code or chat.
- **Not yet verified:** which tools ForgeMCP exposes, specifically Text-to-3D with style and rig options, job status, and GLB download. That is only visible once it is connected in a session.
- **Text-to-3D in the web UI** (`/studio?tab=3d`) has:
  - generation mode (3D Model / Asset Pack) and a mesh model selector (Auto)
  - reference image upload
  - Style presets Cartoon / Realistic / **Anime**, plus a custom style reference
  - Polygons and Faces sliders
  - an **Automatically Rig** switch
  - the Prompt box, a generation count (1 of 1, "Each request makes one 3D model") and **Create Now**
  - your previous L5 generation, listed under Generated Assets with Download

## 2. Recommendation

1. **Preferred: official ForgeMCP / API.** The plan includes it, so it is the sanctioned path. It needs one step from you: sign in to forgegui.com (in the built-in browser or your own) so the `/forge-mcp` setup page can be read. If it documents a Claude Code (or generic MCP) setup with Text-to-3D and GLB download, the generator will use it. Any key or OAuth is configured by you through ForgeGUI's own flow and kept out of source code and chat (environment variable or MCP client config).
2. **Fallback: assisted UI automation in your own signed-in session.** Claude drives the `/create` page in the desktop app's built-in browser, with you signed in yourself. That gives one generation per explicit approval, a human pace, no scraping, no token extraction, no hidden or private endpoints, and no workaround for rate limits or bot checks. If ForgeGUI shows a CAPTCHA or a limit, the run stops and hands over to you. This is not explicitly prohibited, but not explicitly permitted either. If you want certainty, ask ForgeGUI support (Discord or email) whether assisted browser use is fine. A standalone Playwright script was considered and not chosen: it would need its own logged-in profile and selectors that cannot be written without seeing the signed-in UI. The built-in browser already gives a user-authenticated session with the same checkpoints.
3. **Not doing:** reverse-engineering private endpoints, copying cookies or tokens, bypassing bot protection, or batch-generating.

## 3. Manifest and tooling

- `Tools/ForgeGUI/asset_manifest.json`: 60 entries (6 families × 10 levels). It supersedes the earlier 10-entry scripture manifest. Fields and statuses are described in `docs/ASSET_PRODUCTION_PLAN.md`. The approved architecture text is still missing, so generation is blocked until it is supplied.
- `Tools/ForgeGUI/validate_glb.py` (Blender 4.5, read-only): mesh, triangle, material and texture counts, rig and animation presence, bounds, and checks (rig off, under 60k triangles, square-ish footprint). Writes a JSON report and front, rear, side, top and 45° previews. It was tested on the L5 GLB: passed, 8,703 triangles.
- Downloads land in `work/forgegui/downloads/`, outside the Unity project. Previews go to `Reports/ForgeGUI/Lxx/`. A model enters `Assets/` only after you approve its previews, using the existing `Tools/Blender/import_glb_asset.py`, a new folder per level, and one `Assign(level, …)` line in `SonMonKitBuilder`.

## 4. Credit estimate

ForgeGUI does **not** show a 3D cost before generating. It isn't on Create Now, in a tooltip, on the Plans page, or in the account menu, and there is no usage history. The balance is 354,891 credits (Pro, 350K per month). A reliable estimate therefore needs one measurement: after you approve level 1, record the balance immediately before and after its single generation. The difference D is the real per-generation cost, and the full plan costs about **9 × D × (1 + retry rate)**. I'll report D and the projected total, and stop for your OK, before level 2. If ForgeMCP is connected first and its tools report a cost, that number is used instead.

## 5. Dry-run (done 2026-10-28, nothing generated)

In the built-in browser, signed in as you, in the Text-to-3D tool:
1. Generation mode 3D Model, mesh model Auto.
2. **Style: Anime** selected (checkmark).
3. **Automatically Rig: off.** Its switch shows the "off" look; on this site "on" has the dot on the left, as the Dark Mode switch does.
4. **Level-1 prompt** (manifest prefix + level 1 + suffix) entered in the Prompt box.
5. 1 of 1 generations queued. **Create Now was not clicked.** The credit balance is unchanged.

Evidence: `Reports/ForgeGUI/dry-run-L01/`: `0-forgemcp-setup-guide`, `1-style-anime-selected`, `2-rig-off-prompt-entered-create-not-clicked`, `3-overview-configured`, `4-pro-plan-credits`.

Note: the level-1 prompt is still the draft from the manifest. Replace it with your approved design before the real generation.
