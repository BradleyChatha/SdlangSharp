#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SdlangSharp
{
    public interface ISdlTokenVisitorBase
    {
        void Reset();
        void VisitOpenBlock();
        void VisitCloseBlock();
        void VisitComment(ReadOnlySpan<char> comment);
        void VisitStartTag(ReadOnlySpan<char> qualifiedName);
        void VisitEndOfFile();
    }

    public interface ISdlTokenVisitor : ISdlTokenVisitorBase
    {
        void VisitNewValue(SdlValue value);
        void VisitNewAttribute(ReadOnlySpan<char> qualifiedName, SdlValue value);
    }

    public interface ISdlTokenRawVisitor : ISdlTokenVisitorBase
    {
        void VisitNewValue(SdlTokenType valueType, ReadOnlySpan<char> value);
        void VisitNewAttribute(SdlTokenType valueType, ReadOnlySpan<char> qualifiedName, ReadOnlySpan<char> value);
    }

    public static class SdlTokenPusher
    {
        public const string ANONYMOUS_TAG_NAME = "content";

        public static void ParseAndVisit(SdlReader reader, ISdlTokenVisitorBase visitor)
        {
            ParseAndVisit(reader, new[]{ visitor });
        }

        public static void ParseAndVisit(SdlReader reader, IEnumerable<ISdlTokenVisitorBase> visitors)
        {
            foreach(var v in visitors)
                v.Reset();

            SdlReader copy = default;
            bool startOfLine = true;

            static SdlValue valueAsSdlValue(ref SdlReader reader) => reader.TokenType switch
            {
                SdlTokenType.Binary => new SdlValue(Convert.FromBase64String(reader.ValueSpan.ToString())),
                SdlTokenType.BooleanFalse => SdlValue.False,
                SdlTokenType.BooleanTrue => SdlValue.True,
                SdlTokenType.Date => new SdlValue(reader.DateTimeValue),
                SdlTokenType.DateTime => new SdlValue(reader.DateTimeValue),
                SdlTokenType.Null => SdlValue.Null,
                SdlTokenType.NumberFloat32 => new SdlValue(Convert.ToSingle(reader.ValueSpan.ToString())),
                SdlTokenType.NumberFloat64 => new SdlValue(Convert.ToDouble(reader.ValueSpan.ToString())),
                SdlTokenType.NumberFloat128 => throw new Exception("Not supported (yet)"),
                SdlTokenType.NumberInt32 => new SdlValue(Convert.ToInt32(reader.ValueSpan.ToString())),
                SdlTokenType.NumberInt64 => new SdlValue(Convert.ToInt64(reader.ValueSpan.ToString())),
                SdlTokenType.StringBackQuoted => new SdlValue(reader.ValueSpan.ToString()),
                SdlTokenType.StringDoubleQuoted => new SdlValue(reader.GetString()),
                SdlTokenType.TimeSpan => new SdlValue(reader.TimeSpanValue),
                _ => throw new Exception($"Expected a value token, not a token of type {reader.TokenType} with value {reader.ValueSpan.ToString()}")
            };

            reader.Read();
            while(reader.TokenType != SdlTokenType.EndOfFile)
            {
                switch(reader.TokenType)
                {
                    case SdlTokenType.Binary:
                    case SdlTokenType.BooleanFalse:
                    case SdlTokenType.BooleanTrue:
                    case SdlTokenType.Date:
                    case SdlTokenType.DateTime:
                    case SdlTokenType.NumberFloat128:
                    case SdlTokenType.NumberFloat32:
                    case SdlTokenType.NumberFloat64:
                    case SdlTokenType.NumberInt32:
                    case SdlTokenType.NumberInt64:
                    case SdlTokenType.StringBackQuoted:
                    case SdlTokenType.StringDoubleQuoted:
                    case SdlTokenType.TimeSpan:
                        SdlValue? value = null;
                        foreach (var visitor in visitors)
                        {
                            if (startOfLine) // anonymous tag support.
                                visitor.VisitStartTag(ANONYMOUS_TAG_NAME);
                            if(visitor is ISdlTokenVisitor highLevelVisitor)
                            {
                                value ??= valueAsSdlValue(ref reader);
                                highLevelVisitor.VisitNewValue(value);
                            }
                            else if(visitor is ISdlTokenRawVisitor lowLevelVisitor)
                                lowLevelVisitor.VisitNewValue(reader.TokenType, reader.ValueSpan);
                        }
                        break;

                    case SdlTokenType.BlockClose: foreach(var v in visitors) v.VisitCloseBlock(); break;
                    case SdlTokenType.BlockOpen: foreach(var v in visitors) v.VisitOpenBlock(); break;

                    case SdlTokenType.Comment:
                        foreach(var v in visitors) // foreachVisistor can't see ref structs.
                            v.VisitComment(reader.ValueSpan);
                        break;

                    case SdlTokenType.EndOfLine:
                        startOfLine = true;
                        reader.Read();
                        continue;

                    case SdlTokenType.Identifier:
                        reader.Clone(ref copy);
                        copy.Read();
                        if(copy.TokenType == SdlTokenType.Equals) // attribute assignment.
                        {
                            if(startOfLine) // anonymous tag
                                foreach(var v in visitors) v.VisitStartTag(ANONYMOUS_TAG_NAME);

                            copy.Read();
                            SdlValue? attribValue = null;
                            foreach(var v in visitors)
                            {
                                if (v is ISdlTokenVisitor highLevelVisitor)
                                {
                                    attribValue ??= valueAsSdlValue(ref copy);
                                    highLevelVisitor.VisitNewAttribute(reader.ValueSpan, attribValue);
                                }
                                else if (v is ISdlTokenRawVisitor lowLevelVisitor)
                                    lowLevelVisitor.VisitNewAttribute(copy.TokenType, reader.ValueSpan, copy.ValueSpan);
                            }
                            reader.Read();
                            reader.Read();
                            break;
                        }
                        // either tag start, or an error
                        if(startOfLine)
                        {
                            foreach(var v in visitors)
                                v.VisitStartTag(reader.ValueSpan);
                            break;
                        }
                        throw new SdlException($"Orphaned identifier. Were you trying to make an attribute? Name = {reader.ValueSpan.ToString()}");

                    default: throw new SdlException($"Unexpected token of type {reader.TokenType}");
                }
                
                startOfLine = false;
                reader.Read();
            }
            foreach(var v in visitors) v.VisitEndOfFile();
        }
    }
}
