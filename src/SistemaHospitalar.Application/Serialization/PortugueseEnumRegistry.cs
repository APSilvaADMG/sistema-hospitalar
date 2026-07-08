using SistemaHospitalar.Domain.Enums;
using SistemaHospitalar.Domain.Entities;

namespace SistemaHospitalar.Application.Serialization;

/// <summary>
/// Rótulos PT-BR para serialização JSON de enums operacionais (wire format em português).
/// Aceita inglês, rótulo PT-BR e número na desserialização para compatibilidade.
/// </summary>
public static partial class PortugueseEnumRegistry
{
    private static readonly Dictionary<Type, EnumMap> Maps = BuildMaps();

    public static bool IsRegistered(Type enumType) =>
        enumType.IsEnum && Maps.ContainsKey(enumType);

    public static string ToPortuguese(Enum value)
    {
        if (!Maps.TryGetValue(value.GetType(), out var map))
        {
            return value.ToString();
        }

        return map.ToPortuguese.TryGetValue(value, out var pt) ? pt : value.ToString();
    }

    public static bool TryParse(Type enumType, string raw, out object? value)
    {
        value = null;
        if (!Maps.TryGetValue(enumType, out var map))
        {
            return false;
        }

        if (map.FromToken.TryGetValue(raw, out var parsed))
        {
            value = parsed;
            return true;
        }

        if (int.TryParse(raw, out var numeric) && Enum.IsDefined(enumType, numeric))
        {
            value = Enum.ToObject(enumType, numeric);
            return true;
        }

        return false;
    }

    private static Dictionary<Type, EnumMap> BuildMaps()
    {
        var maps = new Dictionary<Type, EnumMap>();
        RegisterClinicalMaps(maps);
        RegisterOperationsMaps(maps);
        RegisterAdministrativeMaps(maps);
        RegisterIntegrationMaps(maps);
        RegisterConnectMaps(maps);
        RegisterComplianceMaps(maps);
        return maps;
    }

    private static void Register<T>(Dictionary<Type, EnumMap> maps, params (T Value, string Portuguese)[] entries)
        where T : struct, Enum =>
        maps[typeof(T)] = BuildMap(entries);

    private static EnumMap BuildMap<T>(IEnumerable<(T Value, string Portuguese)> entries)
        where T : struct, Enum
    {
        var toPt = new Dictionary<Enum, string>();
        var fromToken = new Dictionary<string, Enum>(StringComparer.OrdinalIgnoreCase);

        foreach (var (value, portuguese) in entries)
        {
            var boxed = (Enum)Enum.ToObject(typeof(T), value)!;
            toPt[boxed] = portuguese;
            fromToken[portuguese] = boxed;
            fromToken[value.ToString()] = boxed;
        }

        return new EnumMap(typeof(T), toPt, fromToken);
    }

    private sealed record EnumMap(
        Type EnumType,
        Dictionary<Enum, string> ToPortuguese,
        Dictionary<string, Enum> FromToken);
}
