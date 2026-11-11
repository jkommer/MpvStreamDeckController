// C# port of the command-string tokenizer used for mpv's input.conf commands by Claude Code then manually edited for readability. 
//
// Ported from mpv master:
//   input/cmd.c      read_token(), pctx_read_token(), parse_cmd_str() (tokenizing part),
//                    mp_input_parse_cmd_str() (";" splitting and trailing "# comment")
//   common/common.c  mp_append_escaped_string_noalloc(), mp_parse_escape()
//
// NOT ported (all depend on mpv's command table in player/command.c):
//   - prefix flags such as "no-osd" / "async" (they appear here as ordinary tokens)
//   - command-name lookup and "_" -> "-" normalisation
//   - per-argument type parsing, default values, and the "trailing unused
//     arguments" check (here every token up to the next ';' / '#' is returned)
//
// NOT part of this file either: input.c's splitting of an input.conf line into
// "<key> <command>", which happens before this code runs.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MpvStreamDeckController;

/// <summary>Result of parsing one command string.</summary>
/// <param name="Commands">
/// One token list per ';'-separated command. Element 0 of each list is the
/// command name (or a flag such as "no-osd").
/// </param>
/// <param name="Description">
/// Text of a trailing "# comment" (mpv stores it in cmd->desc), or null.
/// </param>
public sealed record ParsedCommandLine(IReadOnlyList<IReadOnlyList<string>> Commands, string? Description);

public static class MpvCommandParser
{
    // bstr.h: #define WHITESPACE " \f\n\r\t\v"  (ASCII only, unlike char.IsWhiteSpace)
    private const string Whitespace = " \f\n\r\t\v";

    /// <summary>
    /// Parses a command string such as <c>seek 10; show-text "a\nb" # note</c>. Mirrors mp_input_parse_cmd_str(); on failure returns false and sets
    /// <paramref name="error"/> to a message modelled on mpv's log output.
    /// </summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out ParsedCommandLine? result, [NotNullWhen(false)] out string? error)
    {
        result = null;
        error = null;

        if (text is null)
        {
            error = "Command text is null.";
            return false;
        }

        var cursor = new Cursor(text);
        var commands = new List<IReadOnlyList<string>>();

        var firstCommand = ParseCommand(cursor);

        if (firstCommand is null)
        {
            error = cursor.Error ?? "Unknown error.";
            return false;
        }

        commands.Add(firstCommand);

        // "Handle multi commands": keep going while we see ';' followed by something that looks like a token.
        while (true)
        {
            cursor.SkipWhitespace();

            if (cursor.Position >= text.Length || text[cursor.Position] != ';')
            {
                break;
            }

            // eat ';' (mpv eats it even if the loop then stops)
            cursor.Position++;

            if (!TryReadPlainToken(text, cursor.Position, out _, out _))
            {
                // e.g. trailing ';' or ';' followed by '#', ';' or end
                break;
            }

            var subsequentCommand = ParseCommand(cursor);

            if (subsequentCommand is null)
            {
                error = cursor.Error ?? "Unknown error.";
                return false;
            }

            commands.Add(subsequentCommand);
        }

        // Trailing "# description". A comment starting with "##" is ignored. Anything else left over (e.g. the second ';' in "a;;b") is silently dropped, exactly as in mpv.
        cursor.SkipWhitespace();
        string? description = null;

        if (cursor.Position < text.Length && text[cursor.Position] == '#' && !(cursor.Position + 1 < text.Length && text[cursor.Position + 1] == '#'))
        {
            var desc = text[(cursor.Position + 1)..].Trim(Whitespace.ToCharArray());

            if (desc.Length > 0)
            {
                description = desc;
            }
        }

        result = new ParsedCommandLine(commands, description);
        return true;
    }

