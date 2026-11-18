using System.Globalization;
using System.Text;

namespace HackMaster.Domain;

/// <summary>
/// Un término de una fórmula: un grupo de dados ("2d6") o un número ("3"), con su signo (+1 o −1).
/// En un grupo, <see cref="Constant"/> vale 0; en un número, <see cref="Count"/> y <see cref="Sides"/> valen 0.
/// </summary>
public sealed record DiceTerm(int Sign, int Count, int Sides, int Constant)
{
    public bool IsDice => Sides > 0;

    public static DiceTerm Dice(int sign, int count, int sides) => new(sign, count, sides, 0);

    public static DiceTerm Number(int sign, int value) => new(sign, 0, 0, value);

    /// <summary>Texto del término sin signo: "2d6" o "3".</summary>
    public override string ToString() => IsDice
        ? $"{Count.ToString(CultureInfo.InvariantCulture)}d{Sides.ToString(CultureInfo.InvariantCulture)}"
        : Constant.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Error de una fórmula: dónde está (posición en el texto escrito), qué trozo y por qué.</summary>
public sealed record DiceFormulaError(int Position, string Fragment, string Message)
{
    /// <summary>Ejemplo de fórmula válida que acompaña a cada error (FR-003).</summary>
    public const string Example = "2d6+3";
}

/// <summary>Resultado de analizar una fórmula: la fórmula o su error.</summary>
public sealed record DiceParseResult(DiceFormula? Formula, DiceFormulaError? Error);

/// <summary>
/// Fórmula de dados: términos unidos por "+" o "-" (FR-001). Gramática:
/// <c>fórmula := [signo] término (signo término)*</c>, <c>término := [N] "d" X | entero</c>.
/// </summary>
public sealed record DiceFormula(string Text, IReadOnlyList<DiceTerm> Terms)
{
    public const int MaxLength = 100;
    public const int MaxDice = 100;
    public const int MinSides = 2;
    public const int MaxSides = 1000;
    public const int MaxNumber = 10000;

    /// <summary>Analiza el texto escrito por el jugador; nunca lanza excepciones.</summary>
    public static DiceParseResult Parse(string? input)
    {
        var text = input ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Fail(0, string.Empty, "Escribe una fórmula de dados.");
        }

        if (text.Trim().Length > MaxLength)
        {
            return Fail(MaxLength, text[MaxLength..], $"La fórmula no puede tener más de {MaxLength} caracteres.");
        }

        return new Parser(text).Run();
    }

    private static DiceParseResult Fail(int position, string fragment, string message) =>
        new(null, new DiceFormulaError(position, fragment, message));

    /// <summary>Analizador descendente sobre el texto original, para dar la posición de cada error.</summary>
    private sealed class Parser(string text)
    {
        private int _position;

        public DiceParseResult Run()
        {
            var terms = new List<DiceTerm>();
            var sign = 1;

            SkipSpaces();
            if (TryReadSign(out var leading))
            {
                sign = leading;
            }

            while (true)
            {
                var error = ReadTerm(sign, terms);
                if (error is not null)
                {
                    return error;
                }

                SkipSpaces();
                if (AtEnd)
                {
                    break;
                }

                var operatorAt = _position;
                if (!TryReadSign(out sign))
                {
                    return IsFormulaCharacter(Current)
                        ? Fail(_position, Current.ToString(), "Falta «+» o «-» entre dos términos.")
                        : InvalidCharacter();
                }

                SkipSpaces();
                if (AtEnd)
                {
                    return Fail(operatorAt, text[operatorAt].ToString(), $"Falta un dado o un número después de «{text[operatorAt]}».");
                }
            }

            if (!terms.Exists(term => term.IsDice))
            {
                return Fail(0, text.Trim(), "La fórmula debe tener al menos un dado, por ejemplo 1d20.");
            }

            var totalDice = terms.Where(term => term.IsDice).Sum(term => term.Count);
            if (totalDice > MaxDice)
            {
                return Fail(0, text.Trim(), $"Como mucho se pueden tirar {MaxDice} dados a la vez; esta fórmula tiene {totalDice}.");
            }

            return new DiceParseResult(new DiceFormula(Normalize(terms), terms), null);
        }

        private bool AtEnd => _position >= text.Length;

        private char Current => text[_position];

        private DiceParseResult? ReadTerm(int sign, List<DiceTerm> terms)
        {
            var start = _position;
            var hasCount = TryReadNumber(out var count, out var countText);

            if (!AtEnd && char.ToLowerInvariant(Current) == 'd')
            {
                var dAt = _position;
                _position++;
                if (!TryReadNumber(out var sides, out var sidesText))
                {
                    var fragment = AtEnd ? text[dAt].ToString() : text[dAt..(_position + 1)];
                    return Fail(dAt, fragment, "Falta el número de caras después de «d» (por ejemplo d6).");
                }

                if (!hasCount)
                {
                    count = 1;
                }
                else if (count < 1)
                {
                    return Fail(start, countText, "El número de dados debe ser al menos 1.");
                }

                if (count > MaxDice)
                {
                    return Fail(start, countText, $"Como mucho se pueden tirar {MaxDice} dados a la vez.");
                }

                if (sides < MinSides)
                {
                    return Fail(dAt + 1, sidesText, $"Un dado debe tener al menos {MinSides} caras.");
                }

                if (sides > MaxSides)
                {
                    return Fail(dAt + 1, sidesText, $"Un dado puede tener como mucho {MaxSides} caras.");
                }

                terms.Add(DiceTerm.Dice(sign, count, sides));
                return null;
            }

            if (hasCount)
            {
                if (count > MaxNumber)
                {
                    return Fail(start, countText, $"Los números no pueden pasar de {MaxNumber}.");
                }

                terms.Add(DiceTerm.Number(sign, count));
                return null;
            }

            return AtEnd
                ? Fail(start, string.Empty, "Falta un dado o un número.")
                : InvalidCharacter();
        }

        private DiceParseResult InvalidCharacter() =>
            Fail(_position, Current.ToString(), $"«{Current}» no vale en una fórmula: usa números, «d», «+» y «-».");

        /// <summary>Caracteres que pueden aparecer en una fórmula; cualquier otro se señala como no válido.</summary>
        private static bool IsFormulaCharacter(char character) =>
            char.IsAsciiDigit(character) || char.ToLowerInvariant(character) == 'd' || character is '+' or '-' || char.IsWhiteSpace(character);

        /// <summary>Lee un entero sin signo. Los muy largos valen <see cref="int.MaxValue"/> para fallar por el límite.</summary>
        private bool TryReadNumber(out int value, out string digits)
        {
            var start = _position;
            while (!AtEnd && char.IsAsciiDigit(Current))
            {
                _position++;
            }

            digits = text[start.._position];
            if (digits.Length == 0)
            {
                value = 0;
                return false;
            }

            value = digits.Length > 9 ? int.MaxValue : int.Parse(digits, CultureInfo.InvariantCulture);
            return true;
        }

        private bool TryReadSign(out int sign)
        {
            sign = 1;
            if (AtEnd || (Current != '+' && Current != '-'))
            {
                return false;
            }

            sign = Current == '-' ? -1 : 1;
            _position++;
            return true;
        }

        private void SkipSpaces()
        {
            while (!AtEnd && char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        private static string Normalize(List<DiceTerm> terms)
        {
            var builder = new StringBuilder();
            foreach (var term in terms)
            {
                if (term.Sign < 0)
                {
                    builder.Append('-');
                }
                else if (builder.Length > 0)
                {
                    builder.Append('+');
                }

                builder.Append(term);
            }

            return builder.ToString();
        }
    }
}
