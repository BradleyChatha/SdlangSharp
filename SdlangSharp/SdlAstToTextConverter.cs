#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SdlangSharp
{
    public static class SdlAstToTextConverter
    {
        public static void WriteInto(StringBuilder builder, SdlTag tag, bool isRootNode = true, int indent = 0)
        {
            if(isRootNode)
            {
                foreach(var child in tag.Children)
                    WriteInto(builder, child, false, indent);
                return;
            }

            builder.Append(' ', indent * 4);

            if(tag.QualifiedName != SdlTokenPusher.ANONYMOUS_TAG_NAME)
            {
                builder.Append(tag.QualifiedName);
                builder.Append(' ');
            }

            foreach(var value in tag.Values)
                WriteInto(builder, value);

            foreach(var kvp in tag.Attributes)
                WriteInto(builder, kvp.Value);

            if(tag.Children.Count == 0)
            {
                builder.Append('\n');
                return;
            }

            builder.Append("{\n");

            foreach(var child in tag.Children)
                WriteInto(builder, child, false, indent + 1);

            builder.Append(' ', indent * 4);
            builder.Append("}\n");
        }

        public static void WriteInto(StringBuilder builder, SdlAttribute attribute)
        {
            builder.Append(attribute.QualifiedName);
            builder.Append('=');
            WriteInto(builder, attribute.Value ?? SdlValue.Null);
        }

        public static void WriteInto(StringBuilder builder, SdlValue value)
        {
            switch(value.Type)
            {
                case SdlValueType.Binary:
                    var bytesString = Convert.ToBase64String(value.Binary.ToArray()); // I don't like that ToArray, but it'll do for now.
                    builder.Append('[');
                    builder.Append(bytesString);
                    builder.Append(']');
                    break;

                case SdlValueType.Boolean:
                    builder.Append(value ? "true" : "false");
                    break;

                case SdlValueType.DateTime:
                    builder.Append(value.DateTimeOffset.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss"));
                    break;

                case SdlValueType.Floating:
                    var floatValue = value.Floating;
                    var formatChar = (floatValue > float.MaxValue || floatValue < float.MinValue)
                                     ? 'f' : 'd';
                    builder.Append(Convert.ToString(floatValue));
                    builder.Append(formatChar);
                    break;

                case SdlValueType.Integer:
                    var intValue = value.Integer;
                    builder.Append(Convert.ToString(intValue));
                    if(intValue > int.MaxValue || intValue < int.MinValue)
                        builder.Append('L');
                    break;

                case SdlValueType.Null:
                    builder.Append("null");
                    break;

                // TODO: Allow customisation of string type.
                case SdlValueType.String:
                    builder.Append("`"); // Lazy way out for now
                    builder.Append(value.String);
                    builder.Append('`');
                    break;

                case SdlValueType.TimeSpan:
                    builder.Append(value.TimeSpan.ToString(@"dd\d:HH:mm:ss"));
                    break;

                default: throw new SdlException($"Unhandled value type: {value.Type}");
            }

            builder.Append(' ');
        }
    }
}
