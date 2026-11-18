using System.Globalization;
using System.Text.RegularExpressions;

namespace HackMaster.Domain;

/// <summary>Materiales de los dados 3D (F008, FR-001): los 11 que eligió el usuario.</summary>
public enum DiceMaterial
{
    Ivory,
    Marble,
    OldGold,
    BlackIron,
    Ruby,
    Obsidian,
    Bone,
    Amethyst,
    Wood,
    Stone,
    Crystal,
}

/// <summary>Efecto cuando un dado penetra («explota») (F008, FR-017).</summary>
public enum PenetrationEffect
{
    Explosion,
    Fire,
    Blood,
    None,
}

/// <summary>Calidad de dibujo de los dados (F008, FR-011).</summary>
public enum DiceQuality
{
    High,
    Low,
}

/// <summary>
/// Aspecto de los dados del jugador, común a todos los dados (F008, FR-001 a FR-004 y FR-009;
/// data-model). Los colores son <c>#rrggbb</c>.
/// </summary>
public sealed partial record DiceLook(
    DiceMaterial Material,
    string Color,
    string Ink,
    PenetrationEffect Effect,
    bool Sound,
    DiceQuality Quality)
{
    /// <summary>Números claros y oscuros que propone la aplicación si los elegidos no se leen.</summary>
    public const string LightInk = "#f8f1e0";

    public const string DarkInk = "#1f2328";

    /// <summary>Colores por defecto de cada material: los del prototipo enseñado al usuario.</summary>
    private static readonly Dictionary<DiceMaterial, (string Color, string Ink)> Defaults = new()
    {
        [DiceMaterial.Ivory] = ("#f4efe6", DarkInk),
        [DiceMaterial.Marble] = ("#2f5a46", "#f1e5c4"),
        [DiceMaterial.OldGold] = ("#d4a842", "#2a1608"),
        [DiceMaterial.BlackIron] = ("#4a4c52", "#e8b54a"),
        [DiceMaterial.Ruby] = ("#c0182a", "#ffe9b0"),
        [DiceMaterial.Obsidian] = ("#141018", "#c9a24a"),
        [DiceMaterial.Bone] = ("#e8dcc0", "#4a1a16"),
        [DiceMaterial.Amethyst] = ("#7b3fb5", "#f6ecff"),
        [DiceMaterial.Wood] = ("#8a5a32", "#f4e3c3"),
        [DiceMaterial.Stone] = ("#8c8a84", "#1f1d1a"),
        [DiceMaterial.Crystal] = ("#cfe3ee", "#1d3a4a"),
    };

    /// <summary>Marfil de F003, explosión, sonido activado (aclaración 3) y calidad alta.</summary>
    public static DiceLook Default { get; } = new(DiceMaterial.Ivory, "#f4efe6", DarkInk, PenetrationEffect.Explosion, Sound: true, DiceQuality.High);

    /// <summary>Colores por defecto de un material.</summary>
    public static (string Color, string Ink) DefaultColors(DiceMaterial material) => Defaults[material];

    /// <summary>El mismo aspecto con otro material y sus colores por defecto.</summary>
    public DiceLook ForMaterial(DiceMaterial material)
    {
        var (color, ink) = Defaults[material];
        return this with { Material = material, Color = color, Ink = ink };
    }

    /// <summary>Contraste de WCAG 2.1 entre el dado y sus números (FR-003).</summary>
    public double InkContrast => Contrast(Color, Ink);

    /// <summary>De los números claros y oscuros, los que más contrastan con el dado (FR-003).</summary>
    public string ReadableInk() => Contrast(Color, LightInk) >= Contrast(Color, DarkInk) ? LightInk : DarkInk;

    /// <summary>Si el texto es un color <c>#rrggbb</c>.</summary>
    public static bool IsColor(string? text) => text is not null && HexColor().IsMatch(text);

    /// <summary>Contraste de WCAG 2.1 entre dos colores <c>#rrggbb</c>.</summary>
    public static double Contrast(string first, string second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(string hex)
    {
        if (!IsColor(hex))
        {
            throw new ArgumentException($"«{hex}» no es un color #rrggbb.", nameof(hex));
        }

        double Channel(int index)
        {
            var value = int.Parse(hex.AsSpan(1 + (2 * index), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(0)) + (0.7152 * Channel(1)) + (0.0722 * Channel(2));
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
