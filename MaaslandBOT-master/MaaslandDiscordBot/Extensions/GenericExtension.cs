using System;

namespace MaaslandDiscordBot.Extensions
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;

    using MaaslandDiscordBot.Models.Enums;

    public static class GenericExtension
    {
        public static bool IsNullOrDefault<T>(this T argument)
        {
            if (Equals(argument, default(T)))
            {
                return true;
            }

            var methodType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(methodType);

            if (underlyingType != null && Equals(argument, Activator.CreateInstance(underlyingType)))
            {
                return true;
            }

            var argumentType = argument.GetType();

            if (argumentType.IsValueType && argumentType != methodType)
            {
                var obj = Activator.CreateInstance(argument.GetType());
                return obj.Equals(argument);
            }

            return false;
        }

        public static string FirstLetterToUpperCaseOrConvertNullToEmptyString(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static int ToInt<T>(this T value)
        {
            return ToInt(value, default(int));
        }

        public static int ToInt<T>(this T value, int defaultValue)
        {
            var result = defaultValue;

            if (value.IsNullOrDefault())
            {
                return result;
            }

            var parse = value.ToString();
            
            while (parse.StartsWith("0") && parse.Length > 1)
            {
                parse = parse.Remove(0, 1);
            }

            return !int.TryParse(parse, out result)
                ? defaultValue
                : result;
        }

        public static async Task<T> GetValue<T>(this DbDataReader reader, string column)
        {
            if (reader.IsClosed)
            {
                return default;
            }

            var columnId = reader.GetOrdinal(column);

            return await reader.IsDBNullAsync(columnId) ? default : await reader.GetFieldValueAsync<T>(columnId);
        }

        public static string GetName(this IdType type)
        {
            switch (type)
            {
                case IdType.Unknown:
                    return string.Empty;
                case IdType.Identifier:
                    return "identifier";
                case IdType.License:
                    return "license";
                case IdType.XBOX:
                    return "xbl";
                case IdType.Live:
                    return "live";
                case IdType.Discord:
                    return "discord";
                case IdType.IP:
                    return "ip";
                default:
                    return string.Empty;
            }
        }

        public static string GenerateString(this Dictionary<string, bool> identifiers, string key)
        {
            var rawString = string.Empty;

            foreach (var identifier in identifiers)
            {
                if (string.IsNullOrEmpty(rawString))
                {
                    rawString = $"{(string.IsNullOrEmpty(key) ? "" : $"{key}:")}{identifier.Key}";
                }
                else
                {
                    rawString += $"\n{(string.IsNullOrEmpty(key) ? "" : $"{key}:")}{identifier.Key}";
                }
            }

            if (string.IsNullOrWhiteSpace(rawString))
            {
                rawString = "-";
            }

            return rawString;
        }

        public static string GenerateToRawString(this Dictionary<IdType, Dictionary<string, bool>> identifiers)
        {
            var rawString = "[";

            foreach (var type in identifiers)
            {
                var key = type.Key.GetName();

                if (type.Key == IdType.Identifier)
                {
                    key = string.Empty;
                }

                rawString = type.Value.Aggregate(rawString, (current, value) => current + $"\"{(string.IsNullOrEmpty(key) ? "" : $"{key}:")}{value.Key}\",");
            }

            if (!rawString.Equals("[", StringComparison.InvariantCultureIgnoreCase))
            {
                rawString = rawString.RemoveLast(",");
            }

            rawString += "]";

            return rawString;
        }

        public static string RemoveLast(this string text, string character)
        {
            return text.Length < 1 ? text : text.Remove(text.LastIndexOf(character, StringComparison.Ordinal), character.Length);
        }
    }
}
