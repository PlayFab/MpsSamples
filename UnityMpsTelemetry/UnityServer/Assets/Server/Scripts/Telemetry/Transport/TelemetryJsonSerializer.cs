namespace PlayFab.Samples.UnityMpsTelemetry
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public sealed class TelemetryJsonSerializer
    {
        public string SerializeObject(object value)
        {
            StringBuilder builder = new StringBuilder();
            WriteValue(builder, value);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string stringValue)
            {
                WriteString(builder, stringValue);
                return;
            }

            if (value is bool boolValue)
            {
                builder.Append(boolValue ? "true" : "false");
                return;
            }

            if (TryWriteNumber(builder, value))
            {
                return;
            }

            if (value is IDictionary<string, object> stringDictionary)
            {
                WriteStringDictionary(builder, stringDictionary);
                return;
            }

            if (value is IDictionary dictionary)
            {
                WriteDictionary(builder, dictionary);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                WriteEnumerable(builder, enumerable);
                return;
            }

            WriteString(builder, value.ToString());
        }

        private static void WriteStringDictionary(StringBuilder builder, IDictionary<string, object> dictionary)
        {
            builder.Append('{');
            bool isFirst = true;
            foreach (KeyValuePair<string, object> item in dictionary)
            {
                if (!isFirst)
                {
                    builder.Append(',');
                }

                WriteString(builder, item.Key);
                builder.Append(':');
                WriteValue(builder, item.Value);
                isFirst = false;
            }

            builder.Append('}');
        }

        private static void WriteDictionary(StringBuilder builder, IDictionary dictionary)
        {
            builder.Append('{');
            bool isFirst = true;
            foreach (DictionaryEntry item in dictionary)
            {
                if (!isFirst)
                {
                    builder.Append(',');
                }

                WriteString(builder, item.Key == null ? string.Empty : item.Key.ToString());
                builder.Append(':');
                WriteValue(builder, item.Value);
                isFirst = false;
            }

            builder.Append('}');
        }

        private static void WriteEnumerable(StringBuilder builder, IEnumerable enumerable)
        {
            builder.Append('[');
            bool isFirst = true;
            foreach (object item in enumerable)
            {
                if (!isFirst)
                {
                    builder.Append(',');
                }

                WriteValue(builder, item);
                isFirst = false;
            }

            builder.Append(']');
        }

        private static bool TryWriteNumber(StringBuilder builder, object value)
        {
            switch (value)
            {
                case byte _:
                case sbyte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case decimal _:
                    builder.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
                    return true;
                case float floatValue:
                    WriteFloatingPointNumber(builder, floatValue);
                    return true;
                case double doubleValue:
                    WriteFloatingPointNumber(builder, doubleValue);
                    return true;
                default:
                    return false;
            }
        }

        private static void WriteFloatingPointNumber(StringBuilder builder, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                builder.Append("null");
                return;
            }

            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (char character in value ?? string.Empty)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }
    }
}
