# Agent Instructions — Unity MR Utility Kit (MRUK) Sample

A Unity sample collection that demonstrates the Meta MR Utility Kit (MRUK) — high-level utilities on top of the Scene API for spatially-aware mixed-reality apps on Meta Quest. Each MRUK feature is its own scene under `Assets/MRUKSamples/`. See the [MR Utility Kit developer docs](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/) and [samples docs](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-samples).

## Source-of-truth files (read these first, do not duplicate their contents in this file)

For setup, build steps, SDK versions, and project layout, read:

- `README.md` — official setup, sample list, and integration guide
- `ProjectSettings/ProjectVersion.txt` — pinned Unity editor version
- `Packages/manifest.json` — Unity package versions (Meta XR Core SDK, MR Utility Kit, URP, OpenXR, etc.)
- `LICENSE` — license terms

## Quest / Horizon-specific notes

- The MR Utility Kit (`com.meta.xr.mrutilitykit`) and Meta XR Core SDK (`com.meta.xr.sdk.core`) versions are pinned together. Keep them in lockstep when upgrading — mismatched versions break MRUK builds.
- Several scenes (QR detection, keyboard tracking, destructible mesh) depend on Scene API features that are device/OS gated. Verify on a real Quest 3 / 3S build before chasing what looks like a logic bug.
- The "Integrate Samples to your own project" flow (copy `Assets/MRUKSamples/` or export a UnityPackage) requires the consuming project to use the same MRUK and Meta XR Core SDK versions.

# Meta Quest tooling

This is a Meta Quest / Horizon OS sample. The bespoke intro above is the source of truth for what this project is and how it's built — use it (and the files it points at) instead of restating facts from memory.

When the user asks anything about Quest device behavior, build / deploy / debug / capture flows, on-device performance, or Horizon OS APIs, reach for these tools instead of generic Unity answers:

- **`hzdb`** — Quest-aware ADB wrapper (device list, install / launch / stop, logs, screenshots, Perfetto traces, on-device docs search). Already wired up as an MCP server via `.mcp.json`, `.vscode/mcp.json`, and `.cursor/mcp.json`. Also runnable directly: `npx -y @meta-quest/hzdb <subcommand>`.
- **Meta Quest Agentic Tools** — the full skill set, including Unity-specific skills: <https://github.com/meta-quest/agentic-tools>. Install per your client (Claude Code: `/plugin install meta-vr@meta-quest`; Gemini CLI: `gemini extensions install https://github.com/meta-quest/agentic-tools`; Cursor / VS Code: install the **Meta Horizon** extension from the Marketplace).

A few behavior expectations:

- **Read this repo's files first.** Before answering anything project-specific, read `README.md` and whichever source-of-truth files the intro above points at. Don't restate their contents in chat — quote or link instead.
- **Use `hzdb` for device-side work.** Anything that touches an attached Quest (install, launch, logs, screenshot, capture, manifest inspection) goes through `hzdb`, not raw `adb`.
- **Check live Horizon OS docs before answering API questions.** `hzdb docs search "..."` queries the live docs; training data on Horizon OS APIs goes stale fast.
- **Don't fabricate SDK / engine versions.** If a version isn't visible in this repo's files, say so rather than guessing.
