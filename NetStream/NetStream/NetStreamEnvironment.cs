using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetStream;

public static class NetStreamEnvironment
{
    private static readonly object SyncRoot = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_loaded)
            {
                return;
            }

            foreach (var candidate in GetCandidateEnvPaths())
            {
                if (!File.Exists(candidate))
                {
                    continue;
                }

                LoadFile(candidate);
                break;
            }

            _loaded = true;
        }
    }

    public static string? GetString(string key)
    {
        Load();
        return Environment.GetEnvironmentVariable(key);
    }

    public static string GetRequiredString(string key, string description)
    {
        var value = GetString(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing {description}. Add `{key}` to the .env file.");
        }

        return value;
    }

    public static string GetRequiredBase64DecodedString(string key, string description)
    {
        var encoded = GetRequiredString(key, description);
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Invalid base64 value for `{key}`.", ex);
        }
    }

    public static IEnumerable<string> SplitList(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Enumerable.Empty<string>();
        }

        return rawValue
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static value => value.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value));
    }

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = ParseValue(line[(separatorIndex + 1)..].Trim());
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string ParseValue(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            return value[1..^1]
                .Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace("\\r", "\r", StringComparison.Ordinal)
                .Replace("\\t", "\t", StringComparison.Ordinal)
                .Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal);
        }

        if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
        {
            return value[1..^1];
        }

        return value;
    }

    private static IEnumerable<string> GetCandidateEnvPaths()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            foreach (var directory in EnumerateDirectories(root))
            {
                var path = Path.Combine(directory, ".env");
                if (seen.Add(path))
                {
                    yield return path;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current != null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }
}
