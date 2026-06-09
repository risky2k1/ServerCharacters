# ServerCharacters Overview

## Purpose

`ServerCharacters` is a Valheim `BepInEx` + `Harmony` mod that moves character ownership away from the client and toward the server.

At this baseline commit, the mod mainly does four things:

1. Stores character data on the server as the authoritative copy.
2. Sends player profile data between client and server during connect/save.
3. Adds some server-admin features around skills, teleport, item giving, and maintenance.
4. Exposes a small TCP web interface for external tools.

This is not a total gameplay overhaul. It is mostly a network/save-authority mod.

## High-Level Design

The codebase is split into three main areas:

- `ServerCharacters/ServerCharacters.cs`
  The plugin entry point. Loads config, applies Harmony patches, initializes folders, starts the web API, and wires maintenance/template file watchers.
- `ServerCharacters/ClientSide.cs`
  Client behavior. Receives authoritative profiles from the server, saves local emergency backups, sends profile/inventory updates back to the server, and adds client console commands.
- `ServerCharacters/ServerSide.cs`
  Server behavior. Accepts incoming profile/inventory data, saves authoritative character files, handles maintenance checks, and serves admin RPC actions.

Supporting files:

- `ServerCharacters/Shared.cs`
  Common network helpers. Compresses and fragments profile payloads, reconstructs them on receive, and patches `PlayerProfile` loading so raw byte arrays can be decoded as profiles.
- `ServerCharacters/Utils.cs`
  Logging, ID normalization, cached profile loading, webhook posting, and save-path helpers.
- `ServerCharacters/WebInterfaceAPI.cs`
  TCP API for external tooling. Can query player/mod state and trigger a few server actions.

## Main Runtime Flow

### 1. Startup

`ServerCharacters.Awake()`:

- registers config
- applies all Harmony patches
- starts file watchers
- ensures a server key exists
- hooks initialization after `FejdStartup.Awake`

`ServerCharacters.Initialize()`:

- creates the character save directory
- migrates legacy character files
- normalizes Steam-prefixed filenames
- preloads existing server character files into cache
- starts the web interface if configured

### 2. Connection Handshake

`Shared` appends `-ServerCharacters` to the Valheim version string.

On server peer-info receive:

- if the connecting client does not have the mod, the server rejects the connection

On new connection:

- server registers custom RPC handlers
- server sends an encryption key exchange package used for emergency backup validation
- client registers its own RPC handlers for profile sync and admin messages

### 3. Authoritative Character Sync

Normal model:

- client has a local profile loaded by the game
- server sends the authoritative `.fch` profile bytes to the client
- client replaces `Game.instance.m_playerProfile` with the server-sent profile
- later saves from the client are forwarded back to the server
- server writes the resulting profile to disk as the authoritative copy

Important detail:

- profile decoding from raw bytes is not done through a clean public API
- instead, `Shared.LoadPlayerProfileFromBytes()` is created by transpiling `PlayerProfile.LoadPlayerFromDisk()`

That means the mod is tightly coupled to Valheim's internal save-loading implementation.

### 4. Emergency Backup Restore

Client-side:

- before certain authoritative saves, the client writes:
  - `<character>.fch.serverbackup`
  - `<character>.fch.signature`

Server-side:

- on reconnect, the client may submit that backup
- server verifies the signature
- if accepted, server restores the backup profile
- inventory may be patched back in from separately tracked inventory bytes

This path exists to recover from failed saves or disconnect edge cases.

### 5. Inventory Handling

The server keeps a temporary `Inventories` map keyed by player identity.

During disconnect or emergency restore:

- the mod may read the inventory section out of a profile byte blob
- then splice a different inventory payload back into the saved profile bytes

This is one of the most fragile parts of the mod, because it depends on byte-level knowledge of Valheim profile layout.

## Extra Features Present In This Baseline

- Maintenance mode with countdown and admin bypass
- Discord webhook messages
- Optional first-login announcement
- Optional poison debuff persistence
- Single-character mode
- Hardcore mode that deletes/archives on death
- Admin RPC actions:
  - raise skill
  - reset skill
  - give item
  - teleport / summon
- TCP web interface for external dashboards/tools

## File Format and Storage Model

Character files are regular Valheim `.fch` files stored under:

- `PlayerProfile.GetCharacterFolderPath(FileHelpers.FileSource.Local)`

Naming convention used by the mod:

- `<PlatformOrSteamPrefix>_<PlayerId>_<charactername>.fch`
- example shape: `Steam_7656119..._alice.fch`

The server treats those files as the source of truth when server-authoritative mode is active.

## Technical Constraints

This code is strongly tied to Valheim internals:

- Harmony transpilers patch internal game methods
- profile parsing depends on the exact `PlayerProfile` load flow
- inventory patching depends on the exact `Player.Load()` serialization order
- client/server behavior assumes Steam backend

Because of that, game updates can silently break save decoding even if the mod still compiles.

## What The Mod Is Not

At this baseline, the mod is not yet a strict "no-cheat" mod.

In fact, it still contains admin-oriented features that work through custom RPCs:

- give items
- reset/raise skills
- teleport / summon

So the current baseline is better described as:

"server-authoritative characters with admin tooling"

not

"console/admin commands fully disabled"

## Best Mental Model For Future Work

When modifying this mod, treat it as three coupled systems:

1. save authority
2. network transport for profile bytes
3. byte-level profile/inventory manipulation

If character corruption or item loss happens, the highest-risk areas are usually:

- `Shared.LoadPlayerProfileFromBytes()`
- emergency backup restore flow
- inventory extraction/patching in `ServerSide`
- assumptions about Valheim save layout after a game update

## Suggested Next Step

Before changing behavior, map and verify these specific flows:

1. server sends authoritative profile to client
2. client saves and sends updated profile back
3. server writes `.fch` to disk
4. reconnect path after crash/disconnect
5. disconnect path that patches inventory back into the server save
