using System.Globalization;
using System.Text.RegularExpressions;
using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>
/// Interpreta las celdas de un elemento. Lo que falta o no se entiende no se inventa ni se corrige:
/// queda nulo, anotado en <see cref="Pending"/> con su texto original en <see cref="Raw"/>, y se
/// avisa a través de <paramref name="onPending"/>.
/// </summary>
internal sealed partial class FieldReader(Action<string, string> onPending)
{
    private static readonly int[] ExistingDice = [4, 6, 8, 10, 12, 20];

    /// <summary>Nombres (como en el JSON) de los campos pendientes de verificar.</summary>
    public List<string> Pending { get; } = [];

    /// <summary>Texto original de los campos que no se pudieron interpretar.</summary>
    public SortedDictionary<string, string> Raw { get; } = new(StringComparer.Ordinal);

    [GeneratedRegex(@"\s*po$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GoldUnit();

    [GeneratedRegex(@"^d?(\d+)$")]
    private static partial Regex Die();

    /// <summary>Marca un campo como pendiente de verificar y avisa.</summary>
    public void MarkPending(string field, string raw, string message)
    {
        if (!Pending.Contains(field))
        {
            Pending.Add(field);
        }

        if (raw.Length > 0)
        {
            Raw[field] = raw;
        }

        onPending(field, message);
    }

    /// <summary>Entero (PC o precio en po). "NA" es nulo sin quedar pendiente.</summary>
    public int? Integer(string field, string label, string text)
    {
        if (!HasValue(field, label, text))
        {
            return null;
        }

        var cleaned = GoldUnit().Replace(text, string.Empty);
        if (TryNumber(cleaned, out var number) && number == Math.Floor(number))
        {
            return (int)number;
        }

        return Unreadable(field, label, text);
    }

    /// <summary>Dificultad en %. Una fracción (0,1) es un porcentaje con formato de celda: 10.</summary>
    public int? Percentage(string field, string label, string text)
    {
        if (!HasValue(field, label, text))
        {
            return null;
        }

        if (TryNumber(text.TrimEnd('%').Trim(), out var number))
        {
            if (number == Math.Floor(number))
            {
                return (int)number;
            }

            if (Math.Abs(number) < 1)
            {
                return (int)Math.Round(number * 100, MidpointRounding.AwayFromZero);
            }
        }

        return Unreadable(field, label, text);
    }

    /// <summary>Dado de maestría normalizado: d4…d20, "NA" o "*". Otro valor queda pendiente.</summary>
    public string? MasteryDie(string text, bool missingIsPending = true)
    {
        if (text.Length == 0)
        {
            if (missingIsPending)
            {
                MarkPending("masteryDie", string.Empty, "falta el dado de maestría; queda pendiente de verificar.");
            }

            return null;
        }

        var normalized = TextNormalizer.Normalize(text);
        if (normalized == "na")
        {
            return "NA";
        }

        if (normalized == "*")
        {
            return "*";
        }

        var match = Die().Match(normalized);
        if (match.Success && int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var sides) && ExistingDice.Contains(sides))
        {
            return $"d{sides}";
        }

        MarkPending("masteryDie", text, $"el dado de maestría «{text}» no es un dado existente; queda pendiente de verificar.");
        return null;
    }

    /// <summary>Características separadas por coma o barra. Las no reconocidas quedan pendientes.</summary>
    public IReadOnlyList<Ability> Abilities(string text)
    {
        if (text.Length == 0)
        {
            MarkPending("abilities", string.Empty, "falta la característica relevante; queda pendiente de verificar.");
            return [];
        }

        var abilities = new List<Ability>();
        var unknown = new List<string>();
        foreach (var token in text.Split([',', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<Ability>(token, ignoreCase: true, out var ability) && Enum.IsDefined(ability) && !char.IsDigit(token[0]))
            {
                if (!abilities.Contains(ability))
                {
                    abilities.Add(ability);
                }
            }
            else
            {
                unknown.Add(token);
            }
        }

        if (unknown.Count > 0)
        {
            MarkPending("abilities", text, $"la característica «{string.Join("», «", unknown)}» no se reconoce; queda pendiente de verificar.");
        }

        return abilities;
    }

    /// <summary>Libros separados por barra; de cada uno, el acrónimo y el resto como página.</summary>
    public IReadOnlyList<BookRef> Books(string text)
    {
        if (text.Length == 0 || text == "0")
        {
            MarkPending("books", string.Empty, "falta el libro; queda pendiente de verificar.");
            return [];
        }

        var books = new List<BookRef>();
        foreach (var reference in text.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = reference.IndexOf(' ', StringComparison.Ordinal);
            books.Add(separator < 0
                ? new BookRef(reference, null)
                : new BookRef(reference[..separator], reference[(separator + 1)..].Trim()));
        }

        return books;
    }

    /// <summary>Clases o grupos permitidos, separados por coma.</summary>
    public IReadOnlyList<string> Classes(string text)
    {
        if (text.Length == 0 || text == "0")
        {
            MarkPending("classes", string.Empty, "faltan las clases permitidas; queda pendiente de verificar.");
            return [];
        }

        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private bool HasValue(string field, string label, string text)
    {
        if (text.Length == 0)
        {
            MarkPending(field, string.Empty, $"falta {label}; queda pendiente de verificar.");
            return false;
        }

        return TextNormalizer.Normalize(text) != "na";
    }

    private int? Unreadable(string field, string label, string text)
    {
        MarkPending(field, text, $"{label} «{text}» no se puede interpretar; queda pendiente de verificar.");
        return null;
    }

    private static bool TryNumber(string text, out double number) =>
        double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
}
