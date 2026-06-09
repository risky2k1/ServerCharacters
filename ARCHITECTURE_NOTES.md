# Architecture Notes

## Baseline Summary

At commit `eddac29`, `ServerCharacters` is a mixed-responsibility mod:

- server-authoritative character sync
- emergency character backup/restore
- admin RPC tooling
- maintenance workflow
- web/TCP control surface
- new-character templating

This matters because future "no-cheat" work should not treat the current codebase as a small anti-cheat patch. It is a broader character-management system.

## Core Files

### `ServerCharacters.cs`

Top-level plugin bootstrap:

- defines config
- applies all Harmony patches
- creates file watchers
- migrates/normalizes save files
- preloads cached profiles
- starts the TCP web API
- drives the maintenance countdown in `FixedUpdate()`

### `Shared.cs`

Shared transport and compatibility layer:

- compresses large byte payloads
- fragments payloads into multiple RPC packages
- reassembles fragments on receive
- rejects clients that do not expose `-ServerCharacters` in version string
- transpiles `PlayerProfile.LoadPlayerFromDisk()` into a byte-array loader

This file is part transport, part compatibility shim.

### `ClientSide.cs`

Client responsibilities:

- registers client-side RPC handlers
- receives authoritative profile bytes from server
- swaps `Game.instance.m_playerProfile`
- sends profile data back to server when saving
- creates emergency backup + signature files
- receives admin actions like teleport, item grant, raise/reset skill
- manages first-login template application

### `ServerSide.cs`

Server responsibilities:

- registers server-side RPC handlers
- validates maintenance/admin conditions
- receives and saves authoritative character profiles
- receives inventory snapshots separately
- restores emergency backups
- applies admin operations to both online and offline characters
- sends current authoritative profile to joining players
- writes zip backups on server save

### `WebInterfaceAPI.cs`

Out-of-process control surface:

- starts a TCP listener
- provides player list, mod list, maintenance state, server config
- can send in-game messages, kick players, save world
- can invoke raise skill, reset skill, give item through server code

This is effectively a remote admin API.

## Main Execution Flows

## 1. Plugin Startup

Sequence:

1. `ServerCharacters.Awake()`
2. register config entries
3. `harmony.PatchAll`
4. create file watchers:
   - `maintenance`
   - `CharacterTemplate.yml`
5. `ServerSide.generateServerKey()`
6. patch `FejdStartup.Awake` to call `Initialize()`

`Initialize()` then:

1. optionally starts `WebInterfaceAPI`
2. creates local character directory
3. migrates legacy files
4. prefixes bare Steam IDs with `Steam_`
5. preloads `.fch` profiles into `Utils.Cache.profiles`

## 2. Handshake and Mod Presence Check

The server expects connecting clients to have the mod installed.

Mechanism:

- `GameVersion.ToString()` is patched to append `-ServerCharacters`
- server intercepts `ZNet.RPC_PeerInfo`
- if version string lacks that suffix, the server disconnects the client

Then on `ZNet.OnNewConnection`:

- server registers its custom RPC endpoints
- server sends a key exchange package for backup signature verification
- client registers its own receive handlers

## 3. Join Flow for Authoritative Characters

Server path:

1. peer connects
2. `ServerSide.SendConfigsAfterLogin` buffers socket traffic
3. server loads `<playerId>_<playerName>.fch`
4. if `singleCharacterMode` is on and another character exists for same ID, reject join
5. if not `backupOnlyMode`, send authoritative profile bytes to client
6. release buffered socket traffic

Client path:

1. `ClientSide.onReceivedProfile()` gets authoritative bytes
2. if empty payload:
   - either force user to create a new local character first
   - or mark as new server-side character/template case
3. if payload is valid:
   - decode bytes into `PlayerProfile`
   - reject if corrupted or forbidden name
   - replace `Game.instance.m_playerProfile`
   - delete stale emergency backup files

## 4. Save Flow

Client-side save:

