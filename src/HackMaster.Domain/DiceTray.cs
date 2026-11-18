using System.Globalization;
using System.Text;

namespace HackMaster.Domain;

/// <summary>Resultado de pulsar un botón de la bandeja: la bandeja nueva, o la misma y el motivo.</summary>
public sealed record TrayChange(DiceTray Tray, string? Problem);

/// <summary>
/// Bandeja de dados: compone la fórmula del tirador a golpe de clic (US4, FR-017 a FR-021). Lleva
/// cuántos dados hay de cada tipo y un modificador; se escribe como "3d6+1d20+2".
/// </summary>
public sealed partial record DiceTray
{
    /// <summary>Dados de la bandeja, por número de caras (FR-017).</summary>
    public static IReadOnlyList<int> Sides { get; } = [4, 6, 8, 10, 12, 20, 100];

    public static DiceTray Empty { get; } = new();

    private readonly int[] _counts = new int[Sides.Count];

    private DiceTray()
    {
    }

    private DiceTray(int[] counts, int modifier)
    {
        _counts = counts;
        Modifier = modifier;
    }

    public int Modifier { get; }

    public int CountOf(int sides) => _counts[IndexOf(sides)];

    /// <summary>
    /// Bandeja equivalente a la fórmula escrita, o nulo si no la entiende: dados restados, dados que
    /// no están en la bandeja o texto que no es una fórmula (FR-020).
    /// </summary>
    public static DiceTray? FromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Empty;
        }

        // Solo números con su signo ("+1", "1+2"): el analizador de fórmulas los rechaza porque no hay
        // dados, pero para la bandeja son el modificador. Dos números sin signo entre ellos no valen.
        var onlyNumbers = OnlyNumbers().Match(text);
        if (onlyNumbers.Success)
        {
            var sum = 0L;
            foreach (System.Text.RegularExpressions.Capture term in onlyNumbers.Groups["term"].Captures)
            {
                var value = term.Value.Replace(" ", string.Empty, StringComparison.Ordinal);
                if (value.TrimStart('+', '-').TrimStart('0').Length > 9)
                {
                    return null;
                }

                sum += long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }

            return Math.Abs(sum) <= DiceFormula.MaxNumber ? new DiceTray(new int[Sides.Count], (int)sum) : null;
        }

        var parsed = DiceFormula.Parse(text);
        if (parsed.Formula is null)
        {
            return null;
        }

        var counts = new int[Sides.Count];
        var modifier = 0;
        foreach (var term in parsed.Formula.Terms)
        {
            if (!term.IsDice)
            {
                modifier += term.Sign * term.Constant;
                continue;
            }

            var index = Array.IndexOf([.. Sides], term.Sides);
            if (term.Sign < 0 || index < 0)
            {
                return null;
            }

            counts[index] += term.Count;
        }

        return Math.Abs(modifier) <= DiceFormula.MaxNumber ? new DiceTray(counts, modifier) : null;
    }

    /// <summary>Suma un dado de ese tipo, si no pasa del máximo de dados (FR-018, FR-021).</summary>
    public TrayChange Add(int sides)
    {
        var index = IndexOf(sides);
        if (_counts.Sum() + 1 > DiceFormula.MaxDice)
        {
            return new TrayChange(this, $"Como mucho se pueden tirar {DiceFormula.MaxDice} dados a la vez.");
        }

        var counts = (int[])_counts.Clone();
        counts[index]++;
        return new TrayChange(new DiceTray(counts, Modifier), null);
    }

    /// <summary>Cambia el modificador, si no pasa del máximo de FR-004 (FR-019, FR-021).</summary>
    public TrayChange AddModifier(int delta)
    {
        var modifier = Modifier + delta;
        return Math.Abs(modifier) > DiceFormula.MaxNumber
            ? new TrayChange(this, $"Los números no pueden pasar de {DiceFormula.MaxNumber}.")
            : new TrayChange(new DiceTray((int[])_counts.Clone(), modifier), null);
    }

    /// <summary>Fórmula: dados de menos a más caras y el modificador al final; vacía si no hay nada.</summary>
    public string ToText()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < Sides.Count; i++)
        {
            if (_counts[i] > 0)
            {
                builder.Append(builder.Length > 0 ? "+" : string.Empty)
                    .Append(_counts[i].ToString(CultureInfo.InvariantCulture))
                    .Append('d')
                    .Append(Sides[i].ToString(CultureInfo.InvariantCulture));
            }
        }

        if (Modifier != 0)
        {
            builder.Append(Modifier > 0 ? "+" : "-").Append(Math.Abs(Modifier).ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^\s*(?<term>[+-]?\s*\d+)(\s*(?<term>[+-]\s*\d+))*\s*$")]
    private static partial System.Text.RegularExpressions.Regex OnlyNumbers();

    private static int IndexOf(int sides)
    {
        for (var i = 0; i < Sides.Count; i++)
        {
            if (Sides[i] == sides)
            {
                return i;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(sides), sides, "Ese dado no está en la bandeja.");
    }
}
