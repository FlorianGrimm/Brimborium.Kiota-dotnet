using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Brimborium.Kiota.Abstractions.Extensions;

namespace Brimborium.Kiota.Abstractions.Helpers {
    /// <summary>
    /// Helper methods for enums
    /// </summary>
    public static class EnumHelpers {
        /// <summary>
        /// Gets the enum value from the raw value
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="rawValue">Raw value</param>
        /// <returns></returns>
        public static T? GetEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string rawValue) where T : struct, Enum {
            if(string.IsNullOrEmpty(rawValue)) {
                return null;
            }

            rawValue = ToEnumRawName<T>(rawValue!);
            if(typeof(T).IsDefined(typeof(FlagsAttribute))) {
                ReadOnlySpan<char> valueSpan = rawValue.AsSpan();
                int value = 0;
                while(valueSpan.Length > 0) {
                    int commaIndex = valueSpan.IndexOf(',');
                    ReadOnlySpan<char> valueNameSpan = commaIndex < 0 ? valueSpan : valueSpan.Slice(0, commaIndex);
                    valueNameSpan = ToEnumRawName<T>(valueNameSpan);

                    if(Enum.TryParse<T>(valueNameSpan, true, out var result)) {
                        value |= (int)(object)result;
                    }

                    valueSpan = commaIndex < 0 ? ReadOnlySpan<char>.Empty : valueSpan.Slice(commaIndex + 1);
                }
                return (T)(object)value;
            }
            else {
                return Enum.TryParse<T>(rawValue, true, out var result) ? result : null;
            }
        }

        private static string ToEnumRawName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string value) where T : struct, Enum {
            return TryGetFieldValueName(typeof(T), value, out var val) ? val : value;
        }

        private static ReadOnlySpan<char> ToEnumRawName<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(ReadOnlySpan<char> span) where T : struct, Enum {
            return TryGetFieldValueName(typeof(T), span.ToString(), out var val) ? val.AsSpan() : span;
        }

        /// <summary>
        /// Gets the enum value from the raw value for the given type
        /// </summary>
        /// <param name="type">Enum type</param>
        /// <param name="rawValue">Raw value</param>
        /// <returns></returns>
        public static object? GetEnumValue([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type? type, string rawValue) {
            object? result;
            if(type == null) {
                return null;
            }
            Type enumType = (Nullable.GetUnderlyingType(type) is { IsEnum: true } underlyingType) ? underlyingType : type;
            if(enumType.IsDefined(typeof(FlagsAttribute))) {
                int intValue = 0;
                ReadOnlySpan<char> valueSpan = rawValue.AsSpan();
                while(valueSpan.Length > 0) {
                    int commaIndex = valueSpan.IndexOf(',');
                    ReadOnlySpan<char> valueNameSpan = commaIndex < 0 ? valueSpan : valueSpan.Slice(0, commaIndex);
                    if(TryGetFieldValueName(enumType, valueNameSpan, out var valueName)) {
                        valueNameSpan = valueName.AsSpan();
                    }
                    if(Enum.TryParse(enumType, valueNameSpan.ToString(), true, out var enumPartResult)) {
                        intValue |= (int)enumPartResult!;
                    }
                    valueSpan = commaIndex < 0 ? ReadOnlySpan<char>.Empty : valueSpan.Slice(commaIndex + 1);
                }
                result = intValue > 0 ? Enum.Parse(enumType, intValue.ToString(), true) : null;
            }
            else {
                if(TryGetFieldValueName(enumType, rawValue, out var value)) {
                    rawValue = value;
                }

                _ = Enum.TryParse(enumType, rawValue, true, out object? enumResult);
                result = enumResult;
            }
            return result;
        }

        private static bool TryGetFieldValueName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type, string rawValue, out string valueName) {
            valueName = string.Empty;
            foreach(var field in type.GetFields()) {
                if(field.GetFieldMemberName() is { } attr
                    && rawValue.Equals(attr, StringComparison.Ordinal)) {
                    valueName = field.Name;
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetFieldValueName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type, ReadOnlySpan<char> rawValue, out string valueName) {
            valueName = string.Empty;
            foreach(var field in type.GetFields()) {
                if(field.GetFieldMemberName() is { } attr
                    && rawValue.Equals(attr.AsSpan(), StringComparison.Ordinal)) {
                    valueName = field.Name;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the enum string representation of the given value. Looks up if there is an <see cref="EnumMemberAttribute"/> and returns the value if found, otherwise returns the enum name in camel case.
        /// </summary>
        /// <typeparam name="T">The Enum type</typeparam>
        /// <param name="value">The enum value</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If value is null</exception>
        public static string? GetEnumStringValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(T value) where T : struct, Enum {
            var type = typeof(T);

            if(Enum.GetName(type, value) is not { } name) {
                throw new ArgumentException($"Invalid Enum value {value} for enum of type {type}");
            }

            if(type.GetField(name)?.GetFieldMemberName() is { } attributeValue) {
                return attributeValue;
            }

            return name;
        }

        private static ConcurrentDictionary<FieldInfo, string?>? _EnumMemberCache;

        private static string? GetFieldMemberName(this FieldInfo field) {
            var enumMemberCache =
                (_EnumMemberCache ??= new());
            if(enumMemberCache.TryGetValue(field, out var cachedValue)) {
                return cachedValue;
            }
            else {
                if(field.GetCustomAttribute<EnumMemberAttribute>() is { Value: not null } attribute) {
                    enumMemberCache[field] = attribute.Value;
                    return attribute.Value;
                }
                else {
                    enumMemberCache[field] = null;
                    return null;
                }
            }
        }
    }
}