1. Valheim serializes profile to bytes
2. transpiler in `PatchPlayerProfileSave_Client` intercepts byte array write
3. `SaveCharacterToServer()` runs
4. if emergency-backup mode is active:
   - write `.fch.signature`
   - write `.fch.serverbackup`
5. if connected to authoritative server:
   - send profile bytes to server via compressed fragmented RPC

Server-side receive:

1. `ServerSide.onReceivedProfile()` receives raw profile bytes
2. decode bytes using `Shared.LoadPlayerProfileFromBytes()`
3. reject invalid payload or forbidden names
4. create `PlayerProfile` named by server convention
5. save `.fch` on server disk

## 5. Disconnect / Inventory Preservation Flow

The mod separately tracks inventory in `Inventories`.

On disconnect:

1. server looks up cached inventory bytes for the peer
2. server reloads the saved profile from disk
3. `PatchPlayerProfileInventory()` splices inventory bytes back into `m_playerData`
4. profile is saved again
5. original file modification time is restored

Intent:

- preserve latest inventory state even if the profile save and inventory state are slightly out of sync

Risk:

- this flow depends on byte-level offsets in the serialized player payload

## 6. Emergency Backup Restore Flow

Client:

- if `.serverbackup` and `.signature` exist on join, send both to server

Server:

1. verify signature using server key + timestamp-derived AES key
2. decode backup profile bytes
3. ensure target profile already belongs to this server
4. ensure backup timestamp is newer than current server file
5. obtain inventory bytes:
   - from `Inventories`
   - or by reading current saved profile
6. splice inventory into restored profile
7. save restored profile

This path is recovery logic, but it is also one of the highest corruption-risk areas.

## Data Manipulation Techniques

## Transpiled byte decoding

`Shared.LoadPlayerProfileFromBytes()` is not a normal hand-written parser.

Instead, it:

- copies IL from `PlayerProfile.LoadPlayerFromDisk()`
- swaps the disk-read part with `new ZPackage(byte[])`

Result:

- mod reuses game logic to parse profile bytes
- but becomes tightly coupled to internal Valheim implementation details

## Inventory and skill splicing

`ServerSide` extracts and rewrites subsections of `profile.m_playerData` by:

- replaying part of `Player.Load()` to discover offset positions
- loading `Inventory` or `Skills`
- replacing a byte slice inside the serialized payload

This is clever, but fragile.

If Valheim changes serialized ordering or structure:

- profile may still partially load
- inventory/skills may be patched into the wrong byte region
- users can lose items or have corrupted saves

## Feature Clusters

### Character Authority

- authoritative `.fch` on server
- client forced to use server copy
- optional single-character enforcement

### Recovery

- local emergency backup files
- server backup zip archives
- reconnect restore path

### Admin Controls

- raise skill
- reset skill
- give item
- teleport
- summon
- kick
- broadcast message

### Maintenance

- countdown timer
- admin bypass
- webhook notifications
- save world and kick non-admins when maintenance starts

### Template / Onboarding

- YAML-defined starter skills/items/spawn
- optional intro/Valkyrie behavior changes

### Remote API

- TCP listener for dashboards or control tools

## Why This Matters For Rebuild Work

If the goal is a simpler and safer anti-cheat-oriented fork, these are likely candidates to reduce or remove first:

- admin item/skill/teleport RPCs
- web API commands that map to admin powers
- byte-splice flows that are not strictly required
- backup restore complexity that depends on fragile inventory surgery

The pieces most worth preserving are:

- server-authoritative profile ownership
- clean connect-time profile validation
- minimal maintenance behavior

## Immediate Technical Risks

Highest-risk areas in this baseline:

1. `Shared.LoadPlayerProfileFromBytes()`
2. `PatchPlayerProfileInventory()`
3. `PatchPlayerProfileSkills()`
4. disconnect-time inventory rewrite
5. emergency restore path

These are the first places to verify against current Valheim before trusting the mod with real characters.
