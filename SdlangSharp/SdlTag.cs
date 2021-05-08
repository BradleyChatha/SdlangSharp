#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace SdlangSharp
{
    [DebuggerDisplay("Name = {QualifiedName} | Children = {Children.Count} | Attributes = {Attributes.Count} | Values = {Values.Count}")]
    public sealed class SdlTag : SdlNamedBase
    {
        public IList<SdlTag> Children { get; set; }
        public IDictionary<string, SdlAttribute> Attributes { get; set; }
        public IList<SdlValue> Values { get; set; }

        public SdlTag(string qualifiedName) : base(qualifiedName)
        {
            this.Ctor();
        }

        public SdlTag(string name, string @namespace) : base(name, @namespace)
        {
            this.Ctor();
        }

        private void Ctor()
        {
            this.Children = new List<SdlTag>();
            this.Attributes = new Dictionary<string, SdlAttribute>();
            this.Values = new List<SdlValue>();
        }

        // ;_; In the name user-usability, I salute my past (and future!) self.
        // This is why I miss D, all of this could've been auto-generated </3

        public bool HasValueAt(int valueIndex)      => valueIndex < this.Values.Count;
        public bool HasAttributeCalled(string name) => this.Attributes.ContainsKey(name);
        public bool HasChildAt(int childIndex)      => childIndex < this.Children.Count;
        public bool HasChildCalled(string name)     => this.Children.Any(c => c.QualifiedName == name);

        public IEnumerable<SdlTag> GetChildrenCalled(string name) => this.Children.Where(c => c.QualifiedName == name);

        public SdlValue         GetValue(int valueIndex)         => this.Values[valueIndex];
        public long             GetValueInteger(int valueIndex)  => this.Values[valueIndex];
        public double           GetValueFloating(int valueIndex) => this.Values[valueIndex];
        public bool             GetValueBoolean(int valueIndex)  => this.Values[valueIndex];
        public IList<byte>      GetValueBinary(int valueIndex)   => this.Values[valueIndex].Binary;
        public string           GetValueString(int valueIndex)   => this.Values[valueIndex];
        public DateTimeOffset   GetValueDateTime(int valueIndex) => this.Values[valueIndex];
        public TimeSpan         GetValueTimeSpan(int valueIndex) => this.Values[valueIndex];

        public SdlValue?        GetValueOrDefault(int valueIndex, SdlValue? @default = null)                => (valueIndex >= this.Values.Count) ? @default : this.Values[valueIndex];
        public long             GetValueIntegerOrDefault(int valueIndex, long @default = 0)                 => this.GetValueOrDefault(valueIndex) ?? @default;
        public double           GetValueFloatingOrDefault(int valueIndex, double @default = 0)              => this.GetValueOrDefault(valueIndex) ?? @default;
        public bool             GetValueBooleanOrDefault(int valueIndex, bool @default = false)             => this.GetValueOrDefault(valueIndex) ?? @default;
        public IList<byte>?     GetValueBinaryOrDefault(int valueIndex, IList<byte>? @default = null)       => this.GetValueOrDefault(valueIndex)?.Binary ?? @default;
        public string?          GetValueStringOrDefault(int valueIndex, string? @default = null)            => this.GetValueOrDefault(valueIndex) ?? @default;
        public DateTimeOffset   GetValueDateTimeOrDefault(int valueIndex, DateTimeOffset @default = default)=> this.GetValueOrDefault(valueIndex) ?? @default;
        public TimeSpan         GetValueTimeSpanOrDefault(int valueIndex, TimeSpan @default = default)      => this.GetValueOrDefault(valueIndex) ?? @default;

        public SdlAttribute     GetAttribute(string attribName)         => this.Attributes[attribName];
        public SdlValue         GetAttributeValue(string attribName)    => this.Attributes[attribName].Value!;
        public long             GetAttributeInteger(string attribName)  => this.Attributes[attribName].Value!;
        public double           GetAttributeFloating(string attribName) => this.Attributes[attribName].Value!;
        public bool             GetAttributeBoolean(string attribName)  => this.Attributes[attribName].Value!;
        public IList<byte>      GetAttributeBinary(string attribName)   => this.Attributes[attribName].Value!.Binary;
        public string           GetAttributeString(string attribName)   => this.Attributes[attribName].Value!;
        public DateTimeOffset   GetAttributeDateTime(string attribName) => this.Attributes[attribName].Value!;
        public TimeSpan         GetAttributeTimeSpan(string attribName) => this.Attributes[attribName].Value!;

        public SdlAttribute?    GetAttributeOrDefault(string name, SdlAttribute? @default = null)           => (!this.Attributes.ContainsKey(name)) ? @default : this.Attributes[name];
        public long             GetAttributeIntegerOrDefault(string name, long @default = 0)                => this.GetAttributeOrDefault(name)?.Value ?? @default;
        public double           GetAttributeFloatingOrDefault(string name, double @default = 0)             => this.GetAttributeOrDefault(name)?.Value ?? @default;
        public bool             GetAttributeBooleanOrDefault(string name, bool @default = false)            => this.GetAttributeOrDefault(name)?.Value ?? @default;
        public IList<byte>?     GetAttributeBinaryOrDefault(string name, IList<byte>? @default = null)      => this.GetAttributeOrDefault(name)?.Value?.Binary ?? @default;
        public string?          GetAttributeStringOrDefault(string name, string? @default = null)           => this.GetAttributeOrDefault(name)?.Value ?? @default;
        public DateTimeOffset   GetAttributeDateTimeOrDefault(string name, DateTimeOffset @default = default)=> this.GetAttributeOrDefault(name)?.Value ?? @default;
        public TimeSpan         GetAttributeTimeSpanOrDefault(string name, TimeSpan @default = default)      => this.GetAttributeOrDefault(name)?.Value ?? @default;
    }
}
