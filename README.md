# ServerCharacters (Valheim BepInEx Mod)

## Project snapshot

- Type: Valheim server/client sync mod (`BepInEx` + `Harmony`)
- Language: C# (.NET Framework 4.8)
- Main goal: server-authoritative character handling, anti-cheat hardening, and operational safety for dedicated servers
- Current plugin identity:
  - GUID: `org.bepinex.plugins.servercharacters`
  - Name: `Server Characters`
  - Version (code): `1.4.16`

## Code layout

- `ServerCharacters/ServerCharacters.cs`
  - Plugin entrypoint, config registration, `Harmony.PatchAll`, startup initialization
  - Maintenance mode toggles and file watchers
- `ServerCharacters/ServerSide.cs`
  - Server authority logic: receive/sanitize profile, enforce maintenance/single-character rules
  - Admin RPC actions (`RaiseSkill`, `ResetSkill`, `GiveItem`, teleport/summon helpers)
  - Backup management and emergency signature verification
- `ServerCharacters/ClientSide.cs`
  - Client hooks for upload/download character profile
  - Emergency backup/signature handling
  - Custom `ServerCharacters` terminal command group for admins
- `ServerCharacters/Shared.cs`
  - Shared compressed-fragment transport and compatibility checks
  - Version handshake guard (`-ServerCharacters` suffix)
- `ServerCharacters/CharacterFileIo.cs`
  - File safety layer: atomic write (`temp + replace`), `.bak` fallback, per-file lock, debounced saves
  - Harmony patches for managed profile read/write paths
- `ServerCharacters/WebInterfaceAPI.cs`
  - TCP API for server tooling (player/mod list, maintenance message, admin actions)

## Security-relevant behavior (current)

- Server validates incoming player profile bytes and forbidden character names.
- Admin-sensitive RPC methods are guarded by server-side admin checks.
- Compatibility handshake rejects peers without ServerCharacters version suffix.
- Character file I/O includes atomic writes and fallback read from `.bak`.

## Active constraints for contributors

- Prefer Harmony Prefix patches for blocking behavior.
- Keep server authority on dedicated server for security-sensitive rules.
- Use file-based storage only (no DB).
- Keep changes minimal; improve safety/reliability without redesign.

## Build notes

- Solution: `ServerCharacters.sln`
- Project: `ServerCharacters/ServerCharacters.csproj`
- Game assemblies resolved from `$(GamePath)` (Valheim installation)
- Output is copied to `$(GamePath)/BepInEx/plugins/ServerCharacters.dll` via MSBuild target.

## Documentation status

- `README.md` now contains architecture overview.
- `manifest.json` stores machine-readable project context for agents/tools.
- `AGENTS.md` / `CLAUDE.md` contain coding and workflow rules (including GitNexus requirements).
