---
name: servercharacters-debug-tracing
description: Use when changing or debugging character authority, profile save/load, login, snapshot, restore, recovery, or anti-cheat flows in ServerCharacters. Requires explicit step-by-step logging at every important decision and file operation.
---

# ServerCharacters Debug Tracing

Use this skill for any task that touches:

- character authority
- login/join profile flow
- profile save/load
- snapshot update or restore
- recovery / backup paths
- anti-cheat enforcement tied to character state

## Rule

Always leave a traceable log path through the flow.

For each important step, add or preserve logs for:

1. who
2. what file/profile
3. what decision
4. what result

## Minimum Logging Standard

For any modified flow, log these phases where applicable:

- connection start
- Steam/Xbox ID resolution
- selected authoritative profile filename
- file exists / file missing
- file read start
- file read success / failure
- decode start
- decode success / failure
- overwrite / restore decision
- snapshot update decision
- snapshot skipped decision
- save start
- save success / failure
- disconnect / recovery decision

## Preferred Log Shape

Keep logs compact and consistent. Prefer one-line factual logs.

Include as many of these fields as are available:

- phase
- player id
- peer / host
- profile name
- authoritative filename
- source path
- destination path
- decision
- result

Example shapes:

```csharp
Utils.Log($"[join] player={playerId} profile={profileName} authoritativeFile={authoritativeFilename} decision=send_server_profile");
Utils.Log($"[snapshot] player={playerId} file={authoritativeFilename} decision=skip_update reason=not_authoritative_session");
Utils.Log($"[restore] player={playerId} source={snapshotPath} target={livePath} result=success");
Utils.Log($"[decode] player={playerId} file={authoritativeFilename} result=failure");
```

## Required Behavior When Editing

Before editing a relevant flow:

- identify the exact entry point
- identify the exact file operation points
- identify the exact branch decisions

After editing:

- ensure logs exist for every new branch that can change authority, file choice, save target, or restore target
- do not remove existing useful logs unless replacing them with better structured logs
- prefer logging the chosen file/path over inferred descriptions

## What To Prioritize

Log decisions that help explain:

- why a player got a specific profile
- why a save was accepted or skipped
- why a snapshot was updated or ignored
- why a restore happened or did not happen
- why a disconnect path changed player state

## What To Avoid

- vague logs like `something failed`
- logs without player/profile context
- logs that only say success without saying which file/profile was used
- excessive spam inside per-frame loops unless rate-limited

## Fast Checklist

For any character/profile task, verify:

- can I tell which profile file was chosen?
- can I tell why that file was chosen?
- can I tell whether decode succeeded?
- can I tell whether save/restore/snapshot happened?
- can I tell why a branch was skipped?

If not, add logs.
