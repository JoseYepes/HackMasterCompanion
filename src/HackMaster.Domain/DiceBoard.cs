using System.Globalization;

namespace HackMaster.Domain;

/// <summary>Formas de dado que se pueden dibujar en 3D.</summary>
public enum DieShape
{
    D4,
    D6,
    D8,
    D10,

    /// <summary>d10 de decenas de un d100: caras 00, 10 … 90.</summary>
    D10Tens,
    D12,
    D20,
}

/// <summary>
/// Un dado que se dibuja: su forma, la cara que debe quedar arriba, la ola en la que rueda (0 la
/// tirada inicial; 1, 2… los dados extra por penetración) y si es un dado extra. <c>Parent</c> (F008):
/// en un dado extra, el índice en <see cref="BoardPlan.Dice"/> del dado que penetró (en un d100, el
/// d10 de las unidades); nulo en los demás.
/// </summary>
public sealed record BoardDie(DieShape Shape, string Face, int Wave, bool IsPenetration, int? Parent = null);

/// <summary>
/// Lo que se dibuja de una tirada. <see cref="TooMany"/>: la tirada empieza con más de
/// <see cref="DiceBoard.MaxDice"/> dados y no se dibuja. <see cref="Truncated"/>: los dados extra
/// por penetración no caben todos; se dibujan los primeros.
/// </summary>
public sealed record BoardPlan(IReadOnlyList<BoardDie> Dice, IReadOnlyList<DiceTerm> Unshaped, bool TooMany, bool Truncated);

/// <summary>Traduce una tirada a los dados que se dibujan en 3D (data-model de F003).</summary>
public static class DiceBoard
{
    /// <summary>Dados en pantalla como mucho (FR-007). Un d100 cuenta dos.</summary>
    public const int MaxDice = 20;

    public static BoardPlan Plan(RollResult result)
    {
        var unshaped = new List<DiceTerm>();
        // Cada «tirada física» es un dado, o los dos d10 de un d100, que no se separan.
        // Parent: la tirada física anterior de la misma cadena de penetración (índice en throws).
        var throws = new List<(int Wave, IReadOnlyList<BoardDie> Dice, int? Parent)>();

        foreach (var group in result.Groups)
        {
            if (!IsDrawable(group.Term.Sides))
            {
                if (!unshaped.Contains(group.Term))
                {
                    unshaped.Add(group.Term);
                }

                continue;
            }

            var wave = 0;
            foreach (var die in group.Dice)
            {
                wave = die.IsPenetration ? wave + 1 : 0;
                throws.Add((wave, Draw(die, wave), die.IsPenetration ? throws.Count - 1 : null));
            }
        }

        // OrderBy es estable: respeta el orden de la tirada.
        var ordered = throws.Select((item, index) => (item.Wave, item.Dice, item.Parent, Index: index)).OrderBy(item => item.Wave).ToList();
        if (ordered.Where(item => item.Wave == 0).Sum(item => item.Dice.Count) > MaxDice)
        {
            return new BoardPlan([], unshaped, TooMany: true, Truncated: false);
        }

        var dice = new List<BoardDie>();
        var lastDieOfThrow = new Dictionary<int, int>();
        var truncated = false;
        foreach (var (_, items, parent, index) in ordered)
        {
            if (dice.Count + items.Count > MaxDice)
            {
                truncated = true;
                break;
            }

            // La tirada padre tiene una ola menos, así que ya está dibujada.
            int? parentDie = parent is int p ? lastDieOfThrow[p] : null;
            dice.AddRange(items.Select(item => item with { Parent = parentDie }));
            lastDieOfThrow[index] = dice.Count - 1;
        }

        return new BoardPlan(dice, unshaped, TooMany: false, truncated);
    }

    private static bool IsDrawable(int sides) => sides is 4 or 6 or 8 or 10 or 12 or 20 or 100;

    private static IReadOnlyList<BoardDie> Draw(DieRoll die, int wave)
    {
        if (die.Sides != 100)
        {
            return [new BoardDie(ShapeOf(die.Sides), Text(die.Value), wave, die.IsPenetration)];
        }

        // d100 como en la mesa: 00 y 0 es 100; 00 y 7 es 7; 70 y 0 es 70.
        var tens = die.Value == 100 ? 0 : die.Value / 10 * 10;
        var units = die.Value % 10;
        return
        [
            new BoardDie(DieShape.D10Tens, tens == 0 ? "00" : Text(tens), wave, die.IsPenetration),
            new BoardDie(DieShape.D10, Text(units), wave, die.IsPenetration),
        ];
    }

    private static DieShape ShapeOf(int sides) => sides switch
    {
        4 => DieShape.D4,
        6 => DieShape.D6,
        8 => DieShape.D8,
        10 => DieShape.D10,
        12 => DieShape.D12,
        _ => DieShape.D20,
    };

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
}
