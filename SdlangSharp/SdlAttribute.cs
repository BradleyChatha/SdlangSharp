#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SdlangSharp
{
    [DebuggerDisplay("Name = {QualifiedName} | Value = [{Value}]")]
    public sealed class SdlAttribute : SdlNamedBase
    {
        public SdlValue? Value { get; set; }

        public SdlAttribute(string qualifiedName, SdlValue? value = null) : base(qualifiedName)
        {
            this.Value = value;
        }

        public SdlAttribute(string name, string @namespace, SdlValue? value = null) : base(name, @namespace)
        {
            this.Value = value;
        }
    }
}
