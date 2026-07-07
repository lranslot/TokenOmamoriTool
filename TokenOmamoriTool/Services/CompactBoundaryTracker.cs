using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TokenOmamoriTool.Services;

/// <summary>
/// Claude Code writes a line with "isCompactSummary":true into a session's .jsonl right after /compact runs.
/// The file itself never shrinks (jsonl is append-only), so raw file size can't reflect post-compact bloat.
/// This tracks, per file path, the byte offset just after the most recent such line, and reports the size
/// of everything appended since. Repeated calls only re-scan bytes appended since the last call.
/// </summary>
public static class CompactBoundaryTracker
{
    private const string CompactMarker = "\"isCompactSummary\":true";
    private const int BackwardChunkSize = 65536;

    private sealed class Entry
    {
        public long ScannedLength;
        public long BoundaryOffset;
    }

    private static readonly Dictionary<string, Entry> Cache = new();
    private static readonly object Lock = new();

    public static long GetEffectiveSizeBytes(string filePath, long currentLength)
    {
        lock (Lock)
        {
            if (!Cache.TryGetValue(filePath, out var entry) || currentLength < entry.ScannedLength)
            {
                entry = new Entry();
            }

            if (currentLength > entry.ScannedLength)
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var readFrom = FindLineStartAtOrBefore(stream, entry.ScannedLength);

                var chunkLength = (int)(currentLength - readFrom);
                var buffer = new byte[chunkLength];
                stream.Seek(readFrom, SeekOrigin.Begin);
                stream.ReadExactly(buffer);

                var chunkText = Encoding.UTF8.GetString(buffer);
                var markerIndex = chunkText.LastIndexOf(CompactMarker, StringComparison.Ordinal);
                if (markerIndex >= 0)
                {
                    var lineEnd = chunkText.IndexOf('\n', markerIndex);
                    var cutoffCharIndex = lineEnd >= 0 ? lineEnd + 1 : chunkText.Length;
                    var bytesBeforeCutoff = Encoding.UTF8.GetByteCount(chunkText.AsSpan(0, cutoffCharIndex));
                    entry.BoundaryOffset = readFrom + bytesBeforeCutoff;
                }

                entry.ScannedLength = currentLength;
                Cache[filePath] = entry;
            }

            return currentLength - entry.BoundaryOffset;
        }
    }

    // Walks backward from `position` in fixed-size chunks to find the offset right after the nearest
    // preceding newline, so a re-scan always starts at a complete line boundary even when the line
    // spans more than one chunk (real compact-summary lines have been observed at 25-30KB).
    private static long FindLineStartAtOrBefore(FileStream stream, long position)
    {
        if (position <= 0) return 0;

        var searchEnd = position;
        while (searchEnd > 0)
        {
            var searchStart = Math.Max(0, searchEnd - BackwardChunkSize);
            var length = (int)(searchEnd - searchStart);
            var buffer = new byte[length];
            stream.Seek(searchStart, SeekOrigin.Begin);
            stream.ReadExactly(buffer);

            for (var i = length - 1; i >= 0; i--)
            {
                if (buffer[i] == (byte)'\n')
                {
                    return searchStart + i + 1;
                }
            }

            searchEnd = searchStart;
        }

        return 0;
    }
}
