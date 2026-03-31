---
name: servercharacters-harmony-anticheat
description: Guides Harmony patching, anti-cheat design, console command blocking, minimal invasive changes, and Valheim mod exploration for ServerCharacters (BepInEx). Use when patching Valheim, blocking cheats or dev commands, working with Terminal.InputText, PlayerProfile, or ZNet, or tracing load/modify/save flows.
---

# ServerCharacters — Harmony, Anti-Cheat, and Exploration

Concise mindsets for this Valheim mod (C#, Harmony, Unity). Prefer **Prefix** patches; keep changes small and reversible.

## 1. Harmony patch mindset

- **Find the right target**: Patch the actual game/mod method that implements the behavior (use game assemblies + existing `[HarmonyPatch]` in this repo as reference).
- **Prefix**: Run before the original; use to **block** or **short-circuit** (set args / state, then skip original when appropriate).
- **Postfix**: Run after the original; use to **adjust results** or side effects when the original must still run.
- **Replace original logic**: In a Prefix, return `false` (and skip original per Harmony rules for the patch type used) when the goal is to fully prevent the vanilla path—only when you intend to substitute behavior.
- **Isolation**: One concern per patch class or logical group; name patches clearly; avoid entangling unrelated behavior so patches stay easy to disable or revise.

## 2. Anti-cheat mindset

- **Block at input**: Intercept commands, dangerous UI paths, and client-initiated toggles before they take effect.
- **Never trust the client** for authority; assume manipulated clients for anything security-sensitive.
- **Validate on the server** for actions that affect world state, characters, or other players.
- **Prevention over detection**: Prefer stopping cheats from applying over logging-only or after-the-fact bans (proportionate to small-group servers).
- **Keep it simple**: Straight checks and early returns; avoid heavy frameworks unless the project already uses them.

## 3. Command blocking

- **Primary hook**: `Terminal.InputText` (and related terminal/command entry points if the game version splits them).
- **Inspect the input string** (trim, case normalization as needed) **before** command execution.
- **Block known cheat / dev commands** at minimum: `spawn`, `god`, `fly`, `debugmode`, `devcommands` (extend the list if the product owner requires it).
- **User feedback**: On block, show a clear in-game message so players know the command is disabled (not a silent failure unless intentionally specified).
- **Do not allow** enabling debug or developer modes via any patched path you control.

## 4. Minimal invasive changes

- **Prefer small Harmony patches** over rewriting large subsystems.
- **Reuse** existing mod utilities, config, and patterns in this repository before adding parallel systems.
- **Scope**: Implement only the requested behavior; no drive-by refactors or unrelated cleanup in the same change.
- **Compatibility**: Avoid breaking public APIs, config keys, or network message contracts other mods or this mod’s own client/server halves rely on.

## 5. Debug and exploration

- **Harmony usage**: Search for `[HarmonyPatch]` and `HarmonyPatch` to see how this mod already hooks the game.
- **Key types**: `PlayerProfile`, `Terminal`, `ZNet` (and ZNet-style networking in this codebase)—trace how character/world data moves.
- **Data flow**: Follow **load → modify → save** for anything touching persistence or sync.
- **Uncertainty**: Add short-lived, gated logs (e.g. behind config or `#if DEBUG`) to confirm execution order; remove or reduce noise before release if appropriate.

## Special Rule: No Cheat Policy

- All cheat-related commands must be blocked
- devcommands and debugmode must never be enabled
- If unsure whether a command is safe, block it

## Behavior Rule

- Always choose the simplest working solution
- Do not introduce complex systems unless explicitly requested

## Quick checklist

- [ ] Correct method patched; Prefix vs Postfix matches the goal
- [ ] Cheats blocked at input; server validates what matters
- [ ] Terminal (and similar) paths covered; user gets feedback on block
- [ ] Smallest change that works; no unrelated edits
- [ ] Exploration done via patches + key types + optional logs
