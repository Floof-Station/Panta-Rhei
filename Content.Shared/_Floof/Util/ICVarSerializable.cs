using System.Linq;
using Robust.Shared.Configuration;

namespace Content.Shared._Floof.Util;

/// <summary>
///     Indicates that instances and lists of instances of this class can be serialized into a cvar-safe string
/// </summary>
public interface ICVarSerializable
{
    /// <summary>
    ///     Returns a list of fields that need to be serialized.
    /// </summary>a
    public IEnumerable<string> SerializeFields();

    /// <summary>
    ///     Takes a list of fields from a serialized object and deserializes them in-place.
    /// </summary>
    public void DeserializeFields(string[] fields);
}

// Note: all of this is mostly going to be used on the client side (the server has the database to store preferences), so i'm not bothering with optimization
public static class CVarSerializableHelpers
{
    public const string FieldSeparator = ",";
    public const string ListSeparator  = ";";
    // Escapes
    private const string Escape = "&";
    private const string EscapedEscape = $"{Escape}{Escape}";
    private const string EscapedFieldSeparator = $"{Escape}f";
    private const string EscapedListSeparator = $"{Escape}l";

    private static string EscapeField(string s) => s
        .Replace(Escape, EscapedEscape) // escape the escape char FIRST
        .Replace(FieldSeparator, EscapedFieldSeparator)
        .Replace(ListSeparator,  EscapedListSeparator);

    private static string UnescapeField(string s) => s
        .Replace(EscapedFieldSeparator, FieldSeparator)
        .Replace(EscapedListSeparator, ListSeparator)
        .Replace(EscapedEscape, Escape); // collapse the escape char LAST

    /// <summary>
    ///     Encodes this ICVarSerializable into a cvar string.
    /// </summary>
    public static string EncodeToString<T>(this T item) where T : ICVarSerializable =>
        string.Join(FieldSeparator, item.SerializeFields().Select(EscapeField));

    /// <summary>
    ///     Decodes a ICVarSerializable in-place from a cvar string. Returns the same item for convenience.
    /// </summary>
    public static T DecodeFromString<T>(this T item, string encoded) where T : ICVarSerializable
    {
        item.DeserializeFields(encoded.Split(FieldSeparator).Select(UnescapeField).ToArray());
        return item;
    }

    /// <summary>
    ///     Encodes a LIST of ICVarSerializable into a cvar string.
    /// </summary>
    public static string EncodeListToString<T>(IEnumerable<T> items) where T : ICVarSerializable =>
        string.Join(ListSeparator, items.Select(it => it.EncodeToString()));

    /// <summary>
    ///     Decodes a LIST of ICVarSerializable from a cvar string.
    /// </summary>
    public static IEnumerable<T> DecodeListFromString<T>(string encoded, Func<T> factory) where T : ICVarSerializable =>
        encoded.Split(ListSeparator).Select(part => DecodeFromString(factory(), part));

    /// <summary>
    ///     Encodes a list of ICVarSerializable into the given CVar.
    /// </summary>
    public static void EncodeListIntoCVar<T>(this IConfigurationManager cfg, CVarDef<string> cvar, IEnumerable<T> items) where T : ICVarSerializable
    {
        var encoded = EncodeListToString(items);
        cfg.SetCVar(cvar, encoded);
    }

    /// <summary>
    ///     Decodes a list of ICVarSerializable from a given CVar.
    /// </summary>
    public static IEnumerable<T> DecodeListFromCVar<T>(this IConfigurationManager cfg, CVarDef<string> cvar, Func<T> factory) where T : ICVarSerializable
    {
        var encoded = cfg.GetCVar(cvar);
        var decoded = DecodeListFromString(encoded, factory);
        return decoded;
    }
}
