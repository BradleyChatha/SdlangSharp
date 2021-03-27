using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SdlangSharp
{
    public enum SdlValueType
    {
        ERROR,
        Integer,
        Floating,
        Boolean,
        Null,
        Binary,
        String,
        DateTime,
        TimeSpan
    }
    
    [DebuggerDisplay("Type = {Type} | Value = {GetValueAsObject()}")]
    public sealed class SdlValue : ICloneable
    {
        public static SdlValue True  => new SdlValue(true);
        public static SdlValue False => new SdlValue(false);
        public static SdlValue Null  => new SdlValue(SdlValueType.Null);

        // idk whether it's better to store a single object and do casting, or to store a bunch of objects and just return
        // the appropriate one.
        long        _integer;
        double      _floating;
        bool        _boolean;
        IList<byte> _binary;
        string      _text;
        DateTimeOffset    _datetime;
        TimeSpan    _timespan;

        public SdlValueType Type { get; private set; }

        public long Integer
        {
            get => GetValueEnforceType(_integer, SdlValueType.Integer);
            set => SetValue(value, ref _integer, SdlValueType.Integer);
        }

        public double Floating
        {
            get => GetValueEnforceType(_floating, SdlValueType.Floating);
            set => SetValue(value, ref _floating, SdlValueType.Floating);
        }

        public bool Boolean
        {
            get => GetValueEnforceType(_boolean, SdlValueType.Boolean);
            set => SetValue(value, ref _boolean, SdlValueType.Boolean);
        }

        public IList<byte> Binary
        {
            get => GetValueEnforceType(_binary, SdlValueType.Binary);
            set => SetValue(value, ref _binary, SdlValueType.Binary);
        }

        public string String
        {
            get => GetValueEnforceType(_text, SdlValueType.String);
            set => SetValue(value, ref _text, SdlValueType.String);
        }

        public DateTimeOffset DateTimeOffset
        {
            get => GetValueEnforceType(_datetime, SdlValueType.DateTime);
            set => SetValue(value, ref _datetime, SdlValueType.DateTime);
        }

        public TimeSpan TimeSpan
        {
            get => GetValueEnforceType(_timespan, SdlValueType.TimeSpan);
            set => SetValue(value, ref _timespan, SdlValueType.TimeSpan);
        }

        public SdlValue(long integer)            => this.Integer = integer;
        public SdlValue(double floating)         => this.Floating = floating;
        public SdlValue(bool boolean)            => this.Boolean = boolean;
        public SdlValue(IList<byte> binary)      => this.Binary = binary;
        public SdlValue(string @string)          => this.String = @string;
        public SdlValue(DateTimeOffset dateTime) => this.DateTimeOffset = dateTime;
        public SdlValue(TimeSpan timeSpan)       => this.TimeSpan = timeSpan;
        private SdlValue(SdlValueType type)      => this.Type = type;

        public override bool Equals(object obj)
        {
            return 
                obj is SdlValue sdlValue
             && sdlValue.Type == this.Type
             && this.GetValueAsObject() == sdlValue.GetValueAsObject();
        }

        public override int GetHashCode()
        {
            return (((13 * 7) + (this.GetValueAsObject().GetHashCode())) * 7) + this.GetValueAsObject().GetHashCode();
        }

        public object Clone()
        {
            return this.Type switch
            {
                SdlValueType.Binary => new SdlValue(this.Binary),
                SdlValueType.Boolean => new SdlValue(this.Boolean),
                SdlValueType.DateTime => new SdlValue(this.DateTimeOffset),
                SdlValueType.Floating => new SdlValue(this.Floating),
                SdlValueType.Integer => new SdlValue(this.Integer),
                SdlValueType.Null => new SdlValue(SdlValueType.Null),
                SdlValueType.String => new SdlValue(this.String),
                SdlValueType.TimeSpan => new SdlValue(this.TimeSpan),
                _ => throw new Exception("This shouldn't happen")
            };
        }

        public static bool operator ==(SdlValue a, SdlValue b) => a.Equals(b);
        public static bool operator !=(SdlValue a, SdlValue b) => !a.Equals(b);
        public static bool operator <=(SdlValue a, SdlValue b) => a < b || a == b;
        public static bool operator >=(SdlValue a, SdlValue b) => a > b || a == b;
        public static bool operator <(SdlValue a, SdlValue b)
        {
            a.EnforceNumerical();
            b.EnforceNumerical();
            return (a.Type == SdlValueType.Integer)
                    ? (b.Type == SdlValueType.Integer) ? a.Integer < b.Integer : a.Integer < b.Floating
                    : (b.Type == SdlValueType.Integer) ? a.Floating < b.Integer : a.Floating < b.Floating;
        }
        public static bool operator >(SdlValue a, SdlValue b)
        {
            a.EnforceNumerical();
            b.EnforceNumerical();
            return (a.Type == SdlValueType.Integer)
                    ? (b.Type == SdlValueType.Integer) ? a.Integer > b.Integer : a.Integer > b.Floating
                    : (b.Type == SdlValueType.Integer) ? a.Floating > b.Integer : a.Floating > b.Floating;
        }
        public static bool operator <(SdlValue a, long b)
        {
            a.EnforceNumerical();
            return (a.Type == SdlValueType.Integer) ? a.Integer < b : a.Floating < b;
        }
        public static bool operator >(SdlValue a, long b)
        {
            a.EnforceNumerical();
            return (a.Type == SdlValueType.Integer) ? a.Integer > b : a.Floating > b;
        }
        public static bool operator <(SdlValue a, double b)
        {
            a.EnforceNumerical();
            return (a.Type == SdlValueType.Integer) ? a.Integer < b : a.Floating < b;
        }
        public static bool operator >(SdlValue a, double b)
        {
            a.EnforceNumerical();
            return (a.Type == SdlValueType.Integer) ? a.Integer > b : a.Floating > b;
        }
        public static implicit operator bool(SdlValue value) => value.GetValueEnforceType(value.Boolean, SdlValueType.Boolean);
        public static implicit operator long(SdlValue value) => value.GetValueEnforceType(value.Integer, SdlValueType.Integer);
        public static implicit operator double(SdlValue value) => value.GetValueEnforceType(value.Floating, SdlValueType.Floating);
        public static implicit operator string(SdlValue value) => value.GetValueEnforceType(value.String, SdlValueType.String);
        public static implicit operator DateTimeOffset(SdlValue value) => value.GetValueEnforceType(value.DateTimeOffset, SdlValueType.DateTime);
        public static implicit operator TimeSpan(SdlValue value) => value.GetValueEnforceType(value.TimeSpan, SdlValueType.TimeSpan);

        private void SetValue<T>(T propValue, ref T member, SdlValueType type)
        {
            member = propValue;
            this.Type = type;
        }

        private T GetValueEnforceType<T>(T value, SdlValueType type)
        {
            if (this.Type != type)
                throw new InvalidOperationException($"I am not a {type}, I am: {this.Type}");
            return value;
        }

        private object GetValueAsObject()
        {
            return this.Type switch
            {
                SdlValueType.Binary => this._binary,
                SdlValueType.Boolean => this._boolean,
                SdlValueType.DateTime => this._datetime,
                SdlValueType.Floating => this._floating,
                SdlValueType.Integer => this._integer,
                SdlValueType.Null => false,
                SdlValueType.String => this._text,
                SdlValueType.TimeSpan => this._timespan,
                _ => throw new InvalidOperationException("This should never happen."),
            };
        }
        
        private void EnforceNumerical()
        {
            if(this.Type != SdlValueType.Integer && this.Type != SdlValueType.Floating)
                throw new InvalidOperationException($"This value isn't not numerical, it is: {this.Type}");
        }
    }
}
