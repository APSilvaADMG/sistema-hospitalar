using System.Text.Json;
using System.Text.Json.Serialization;

namespace SistemaHospitalar.Application.Serialization;

/// <summary>
/// Enums registrados em <see cref="PortugueseEnumRegistry"/> são serializados em PT-BR na API.
/// </summary>
public sealed class PortugueseEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        PortugueseEnumRegistry.IsRegistered(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(PortugueseEnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class PortugueseEnumJsonConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                {
                    var numeric = reader.GetInt32();
                    if (Enum.IsDefined(typeof(T), numeric))
                    {
                        return (T)Enum.ToObject(typeof(T), numeric);
                    }

                    break;
                }
                case JsonTokenType.String:
                {
                    var raw = reader.GetString() ?? string.Empty;
                    if (PortugueseEnumRegistry.TryParse(typeof(T), raw, out var parsed) && parsed is T value)
                    {
                        return value;
                    }

                    if (Enum.TryParse<T>(raw, ignoreCase: true, out var english))
                    {
                        return english;
                    }

                    throw new JsonException($"Valor de enum inválido para {typeof(T).Name}: '{raw}'.");
                }
            }

            throw new JsonException($"Token JSON inválido para enum {typeof(T).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(PortugueseEnumRegistry.ToPortuguese(value));
        }
    }
}
