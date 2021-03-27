#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;

namespace SdlangSharp
{
    [Flags]
    enum SdlReadCharOptions
    {
        None,
        CanEscapeNewLines    = 1 << 0,
        CanEscapeStringChars = 1 << 1
    }

    [DebuggerDisplay("Type = {TokenType} | Cursor = {_inputCursor} | Value = {ValueSpan}")]
    public ref struct SdlReader
    {
        ReadOnlySpan<char> _input;
        int _inputCursor;

        // For the last read token.
        public SdlTokenType TokenType { get; private set; }
        public ReadOnlySpan<char> ValueSpan { get; private set; }

        // For tag names only
        public ReadOnlySpan<char> TagNamespaceSpan { get; private set; }
        public ReadOnlySpan<char> TagNameSpan { get; private set; }

        // For date & datetime only
        public DateTimeOffset DateTimeValue { get; private set; }

        // For timespan only
        public TimeSpan TimeSpanValue { get; private set; }

        public SdlReader(ReadOnlySpan<char> input)
        {
            this._input = input;
            this.TokenType = SdlTokenType.Failsafe;
            this._inputCursor = 0;
            this.ValueSpan = default;
            this.TagNamespaceSpan = default;
            this.TagNameSpan = default;
            this.DateTimeValue = default;
            this.TimeSpanValue = default;
        }

        public void GetString(SdlStringVisitor visitor)
        {
            switch(this.TokenType)
            {
                default: throw new Exception($"This function only works on StringDoubleQuoted and StringBackQuoted, not {this.TokenType}");
                
                case SdlTokenType.StringBackQuoted:
                    visitor(this.ValueSpan);
                    break;

                case SdlTokenType.StringDoubleQuoted:
                    var pusher = new SdlStringPusher(this.ValueSpan);
                    pusher.Visit(visitor);
                    break;
            }
        }

        public string GetString()
        {
            var builder = new StringBuilder(this.ValueSpan.Length);
            this.GetString(span => builder.Append(span));

            return builder.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read()
        {
            this.ReadImpl();
        }

        public void Clone(ref SdlReader reader)
        {
            reader._input = this._input;
            reader._inputCursor = this._inputCursor;
            reader.TokenType = this.TokenType;
            reader.ValueSpan = this.ValueSpan;
            reader.TagNamespaceSpan = this.TagNamespaceSpan;
            reader.TagNameSpan = this.TagNameSpan;
            reader.TimeSpanValue = this.TimeSpanValue;
            reader.DateTimeValue = this.DateTimeValue;
        }

        // NOTE: All functions marked as AggressiveInlining were all individually tested for their performance improvement/impact.
        //       I can only test on my own CPU of course, but all functions marked to be inlined have provided speed increases.
        //       The most notable is PeekChar, which cut down parsing 200MB 100 times from 100 seconds, down to 70 seconds,
        //       which is weird since you'd think a function that large and used(inlined) that frequently
        //       would mess up the cache lines, but the numbers don't lie.
        //
        //       It's even weirder if you consider that most of PeekChar is conditional code that isn't often executed, am I missing something?
        //
        //       After adding SIMD: logicless parsing from ~56MB/s to ~200MB/s, at least for the example input data. wowee.
        //       I'm not quite sure how, and I'm kind of in doubt about it still, but at one point the numbers were ~800MB/s for the old example data.
        //
        //       After changing up the example file: Seems to stabilise around ~263MB/s no matter what I change the example to now.

        #region Private parsing functions
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void SetToken(SdlTokenType type, int start, int end)
        {
            this.TokenType = type;
            this.ValueSpan = this._input[start..end];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadImpl()
        {
            this.SkipSpacesAndTabs();
            if(this.IsEof())
            {
                this.SetToken(SdlTokenType.EndOfFile, 0, 0);
                return;
            }

            var nextCh = this.PeekChar(out int charsRead, SdlReadCharOptions.CanEscapeNewLines);
            if(charsRead > 1)
            {
                this.NextChar(charsRead-1); // Means we've probably hit an escaped new line.
                charsRead = 1;
            }

            if(IsCommentStartChar(nextCh))
            {
                // Edge case: - could either be the start of a comment, or the start of a number.
                if(nextCh == '-' && this._inputCursor + 1 < this._input.Length && this._input[this._inputCursor + 1] != '-')
                    this.ReadNumberOrDateOrTimeOrTimespan();
                else
                    this.ReadComment();
            }
            else if(Char.IsLetter(nextCh) || nextCh == '_')
                this.ReadIdentifierOrBooleanOrNull();
            else if(Char.IsDigit(nextCh) || nextCh == '-')
                this.ReadNumberOrDateOrTimeOrTimespan(); // The name of this function reflects my struggles.
            else if(nextCh == '"' || nextCh == '`')
                this.ReadString();
            else if(nextCh == '{' || nextCh == '}' || nextCh == '=')
            {
                this.NextChar(charsRead);
                this.SetToken(
                    (nextCh == '{') ? SdlTokenType.BlockOpen 
                        : (nextCh == '}') ? SdlTokenType.BlockClose
                            : SdlTokenType.Equals,
                    this._inputCursor - 1, 
                    this._inputCursor
                );
            }
            else if(nextCh == '[')
            {
                this.NextChar(charsRead);
                var start = this._inputCursor;
                this.ReadToEndOrChar(']', out int _);
                if(this.IsEof())
                    throw new SdlException("Unterminated Base64 sequence. Could not find ending ']' bracket.");
                this.SetToken(SdlTokenType.Binary, start, this._inputCursor);
                this.NextChar(1); // Skip end bracket.
            }
            else if(nextCh == '\n' || nextCh == ';') // Functionally the semi-colon in SDLang is just a fancy new line.
            {
                this.NextChar(charsRead);
                this.SetToken(SdlTokenType.EndOfLine, 0, 0);
            }
            else if (nextCh == ' ' || nextCh == '\t') // Slight edge case in how PeekChar works
                this.ReadImpl();
            else
                throw new SdlException("Unexpected character: '"+nextCh+"'");
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadComment()
        {
            var startChar = this.NextChar();
            var startCursor = this._inputCursor;
            int endCursor;

            switch (startChar)
            {
                case '#': this.ReadToEndOrChar('\n', out endCursor); break;
                
                case '-':
                case '/':
                    var nextChar = this.NextChar();
                    startCursor = this._inputCursor;

                    if ((startChar == '/' && nextChar != '*') || startChar != '/')
                    {
                        if(nextChar != startChar)
                            throw new SdlException("Expected second '"+startChar+"' to start a line comment, not '"+nextChar+"'");
                    
                        this.ReadToEndOrChar('\n', out endCursor);
                        break;
                    }

                    while(true)
                    {
                        endCursor = this._inputCursor;

                        if(this.IsEof())
                            throw new SdlException("Unexpected EOF when reading multi-line comment");
                        var charOne = this.NextChar();
                        if(this.IsEof())
                            throw new SdlException("Unexpected EOF when reading multi-line comment");
                        var charTwo = this.PeekChar(out int charsRead);

                        if(charOne == '*' && charTwo == '/')
                        {
                            this.NextChar(charsRead);
                            break;
                        }
                    }
                    break;

                default: throw new Exception("This shouldn't happen.");
            }

            this.SetToken(SdlTokenType.Comment, startCursor, endCursor);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadIdentifierOrBooleanOrNull()
        {
            var start = this._inputCursor;
            var nameStartCursor = start;
            while(!this.IsEof())
            {
                this.ReadToEndOrChars(':', ' ', '=', '\r', '\n');
                if(this.IsEof())
                    break;

                var ch = this.PeekCharRaw();
                if(ch == ':')
                {
                    this.TagNamespaceSpan = this._input[start..this._inputCursor];
                    this.NextChar(1);
                    nameStartCursor = this._inputCursor;
                    continue;
                }

                break; // Hit an end delimiter.
            }

            this.SetToken(SdlTokenType.Identifier, start, this._inputCursor);
            if(nameStartCursor < this._input.Length)
                this.TagNameSpan = this._input[nameStartCursor..this._inputCursor];
            else
                this.TagNameSpan = default;

            nameStartCursor -= (nameStartCursor != start) ? 1 : 0;
            this.TagNamespaceSpan = this._input[start..nameStartCursor];

            if (this.ValueSpan.Equals("true", StringComparison.Ordinal) || this.ValueSpan.Equals("on", StringComparison.Ordinal))
                this.TokenType = SdlTokenType.BooleanTrue;
            else if(this.ValueSpan.Equals("false", StringComparison.Ordinal) || this.ValueSpan.Equals("off", StringComparison.Ordinal))
                this.TokenType = SdlTokenType.BooleanFalse;
            else if(this.ValueSpan.Equals("null", StringComparison.Ordinal))
                this.TokenType = SdlTokenType.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadString()
        {
            var quoteChar = this.NextChar();
            var start = this._inputCursor;

            if (quoteChar == '`')
            {
                this.ReadToEndOrChar('`', out int _);
                if(this.IsEof())
                    throw new SdlException("Unterminated string");
                this.SetToken(SdlTokenType.StringBackQuoted, start, this._inputCursor);
                this.NextChar(1);
                return;
            }

            // New lines are a PITA, especially escapable ones, but this is still several times faster than
            // the character-by-character algorithm.
            //
            // Keep note: This is actually an O(2n) algorithm, which isn't much of a problem generally
            //            unless the system doesn't have SIMD support, but who even uses CPUs that old anyway? (Andy...?)
            //            So if ReadToEndOrChar has to use the slow non-SIMD loop, then this is 2x slower than the
            //            previous code which was an O(n) non-SIMD loop.

            // First, let's find a non-escaped speech mark (or end :()
            int speechMarkCursor = -1;
            while(speechMarkCursor < 0)
            {
                this.ReadToEndOrChar('"', out int _);
                if(this.IsEof())
                    throw new SdlException("Unterminated string");

                // Escaped speech mark.
                if(this._inputCursor > 0 && this._input[this._inputCursor - 1] == '\\')
                {
                    this.NextChar(1);
                    continue;
                }

                speechMarkCursor = this._inputCursor;
            }

            // Now, to keep with the spec we need to make sure that the string is only on a single line, unless the line
            // breaks were escaped. So we start reading from the start again (with SIMD this is still really really fast).
            //
            // We *technically* need to disallow tabs as well according to the language guide, buuuut fuck that, who cares.
            this._inputCursor = start;
            while(!this.IsEof() && this._inputCursor < speechMarkCursor)
            {
                this.ReadToEndOrChar('\n', out int _);
                if(this.IsEof() || this._inputCursor > speechMarkCursor) // String is valid.
                    break;

                // Escaped new line.
                if((this._inputCursor > 0 && this._input[this._inputCursor - 1] == '\\')
                || (this._inputCursor > 1 && this._input[this._inputCursor - 1] == '\r' && this._input[this._inputCursor - 2] == '\\'))
                {
                    this.NextChar(1);
                    continue;
                }

                throw new SdlException("Unescaped new line found within a string. All new lines within a double quoted (\") string must be escaped by prefixing it with a backslash (\\).");
            }

            this._inputCursor = speechMarkCursor;
            this.NextChar(1); // Skip over speech mark.
            this.SetToken(SdlTokenType.StringDoubleQuoted, start, speechMarkCursor);
        }

        // Defaults to ReadNumber
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadNumberOrDateOrTimeOrTimespan()
        {
            int start = this._inputCursor;
            this.NextChar();

            while(!this.IsEof())
            {
                var ch = this.PeekChar(out int charsRead);

                // I am ~~speed~~ snail.
                // Followers of the DRY cult, I shall forsake thee for favour of the goddess of laziness.
                if(Char.IsDigit(ch) || ch == '.')
                {
                    this.NextChar(charsRead);
                    continue;
                }
                else if (ch == ':')
                {
                    // Reset to the start just for simpler logic.
                    this._inputCursor = start;
                    this.ReadTime();
                    return;
                }
                else if (ch == '/')
                {
                    this._inputCursor = start;
                    this.ReadDateOrDateTime();
                    return;
                }
                else if (Char.IsWhiteSpace(ch))
                {
                    this.SetToken(SdlTokenType.NumberInt32, start, this._inputCursor);
                    return;
                }
                else if(ch == 'l' || ch == 'L')
                {
                    this.SetToken(SdlTokenType.NumberInt64, start, this._inputCursor);
                    this.NextChar(charsRead);
                    return;
                }
                else if (ch == 'f' || ch == 'F')
                {
                    this.SetToken(SdlTokenType.NumberFloat32, start, this._inputCursor);
                    this.NextChar(charsRead);
                    return;
                }
                else if (ch == 'd' || ch == 'D')
                {
                    this.SetToken(SdlTokenType.NumberFloat64, start, this._inputCursor);
                    this.NextChar(charsRead);

                    // Edge case: "0d:hh:mm:ss" the "0d:" is handled here.
                    if(!this.IsEof() && this.PeekChar(out int _) == ':')
                    {
                        this._inputCursor = start;
                        this.ReadTime();
                        return;
                    }
                    return;
                }
                else if (ch == 'b' || ch == 'B')
                {
                    this.NextChar(charsRead);
                    var nextCh = this.PeekChar(out charsRead);

                    if(nextCh != 'd' && nextCh != 'D')
                        throw new SdlException("Expected 'd' or 'D' following 'b' or 'B' 128-bit float suffix, but got: "+nextCh);

                    this.SetToken(SdlTokenType.NumberFloat128, start, this._inputCursor - 1);
                    this.NextChar(charsRead);
                    return;
                }
                else
                    throw new SdlException("Unexpected character when parsing number/date/time: "+ch);
            }

            this.SetToken(SdlTokenType.NumberInt32, start, this._inputCursor);
        }

        // here be dargons. A mighty storm?
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadTime()
        {
            this.TimeSpanValue = default;

            var startCursor = this._inputCursor;
            var ch = this.PeekChar(out int charsRead);
            var isNegative = (ch == '-');

            if(isNegative)
                this.NextChar(charsRead);

            bool hitColon = false;
            bool hitDot = false;
            void readToColonOrSpaceOrEndOrDot(ref SdlReader context, out ReadOnlySpan<char> resultRef)
            {
                hitColon = false;
                hitDot = false;
                int start = context._inputCursor;
                while (!context.IsEof())
                {
                    var currentCh = context.PeekChar(out charsRead);
                    if (currentCh == ':' || Char.IsWhiteSpace(currentCh) || currentCh == '.')
                    {
                        hitColon = (currentCh == ':');
                        hitDot = (currentCh == '.');
                        resultRef = context._input[start..context._inputCursor];
                        if (!Char.IsWhiteSpace(currentCh))
                            context.NextChar(charsRead);
                        return;
                    }
                    context.NextChar(charsRead);
                }
                resultRef = context._input[start..context._inputCursor];
            }

            int toInt(ref ReadOnlySpan<char> resultRef, string unit)
            {
                if(resultRef.Length == 0)
                    throw new SdlException("No value provided for timespan "+unit);
                if(!Int32.TryParse(resultRef, out int result))
                    throw new SdlException($"Invalid timespan {unit} value: {resultRef.ToString()}");
                return result;
            }

            void enforceHitColon()
            {
                if (!hitColon)
                    throw new SdlException("Expected colon");
            }

            readToColonOrSpaceOrEndOrDot(ref this, out ReadOnlySpan<char> result);
            enforceHitColon();
            if (result.EndsWith("d"))
            {
                var slicedResult = result[0..^1];
                this.TimeSpanValue = this.TimeSpanValue.Add(TimeSpan.FromDays(toInt(ref slicedResult, "days")));
                result = default;
            }

            if(result == default)
                readToColonOrSpaceOrEndOrDot(ref this, out result);
            enforceHitColon();
            this.TimeSpanValue = this.TimeSpanValue.Add(TimeSpan.FromHours(toInt(ref result, "hours")));
            readToColonOrSpaceOrEndOrDot(ref this, out result);
            enforceHitColon();
            this.TimeSpanValue = this.TimeSpanValue.Add(TimeSpan.FromMinutes(toInt(ref result, "minutes")));
            readToColonOrSpaceOrEndOrDot(ref this, out result);
            this.TimeSpanValue = this.TimeSpanValue.Add(TimeSpan.FromSeconds(toInt(ref result, "seconds")));

            if(hitDot)
            {
                readToColonOrSpaceOrEndOrDot(ref this, out result);
                this.TimeSpanValue = this.TimeSpanValue.Add(TimeSpan.FromMilliseconds(toInt(ref result, "millisecs")));
            }

            if(isNegative)
                this.TimeSpanValue = this.TimeSpanValue.Negate();
            this.SetToken(SdlTokenType.TimeSpan, startCursor, this._inputCursor);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadDateOrDateTime()
        {
            int REQUIRED_CHARS = "yyyy/mm/dd".Length;

            if(this._inputCursor + REQUIRED_CHARS > this._input.Length)
                throw new SdlException($"Expected {REQUIRED_CHARS} available characters for Date, but reached EOF. Format must be yyyy/mm/dd");

            var start = this._inputCursor;
            this._inputCursor += REQUIRED_CHARS;

            // See if we need to read the time as well.
            var end = this._inputCursor;
            this.SkipSpacesAndTabs();

            // Read to space/end. Using some hereustics since I really cba to parse this part properly.
            bool hasColon = false; // Hereustic we can use to determine if we're looking at a time value.
            var hyphenCursor = 0;
            while(!this.IsEof())
            {
                var ch = this.PeekChar(out int charsRead);
                if(Char.IsWhiteSpace(ch))
                    break;

                this._inputCursor += charsRead;

                hasColon = hasColon || ch == ':';
                if(ch == '-' && hyphenCursor == 0)
                    hyphenCursor = this._inputCursor;
            }

            if(hasColon)
                end = (hyphenCursor > 0) ? hyphenCursor-1 : this._inputCursor;
            else
                this._inputCursor = end;

            var timeSlice = this._input[start..end];
            if (!DateTime.TryParse(timeSlice, out DateTime dateTime))
                throw new SdlException("Invalid DateTime value: "+timeSlice.ToString());

            this.DateTimeValue = dateTime;
            this.SetToken((hasColon) ? SdlTokenType.DateTime : SdlTokenType.Date, start, this._inputCursor);

            if(hyphenCursor > 0)
            {
                var timezoneSlice = this._input[hyphenCursor..this._inputCursor];
                var match = Regex.Match(timezoneSlice.ToString(), @"GMT([\+\-]?)(\d?\d?:?\d?\d?)");
                if(!match.Success || (match.Groups[2].Value.Length > 0 && match.Groups[1].Value.Length == 0))
                    throw new SdlException("Invalid timezone within DateTime, only supported format is: -GMT+/-nn:nn and -GMT");

                var timezone = TimeSpan.Zero;

                if(!(
                    match.Groups[2].Value.Length == 0
                 || TimeSpan.TryParseExact(match.Groups[2].Value, "hh", null, out timezone)
                 || TimeSpan.TryParseExact(match.Groups[2].Value, "hh':'mm", null, out timezone)
                ))
                    throw new SdlException("Invalid timezone within DateTime, use 'hh' or 'hh:mm' format. Bad format: "+ match.Groups[2].Value);

                if(match.Groups[1].Value == "-")
                    timezone = timezone.Negate();

                this.DateTimeValue = new DateTimeOffset(this.DateTimeValue.DateTime, timezone);
            }

            var delta = this._inputCursor - start;
            this._inputCursor = start;
            this.NextChar(delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static bool IsCommentStartChar(char ch) => 
            ch == '#'
         || ch == '-'
         || ch == '/';

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static bool IsValidIdentifierChar(char ch) =>
            Char.IsLetterOrDigit(ch)
         || ch == '.'
         || ch == '_'
         || ch == '-'
         || ch == '$';

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private char PeekCharRaw() => this._input[this._inputCursor];

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private char PeekChar(out int charsRead, SdlReadCharOptions options = SdlReadCharOptions.None)
        {
            return this.PeekChar(out charsRead, out bool _, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private char PeekChar(out int charsRead, out bool wasStringCharEscaped, SdlReadCharOptions options = SdlReadCharOptions.None)
        {
            var ch = this._input[this._inputCursor];
            charsRead = 1;
            wasStringCharEscaped = false;

            // Special case: Sdlang spec defines '\r\n' as always being read as '\n'.
            if(ch == '\r')
            {
                if(this._inputCursor + 1 >= this._input.Length
                || this._input[this._inputCursor + 1] != '\n')
                    throw new SdlException("Stray carriage return - no line feed character following it.");

                ch = '\n';
                charsRead = 2;
            }
            else if(ch == '\\')
            {
                if((options & SdlReadCharOptions.CanEscapeNewLines) > 0)
                {
                    var startCursor = this._inputCursor;
                    this._inputCursor++;

                    var nextCh = this.PeekChar(out int extraCharsRead, options);
                    if(nextCh == '\n')
                    {
                        charsRead += extraCharsRead;
                        this._inputCursor += extraCharsRead;
                        ch = this.PeekChar(out extraCharsRead, options);
                        charsRead += extraCharsRead;
                        this._inputCursor = startCursor;
                        return ch;
                    }
                }

                if((options & SdlReadCharOptions.CanEscapeStringChars) > 0)
                {
                    if(this._inputCursor + 1 >= this._input.Length)
                        throw new SdlException("Unexpected EOF after initial escape backslash");

                    var nextCh = this._input[this._inputCursor + 1];
                    charsRead = 2;

                    wasStringCharEscaped = true;
                    switch(nextCh)
                    {
                        case 'n': ch = '\n'; break;
                        case 't': ch = '\t'; break;
                        case 'r': ch = '\r'; break;
                        case '"': ch = '"'; break;
                        case '\\': ch = '\\'; break;

                        default: wasStringCharEscaped = false; break;
                    }

                    if(wasStringCharEscaped)
                        return ch;
                }
            }

            return ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private char NextChar(SdlReadCharOptions options = SdlReadCharOptions.None)
        {
            var ch = this.PeekChar(out int charsRead, options);
            this.NextChar(charsRead);
            
            return ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void NextChar(int charsReadFromPeek)
        {
            this._inputCursor += charsReadFromPeek;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private bool IsEof() => this._inputCursor >= this._input.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private void SkipSpacesAndTabs()
        {
            while (!this.IsEof())
            {
                var ch = this.PeekCharRaw();
                if (ch != ' ' && ch != '\t')
                    break;
                this.NextChar(1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadToEndOrChar(char expectedChar, out int cursorBeforeCharacter)
        {
            // If we have a relatively modern CPU, then we can process look at 16 chars at a time for a new line.
            if (Avx2.IsSupported)
            {
                unsafe
                {
                    /*
                        So basically, while we have at least 16 characters to read:
                            Load the next 16 characters into the `chars` vector;
                            Load a `newLineVect` where every ushort element is the UTF-16 character;
                            Compare the two vectors, and get the mask into `mask`.
                                - There will be two bits set per character found, 
                                  since our ushort compare sets two bytes to 0xFF per new line
                                  and MoveMask moves the MSB of each vector byte into the mask
                            We need to find the earliest LSB set, as that represents the earliest found character within our chars vector,
                            keep in mind more than one of the character may exist, which is why we can't use LZCNT instead.
                                - To do this, we perform TZCNT to count up to the earliest bit instead.
                            Finally, we halve this count (since the count is in bits, and every 2 bits is 1 char)
                            to find out how many characters to move forward by in order to get our cursor on top of the character.
                            If we can't find the new line in this block of 16 characters, move onto the next 16, and so on.

                        Yes, this is here mostly for my future self's sake ;^^^^)
                     */
                    fixed (char* charPtr = this._input)
                    {
                        var charAsShort = (ushort)expectedChar;

                        var ushortPtr = ((ushort*)charPtr) + this._inputCursor; // Side note: In C# land: pointer + n = pointer + (n * sizeof(T))
                        var charVect = Avx2.BroadcastScalarToVector256(&charAsShort);
                        var remainingChars = this._input.Length - this._inputCursor;
                        var vectorisableCharBlocks = remainingChars / 16; // 16 UTF-16 characters fit into a 256-bit register

                        for (int i = 0; i < vectorisableCharBlocks; i++)
                        {
                            var chars = Avx2.LoadVector256(ushortPtr + (i * 16));
                            var result = Avx2.CompareEqual(chars, charVect);
                            var mask = Avx2.MoveMask(result.AsByte());
                            if (mask != 0)
                            {
                                var cnt = BitOperations.TrailingZeroCount((uint)mask);
                                this._inputCursor += (cnt / 2);
                                cursorBeforeCharacter = this._inputCursor - 1;
                                return;
                            }

                            this._inputCursor += 16;
                        }
                    }
                }
            }

            // Otherwise, either because our CPU is old, or because we've ran out of blocks of 16 chars to process,
            // fallback to the slow-boi loop.
            cursorBeforeCharacter = this._inputCursor;
            while (!this.IsEof())
            {
                var ch = this.PeekChar(out int charsRead);
                if (ch == expectedChar)
                    break;
                this.NextChar(charsRead);
                cursorBeforeCharacter = this._inputCursor;
            }
        }

        // This is a version of ReadToEndOrChar that can look for multiple different characters.
        // Since this version is used in only a select few places and is slower, it's been hoisted into a seperate function.
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ReadToEndOrChars(char c1, char c2, char c3, char c4, char c5)
        {
            if (Avx2.IsSupported)
            {
                unsafe
                {
                    fixed (char* charPtr = this._input)
                    {
                        Span<char> chars = stackalloc char[5] { c1, c2, c3, c4, c5 };
                        Span<Vector256<ushort>> charVects = stackalloc Vector256<ushort>[5];

                        fixed (char* charsByPtr = chars)
                        {
                            for(int i = 0; i < chars.Length; i++)
                                charVects[i] = Avx2.BroadcastScalarToVector256(&((ushort*)charsByPtr)[i]);
                        }

                        var ushortPtr = ((ushort*)charPtr) + this._inputCursor; // Side note: In C# land: pointer + n = pointer + (n * sizeof(T))
                        var remainingChars = this._input.Length - this._inputCursor;
                        var vectorisableCharBlocks = remainingChars / 16; // 16 UTF-16 characters fit into a 256-bit register

                        for (int i = 0; i < vectorisableCharBlocks; i++)
                        {
                            var inputChars = Avx2.LoadVector256(ushortPtr + (i * 16));
                            var result = Vector256<ushort>.Zero;
                            foreach(var vect in charVects)
                            {
                                var vectCompareResult = Avx2.CompareEqual(inputChars, vect);
                                result = Avx2.Or(result, vectCompareResult);
                            }

                            var mask = Avx2.MoveMask(result.AsByte());
                            if (mask != 0)
                            {
                                var cnt = BitOperations.TrailingZeroCount((uint)mask);
                                this._inputCursor += (cnt / 2);
                                return;
                            }

                            this._inputCursor += 16;
                        }
                    }
                }
            }

            while (!this.IsEof())
            {
                var ch = this.PeekChar(out int charsRead);
                if (ch == c1 || ch == c2 || ch == c3 || ch == c4 || ch == c5)
                    break;
                this.NextChar(charsRead);
            }
        }
        #endregion
    }
}
