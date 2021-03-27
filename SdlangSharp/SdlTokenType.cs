#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    public enum SdlTokenType
    {
        Failsafe,

        Comment,
        Identifier,
        StringDoubleQuoted,
        StringBackQuoted,
        NumberInt32,
        NumberInt64,
        NumberFloat32,
        NumberFloat64,
        NumberFloat128,
        BooleanTrue,
        BooleanFalse,
        Date,
        DateTime,
        TimeSpan,
        EndOfLine,
        EndOfFile,
        BlockOpen,
        BlockClose,
        Equals,
        Null,
        Binary
    }
}
