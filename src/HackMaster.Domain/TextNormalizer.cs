using System.Globalization;
using System.Text;

namespace HackMaster.Domain;

/// <summary>Normaliza texto para comparar y buscar sin distinguir acentos ni mayúsculas.</summary>
public static class TextNormalizer
{
    /// <summary>Devuelve el texto sin acentos, en minúsculas y con los espacios compactados.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    /// <summary>Devuelve un identificador para direcciones: letras y cifras separadas por guiones.</summary>
    public static string ToSlug(string? text)
    {
        var normalized = Normalize(text);
        var builder = new StringBuilder(normalized.Length);
        var pendingHyphen = false;

        foreach (var character in normalized)
        {
            if (!char.IsLetterOrDigit(character))
            {
                pendingHyphen = builder.Length > 0;
                continue;
            }

            if (pendingHyphen)
            {
                builder.Append('-');
                pendingHyphen = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
