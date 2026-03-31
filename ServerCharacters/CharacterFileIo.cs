using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using HarmonyLib;
using JetBrains.Annotations;

namespace ServerCharacters;

/// <summary>Atomic character-file writes, .bak via File.Replace, load fallback, trailing debounce for .fch (5s per file).</summary>
internal static class CharacterFileIo
{
	internal const int MinProfileBytes = 32;
	private static readonly TimeSpan DebounceInterval = TimeSpan.FromSeconds(5);

	private static readonly object DebounceRegistryLock = new();
	private static readonly Dictionary<string, DebounceEntry> DebounceByPath = new(StringComparer.OrdinalIgnoreCase);

	private static readonly object IoLockRegistryLock = new();
	private static readonly Dictionary<string, object> IoLockByPath = new(StringComparer.OrdinalIgnoreCase);

	private sealed class DebounceEntry
	{
		internal readonly object IoLock = new();
		internal byte[]? PendingBytes;
		internal Timer? Timer;
	}

	private static DebounceEntry GetDebounceEntry(string normalizedPath)
	{
		lock (DebounceRegistryLock)
		{
			if (!DebounceByPath.TryGetValue(normalizedPath, out DebounceEntry? e))
			{
				e = new DebounceEntry();
				DebounceByPath[normalizedPath] = e;
			}

			return e;
		}
	}

	private static object GetIoLock(string normalizedPath)
	{
		lock (IoLockRegistryLock)
		{
			if (!IoLockByPath.TryGetValue(normalizedPath, out object? o))
			{
				o = new object();
				IoLockByPath[normalizedPath] = o;
			}

			return o;
		}
	}

	internal static string NormalizePath(string path) => Path.GetFullPath(path);

