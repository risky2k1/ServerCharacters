---
name: servercharacters
description: "Skill for the ServerCharacters area of ServerCharacters. 84 symbols across 6 files."
---

# ServerCharacters

84 symbols | 6 files | Cohesion: 69%

## When to Use

- Working with code in `ServerCharacters/`
- Understanding how StartServer, IsServerCharactersFilePattern, GetPlayerID work
- Modifying servercharacters-related functionality

## Key Files

| File | Symbols |
|------|---------|
| `ServerCharacters/ServerSide.cs` | isAdmin, onGiveItem, onGetPlayerPos, onSendOwnPos, onResetSkill (+27) |
| `ServerCharacters/WebInterfaceAPI.cs` | StartServer, GetPlayerList, SendIngameMessage, KickPlayer, SaveWorld (+11) |
| `ServerCharacters/ClientSide.cs` | onReceivedProfile, cleanEmergencyBackup, PlayerSnapshot, snapShotProfile, Prefix (+7) |
| `ServerCharacters/Utils.cs` | IsServerCharactersFilePattern, GetPlayerID, fromPeer, Log, loadProfile (+5) |
| `ServerCharacters/ServerCharacters.cs` | Initialize, toggleMaintenanceMode, FixedUpdate, config, ConfigurationManagerAttributes (+3) |
| `ServerCharacters/Shared.cs` | Prefix, CharacterNameIsForbidden, sendCompressedDataToPeer, waitForQueue, SendPackage (+1) |

## Entry Points

Start here when exploring this area:

- **`StartServer`** (Method) — `ServerCharacters/WebInterfaceAPI.cs:22`
- **`IsServerCharactersFilePattern`** (Method) — `ServerCharacters/Utils.cs:54`
- **`GetPlayerID`** (Method) — `ServerCharacters/Utils.cs:122`
- **`onGiveItem`** (Method) — `ServerCharacters/ServerSide.cs:194`
- **`onResetSkill`** (Method) — `ServerCharacters/ServerSide.cs:253`

## Key Symbols

| Symbol | Type | File | Line |
|--------|------|------|------|
| `StartServer` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 22 |
| `IsServerCharactersFilePattern` | Method | `ServerCharacters/Utils.cs` | 54 |
| `GetPlayerID` | Method | `ServerCharacters/Utils.cs` | 122 |
| `onGiveItem` | Method | `ServerCharacters/ServerSide.cs` | 194 |
| `onResetSkill` | Method | `ServerCharacters/ServerSide.cs` | 253 |
| `onRaiseSkill` | Method | `ServerCharacters/ServerSide.cs` | 303 |
| `GetHostName` | Method | `ServerCharacters/ServerSide.cs` | 406 |
| `ConsumePlayerSaveUntilSkills` | Method | `ServerCharacters/ServerSide.cs` | 680 |
| `Initialize` | Method | `ServerCharacters/ServerCharacters.cs` | 142 |
| `fromPeer` | Method | `ServerCharacters/Utils.cs` | 61 |
| `ConsumePlayerSaveUntilInventory` | Method | `ServerCharacters/ServerSide.cs` | 675 |
| `GetPlayerList` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 197 |
| `SendIngameMessage` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 214 |
| `KickPlayer` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 223 |
| `SaveWorld` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 236 |
| `RaiseSkill` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 241 |
| `ResetSkill` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 246 |
| `GiveItem` | Method | `ServerCharacters/WebInterfaceAPI.cs` | 251 |
| `Log` | Method | `ServerCharacters/Utils.cs` | 47 |
| `CharacterNameIsForbidden` | Method | `ServerCharacters/Shared.cs` | 193 |

## Execution Flows

| Flow | Type | Steps |
|------|------|-------|
| `RaiseSkill → GetHostName` | cross_community | 4 |
| `ResetSkill → GetHostName` | cross_community | 4 |
| `GiveItem → GetHostName` | cross_community | 4 |
| `Server → SendMessage` | intra_community | 4 |
| `Postfix → GetHostName` | cross_community | 3 |
| `OnReceivedSignature → DeriveKey` | intra_community | 3 |
| `Postfix → Log` | cross_community | 3 |
| `Postfix → GetPlayerID` | cross_community | 3 |
| `Postfix → GetHostName` | cross_community | 3 |
| `Prefix → GetSendQueueSize` | cross_community | 3 |

## How to Explore

1. `gitnexus_context({name: "StartServer"})` — see callers and callees
2. `gitnexus_query({query: "servercharacters"})` — find related execution flows
3. Read key files listed above for implementation details
