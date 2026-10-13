using System;
using System.Collections.Generic;

namespace Squealify;

/// <summary>
/// Lexical scanning of user SQL: parameter placeholders and statement kind. Skips string literals, quoted identifiers and comments.
/// </summary>
internal static class SqlText
{
    private static readonly string[] QueryKeywords = ["SELECT", "WITH", "VALUES", "PRAGMA", "EXPLAIN", "SHOW"];

    /// <summary>
    /// Names of <c>@name</c> placeholders, in order of appearance (with repetitions).
    /// </summary>
    public static List<string> Parameters(string sql)
    {
        var parameters = new List<string>();
        foreach (var (start, length) in Code(sql))
        {
            var end = start + length;
            for (var i = start; i < end; i++)
            {
                if (sql[i] != '@' || i + 1 >= end || !IsIdentifierStart(sql[i + 1]))
                {
                    continue;
                }

                var nameStart = i + 1;
                var nameEnd = nameStart;
                while (nameEnd < end && IsIdentifierPart(sql[nameEnd]))
                {
                    nameEnd++;
                }

                parameters.Add(sql.Substring(nameStart, nameEnd - nameStart));
                i = nameEnd - 1;
            }
        }

        return parameters;
    }

    /// <summary>
    /// True when the statement returns rows: it starts with a query keyword or contains RETURNING.
    /// </summary>
    public static bool IsQuery(string sql)
    {
        var first = true;
        foreach (var word in Words(sql))
        {
            if (first)
            {
                first = false;
                foreach (var keyword in QueryKeywords)
                {
                    if (string.Equals(word, keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            if (string.Equals(word, "RETURNING", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Words(string sql)
    {
        foreach (var (start, length) in Code(sql))
        {
            var end = start + length;
            var i = start;
            while (i < end)
            {
                if (!IsIdentifierStart(sql[i]) || (i > start && sql[i - 1] == '@'))
                {
                    i++;
                    continue;
                }

                var wordEnd = i;
                while (wordEnd < end && IsIdentifierPart(sql[wordEnd]))
                {
                    wordEnd++;
                }

                yield return sql.Substring(i, wordEnd - i);
                i = wordEnd;
            }
        }
    }

    /// <summary>
    /// Ranges of <paramref name="sql"/> outside string literals, quoted identifiers and comments.
    /// </summary>
    private static IEnumerable<(int Start, int Length)> Code(string sql)
    {
        var start = 0;
        var i = 0;
        while (i < sql.Length)
        {
            var c = sql[i];
            int skipEnd;
            if (c is '\'' or '"')
            {
                skipEnd = i + 1;
                while (skipEnd < sql.Length)
                {
                    if (sql[skipEnd] == c)
                    {
                        if (skipEnd + 1 < sql.Length && sql[skipEnd + 1] == c)
                        {
                            skipEnd += 2;
                            continue;
                        }

                        skipEnd++;
                        break;
                    }

                    skipEnd++;
                }
            }
            else if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                skipEnd = sql.IndexOf('\n', i);
                skipEnd = skipEnd < 0 ? sql.Length : skipEnd + 1;
            }
            else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                skipEnd = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                skipEnd = skipEnd < 0 ? sql.Length : skipEnd + 2;
            }
            else
            {
                i++;
                continue;
            }

            if (i > start)
            {
                yield return (start, i - start);
            }

            i = skipEnd;
            start = skipEnd;
        }

        if (sql.Length > start)
        {
            yield return (start, sql.Length - start);
        }
    }

    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';
}