	internal static bool IsManagedCharacterPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		try
		{
			string full = NormalizePath(path);
			string root = NormalizePath(Utils.CharacterSavePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			bool under = full.Equals(root, StringComparison.OrdinalIgnoreCase) || full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || full.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
			if (!under)
			{
				return false;
			}

			return full.EndsWith(".fch", StringComparison.OrdinalIgnoreCase)
				|| full.EndsWith(".fch.signature", StringComparison.OrdinalIgnoreCase)
				|| full.EndsWith(".fch.serverbackup", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsManagedMainFchPath(string path) =>
		IsManagedCharacterPath(path) && path.EndsWith(".fch", StringComparison.OrdinalIgnoreCase)
		&& !path.EndsWith(".fch.signature", StringComparison.OrdinalIgnoreCase)
		&& !path.EndsWith(".fch.serverbackup", StringComparison.OrdinalIgnoreCase);

	internal static bool IsValidPayload(byte[]? data) => data is { Length: >= MinProfileBytes };

	internal static byte[]? ReadMainOrBak(string path)
	{
		string full = NormalizePath(path);
		byte[]? main = TryReadValidBytes(full);
		if (main != null)
		{
			return main;
		}

		return TryReadValidBytes(full + ".bak");
	}

	private static byte[]? TryReadValidBytes(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}

			byte[] data = ReadAllBytesRaw(path);
			if (!IsValidPayload(data))
			{
				return null;
			}

			return data;
		}
		catch
		{
			return null;
		}
	}

	internal static byte[] ReadAllBytesRaw(string path)
	{
		using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		if (fs.Length == 0 || fs.Length < MinProfileBytes)
		{
			return Array.Empty<byte>();
		}

		int len = (int)Math.Min(int.MaxValue, fs.Length);
		byte[] buffer = new byte[len];
		int read = fs.Read(buffer, 0, len);
		if (read != len)
		{
			Array.Resize(ref buffer, read);
		}

		return buffer;
	}

	internal static void WriteAtomicImmediate(string path, byte[] bytes)
	{
		string full = NormalizePath(path);
		object ioLock = GetIoLock(full);
		lock (ioLock)
		{
			WriteAtomicUnlocked(full, bytes);
		}
	}

	internal static void WriteDebounced(string path, byte[] bytes)
	{
		string full = NormalizePath(path);
		DebounceEntry entry = GetDebounceEntry(full);
		lock (entry.IoLock)
		{
			entry.PendingBytes = bytes;
			entry.Timer?.Dispose();
			entry.Timer = new Timer(_ => FlushDebounced(full), null, DebounceInterval, Timeout.Infinite);
		}
	}

	private static void FlushDebounced(string normalizedPath)
	{
		DebounceEntry entry = GetDebounceEntry(normalizedPath);
		byte[]? toWrite;
		lock (entry.IoLock)
		{
			toWrite = entry.PendingBytes;
			entry.Timer?.Dispose();
			entry.Timer = null;
			entry.PendingBytes = null;
		}

		if (toWrite == null)
		{
			return;
		}

		object ioLock = GetIoLock(normalizedPath);
		lock (ioLock)
		{
			WriteAtomicUnlocked(normalizedPath, toWrite);
		}
	}

	internal static void FlushAllPendingSaves()
	{
		List<KeyValuePair<string, DebounceEntry>> snapshot;
		lock (DebounceRegistryLock)
		{
			snapshot = new List<KeyValuePair<string, DebounceEntry>>(DebounceByPath);
		}

		foreach (KeyValuePair<string, DebounceEntry> kv in snapshot)
		{
			FlushDebounced(kv.Key);
		}
	}

	private static void WriteAtomicUnlocked(string fullPath, byte[] bytes)
	{
		string? dir = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}

		string temp = Path.Combine(dir ?? ".", ".w_" + Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			using (FileStream fs = new(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				fs.Write(bytes, 0, bytes.Length);
			}

			string bak = fullPath + ".bak";
			if (File.Exists(fullPath))
			{
				File.Replace(temp, fullPath, bak, true);
			}
			else
			{
				File.Move(temp, fullPath);
			}
		}
		finally
		{
			if (File.Exists(temp))
			{
				try
				{
					File.Delete(temp);
				}
				catch
				{
					// ignored
				}
			}
		}
	}

	[HarmonyPatch(typeof(File), nameof(File.WriteAllBytes), new[] { typeof(string), typeof(byte[]) })]
	private static class PatchFileWriteAllBytes
	{
		[UsedImplicitly]
		private static bool Prefix(string path, byte[] bytes)
		{
			if (!IsManagedCharacterPath(path))
			{
				return true;
			}

			// Host / single-player / menu: write now so server backup Postfix and shutdown see current data.
			// Debounce only remote clients (local .fch still written while connected).
			bool writeNow = ZNet.instance == null || ZNet.instance.IsServer();
			if (IsManagedMainFchPath(path) && !writeNow)
			{
				WriteDebounced(path, bytes);
			}
			else
			{
				WriteAtomicImmediate(path, bytes);
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(File), nameof(File.ReadAllBytes), new[] { typeof(string) })]
	private static class PatchFileReadAllBytes
	{
		[UsedImplicitly]
		private static bool Prefix(string path, ref byte[] __result)
		{
			if (!IsManagedMainFchPath(path))
			{
				return true;
			}

			try
			{
				byte[]? data = ReadMainOrBak(path);
				__result = data ?? Array.Empty<byte>();
			}
			catch
			{
				__result = Array.Empty<byte>();
			}

			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.LoadPlayerDataFromDisk))]
	private static class PatchLoadPlayerDataFromDisk
	{
		[UsedImplicitly]
		private static bool Prefix(PlayerProfile __instance, ref ZPackage __result)
		{
			if (__instance.m_fileSource != FileHelpers.FileSource.Local)
			{
				return true;
			}

			string folder = PlayerProfile.GetCharacterFolderPath(__instance.m_fileSource);
			string path = folder + __instance.m_filename + ".fch";
			if (!IsManagedMainFchPath(path))
			{
				return true;
			}

			try
			{
				byte[]? data = ReadMainOrBak(path);
				if (!IsValidPayload(data))
				{
					__result = null!;
					return false;
				}

				try
				{
					__result = new ZPackage(data);
				}
				catch
				{
					__result = null!;
				}

				return false;
			}
			catch
			{
				__result = null!;
				return false;
			}
		}
	}
}