    /// <summary>
    /// Equivalent of parse_cmd_str(), tokenizing part only
    /// </summary>
    private static List<string>? ParseCommand(Cursor cursor)
    {
        // mpv's ctx->start: set before leading whitespace is stripped.
        cursor.CommandStart = cursor.Position;
        cursor.SkipWhitespace();

        var tokens = new List<string>();
        var result = ReadToken(cursor, out var token);

        if (result is TokenResult.Error)
        {
            return null;
        }

        if (result is TokenResult.Exhausted)
        {
            cursor.Error = "Command name missing.";
            return null;
        }

        tokens.Add(token);

        while (true)
        {
            result = ReadToken(cursor, out token);

            if (result is TokenResult.Error)
            {
                cursor.Error = $"Command {tokens[0]}: error in argument {tokens.Count}. {cursor.Error}";
                return null;
            }

            if (result is TokenResult.Exhausted)
            {
                // hit ';', '#' or end of input
                break;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    /// <summary>
    /// Equivalent of read_token(): an unquoted token. Stops at whitespace, '#' or ';'. Returns false if there is no token at the current position.
    /// </summary>
    private static bool TryReadPlainToken(string text, int position, out int start, out int end)
    {
        start = position;

        while (start < text.Length && IsWhitespace(text[start]))
        {
            start++;
        }

        end = start;

        while (end < text.Length && !IsWhitespace(text[end]) && text[end] != '#' && text[end] != ';')
        {
            end++;
        }

        return end > start;
    }

    private enum TokenResult
    {
        Error = -1,
        Exhausted = 0,
        TokenRead = 1
    }

    /// <summary>
    /// Equivalent of pctx_read_token(): mapped the return types onto an enum instead of just an int. 
    /// </summary>
    private static TokenResult ReadToken(Cursor cursor, out string token)
    {
        token = "";
        cursor.SkipWhitespace();

        var text = cursor.Text;
        var start = cursor.Position; // for error messages

        // "double quoted" with C-style escapes
        if (cursor.Eat('"'))
        {
            var buffer = new Utf8Buffer();

            if (!AppendEscapedString(cursor, buffer))
            {
                cursor.Error = $"Broken string escapes: ...>{text[start..]}<.";
                return TokenResult.Error;
            }

            if (!cursor.Eat('"'))
            {
                cursor.Error = $"Unterminated double quote: ...>{text[start..]}<.";
                return TokenResult.Error;
            }

            token = buffer.ToString();
            return TokenResult.TokenRead;
        }

        // 'single quoted': raw, no escapes
        if (cursor.Eat('\''))
        {
            var next = text.IndexOf('\'', cursor.Position);

            if (next < 0)
            {
                cursor.Error = $"Unterminated single quote: ...>{text[start..]}<.";
                return TokenResult.Error;
            }

            token = text[cursor.Position..next];
            cursor.Position = next + 1;
            return TokenResult.TokenRead;
        }

        // `Xcontent X` : backtick, a delimiter character X, content, then X and a closing backtick. Content is raw.
        // The "> 1" check mirrors mpv's `ctx->start.len > 1`: ctx->start is the text from where this command began (before its leading whitespace
        // was stripped) to the end of the input, NOT the text remaining at the current position, so it is nearly always true. This means a lone
        // trailing backtick (e.g. "foo `") is an unterminated custom quote in mpv, not a plain token.
        if (text.Length - cursor.CommandStart > 1 && cursor.Eat('`'))
        {
            // mpv reads the delimiter without a bounds check here (it hits the string's terminating NUL) and then fails to find the end quote.
            if (cursor.Position >= text.Length)
            {
                cursor.Error = $"Unterminated custom quote: ...>{text[start..]}<.";
                return TokenResult.Error;
            }

            var delimiter = text[cursor.Position];
            cursor.Position++;
            string endQuote = new([delimiter, '`']);

            var next = text.IndexOf(endQuote, cursor.Position, StringComparison.Ordinal);

            if (next < 0)
            {
                cursor.Error = $"Unterminated custom quote: ...>{text[start..]}<.";
                return TokenResult.Error;
            }

            token = text[cursor.Position..next];
            cursor.Position = next + 2;
            return TokenResult.TokenRead;
        }

        // Unquoted. Quotes are only special at the *start* of a token, so a"b c"d yields the two tokens  a"b  and  c"d.
        if (TryReadPlainToken(text, cursor.Position, out var tokStart, out var tokEnd))
        {
            token = text[tokStart..tokEnd];
            cursor.Position = tokEnd;
            return TokenResult.TokenRead;
        }

        return TokenResult.Exhausted;
    }

    /// <summary>
    /// Equivalent of mp_append_escaped_string_noalloc(): consume up to (not including) the closing '"' or end of input, resolving backslash escapes.
    /// </summary>
    private static bool AppendEscapedString(Cursor cursor, Utf8Buffer destination)
    {
        var text = cursor.Text;
        var position = cursor.Position;

        while (true)
        {
            var runStart = position;

            while (position < text.Length && text[position] != '"' && text[position] != '\\')
            {
                position++;
            }

            destination.Append(text, runStart, position - runStart);

            if (position >= text.Length || text[position] == '"')
            {
                cursor.Position = position;
                return true;
            }

            // skip the backslash
            position++;

            if (!ParseEscape(text, ref position, destination))
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Equivalent of mp_parse_escape(): `pos` points at the character after the backslash.
    /// </summary>
    private static bool ParseEscape(string text, ref int position, Utf8Buffer destination)
    {
        var remaining = text.Length - position;

        if (remaining < 1)
        {
            return false;
        }

        byte? replace = text[position] switch
        {
            '"' => (byte)'"',
            '\\' => (byte)'\\',
            '/' => (byte)'/',
            'b' => (byte)'\b',
            'f' => (byte)'\f',
            'n' => (byte)'\n',
            'r' => (byte)'\r',
            't' => (byte)'\t',
            'e' => 0x1b,
            '\'' => (byte)'\'',
            _ => null,
        };

        if (replace is not null)
        {
            destination.AppendByte(replace.Value);
            position += 1;
            return true;
        }

        // \xHH: appends ONE RAW BYTE (not a code point), so "\xc3\xa9" is the two bytes of UTF-8 "é". Utf8Buffer decodes at the end to preserve that.
        if (text[position] == 'x' && remaining >= 3)
        {
            if (!TryParseHex(text, position + 1, 2, out var @byte))
            {
                return false;
            }

            destination.AppendByte((byte)@byte);
            position += 3;
            return true;
        }

        // \uXXXX, with a following \uXXXX required after a high surrogate.
        if (text[position] == 'u' && remaining >= 5)
        {
            if (!TryParseHex(text, position + 1, 4, out var codePoint))
            {
                return false;
            }

            if (codePoint >= 0xD800 && codePoint <= 0xDBFF)
            {
                // need "uXXXX" + "\" + "uXXXX" = 11 characters
                if (remaining < 11 || text[position + 5] != '\\' || text[position + 6] != 'u')
                {
                    return false;
                }
                if (!TryParseHex(text, position + 7, 4, out var c2) || c2 < 0xDC00 || c2 > 0xDFFF)
                {
                    return false;
                }

                codePoint = ((codePoint - 0xD800) << 10) + 0x10000 + (c2 - 0xDC00);
                position += 11;
            }
            else
            {
                // Note: like mpv, a lone LOW surrogate (DC00-DFFF) is not rejected.
                position += 5;
            }

            destination.AppendCodePoint(codePoint);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Strict hex parsing. mpv uses bstrtoll(..., 16), which is built on strtoll()  and therefore also tolerates things like "+f", " f" or "0x1" inside \x / \u.
    /// That looks accidental, so it is deliberately not reproduced here.
    /// </summary>
    private static bool TryParseHex(string text, int position, int count, out uint value)
    {
        value = 0;

        if (position + count > text.Length)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var chr = text[position + i];

            var digit = chr switch
            {
                >= '0' and <= '9' => chr - '0',
                >= 'a' and <= 'f' => chr - 'a' + 10,
                >= 'A' and <= 'F' => chr - 'A' + 10,
                _ => -1,
            };

            if (digit < 0)
            {
                return false;
            }

            value = (value << 4) | (uint)digit;
        }

        return true;
    }

    private static bool IsWhitespace(char chr) => Whitespace.Contains(chr);

    /// <summary>
    /// Equivalent of mpv's parse_ctx: the text plus a read position.
    /// </summary>
    private sealed class Cursor(string text)
    {
        public readonly string Text = text;
        public int Position;
        public int CommandStart;
        public string? Error;

        public void SkipWhitespace()
        {
            while (Position < Text.Length && IsWhitespace(Text[Position]))
            {
                Position++;
            }
        }

        public bool Eat(char chr)
        {
            if (Position < Text.Length && Text[Position] == chr)
            {
                Position++;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// mpv strings are byte strings and "\xHH" appends a raw byte, so build the token as UTF-8 bytes and decode once at the end.
    /// </summary>
    private sealed class Utf8Buffer
    {
        private readonly List<byte> _bytes = [];

        public void Append(string text, int start, int count)
        {
            if (count > 0)
            {
                _bytes.AddRange(Encoding.UTF8.GetBytes(text.Substring(start, count)));
            }
        }

        public void AppendByte(byte @byte) => _bytes.Add(@byte);

        // Mirrors mp_append_utf8_bstr(); surrogate code points are encoded as 3 bytes (invalid UTF-8), which later decode to U+FFFD.
        public void AppendCodePoint(uint codePoint)
        {
            if (codePoint < 0x80)
            {
                _bytes.Add((byte)codePoint);
            }
            else if (codePoint < 0x800)
            {
                _bytes.Add((byte)(0xC0 | (codePoint >> 6)));
                _bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else if (codePoint < 0x10000)
            {
                _bytes.Add((byte)(0xE0 | (codePoint >> 12)));
                _bytes.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                _bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else
            {
                _bytes.Add((byte)(0xF0 | (codePoint >> 18)));
                _bytes.Add((byte)(0x80 | ((codePoint >> 12) & 0x3F)));
                _bytes.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                _bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
        }

        public override string ToString() => Encoding.UTF8.GetString([.. _bytes]);
    }
}
