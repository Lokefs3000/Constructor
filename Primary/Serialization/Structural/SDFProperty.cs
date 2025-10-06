using System.Globalization;

namespace Primary.Serialization.Structural
{
    public sealed class SDFProperty : SDFBase
    {
        private SDFValueKind _valueKind;
        private string? _valueString;

        public SDFProperty()
        {
            _valueKind = SDFValueKind.None;
            _valueString = null;
        }

        public bool TryGetNumber(out double value) => double.TryParse(_valueString, CultureInfo.InvariantCulture, out value);
        public bool TryGetIntegral(out long value) => long.TryParse(_valueString, out value);
        public bool TryGetUIntegral(out ulong value) => ulong.TryParse(_valueString, out value);
        public bool TryGetBoolean(out bool value) => bool.TryParse(_valueString, out value);

        public double GetNumber() => double.Parse(_valueString!, CultureInfo.InvariantCulture);
        public long GetIntegral() => long.Parse(_valueString!);
        public ulong GetUIntegral() => ulong.Parse(_valueString!);
        public bool GetBoolean() => bool.Parse(_valueString!);

        public string? GetString() => _valueString;

        public SDFValueKind ValueKind => _valueKind;
        public string? RawValueString => _valueString;

        public object? Value
        {
            get
            {
                switch (_valueKind)
                {
                    case SDFValueKind.Number:
                        {
                            bool hasDecimal = _valueString!.Contains('.') || _valueString!.Contains(',');

                            if (hasDecimal)
                            {
                                if (double.TryParse(_valueString, out double doubleResult))
                                    return doubleResult;
                            }
                            else
                            {
                                bool isNegative = _valueString!.StartsWith('-');
                                if (!isNegative && ulong.TryParse(_valueString, out ulong ulongResult))
                                {
                                    return ulongResult > long.MaxValue ? ulongResult : (long)ulongResult;
                                }

                                if (long.TryParse(_valueString, out long longResult))
                                    return longResult;
                            }

                            return null;
                        }
                    case SDFValueKind.Boolean:
                        {
                            if (bool.TryParse(_valueString, out bool result))
                                return result;
                            return null;
                        }
                    case SDFValueKind.String: return _valueString;
                }

                return null;
            }
            set
            {
                if (value is IFormattable formattable)
                    _valueString = formattable.ToString(null, CultureInfo.InvariantCulture);
                else
                    _valueString = value?.ToString();
            }
        }
    }

    public enum SDFValueKind : byte
    {
        None = 0,

        Number,
        Boolean,
        String
    }
}
