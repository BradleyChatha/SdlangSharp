using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    public delegate void SdlStringVisitor(ReadOnlySpan<char> slice);

    internal ref struct SdlStringPusher
    {
        readonly ReadOnlySpan<char> _string;

        public SdlStringPusher(ReadOnlySpan<char> str)
        {
            this._string = str;
        }

        public void Visit(SdlStringVisitor visitor)
        {
            if(visitor == null)
                throw new ArgumentNullException(nameof(visitor));

            var start = 0;
            var cursor = 0;

            // Don't need to perform much validation here, as it should've been caught inside of SdlReader already.
            while(cursor < this._string.Length)
            {
                var ch = this._string[cursor];

                if(ch == '\\')
                {
                    visitor(this._string[start..cursor]);

                    var nextCh = this._string[++cursor];
                    start = cursor;

                    switch (nextCh)
                    {
                        case 'n': visitor("\n"); break;
                        case 't': visitor("\t"); break;
                        case 'r': visitor("\r"); break;
                        case '"': visitor("\""); break;
                        case '\\': visitor("\\"); break;

                        case '\r': cursor++; goto case '\n'; // \n should always follow. Enforced in SdlReader.
                        case '\n':
                            // Spec says to trim-left any whitespace after an escaped multi-line.
                            cursor++;
                            while(cursor < this._string.Length 
                               && (this._string[cursor] == ' ' ||this._string[cursor] == '\t')
                            )
                                cursor++;
                            start = cursor;
                            continue;

                        default: throw new Exception("This shouldn't happen");
                    }
                    continue;
                }
                else if(ch == '\r')
                {
                    visitor(this._string[start..cursor]);

                    if (this._string[++cursor] != '\n')
                        throw new Exception("This should've been caught in SdlReader");

                    start = cursor;
                }
                else
                    cursor++;
            }

            if(start != cursor)
                visitor(this._string[start..cursor]);
        }
    }
}
