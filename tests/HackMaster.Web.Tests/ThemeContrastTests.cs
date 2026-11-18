using System.Globalization;
using System.Text.RegularExpressions;

namespace HackMaster.Web.Tests;

/// <summary>
/// Colores de los dos modos (F007): cada texto se lee sobre su fondo con el contraste de WCAG 2.1
/// (FR-006) y ninguna regla usa colores sueltos que se quedarían sin modo noche (FR-002, FR-009).
/// Lee la hoja de estilos de la aplicación (specs/005-estilo-fantasia/research.md §2).
/// </summary>
public sealed partial class ThemeContrastTests
{
    private const string LightBlock = ":root";
    private const string DarkBlock = "html[data-theme=\"dark\"]";

    private static readonly string Css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "css", "app.css"));

    /// <summary>Texto (primero) sobre fondo (segundo) que debe llegar a 4,5:1.</summary>
    public static TheoryData<string, string> TextPairs => new()
    {
        { "--ink", "--paper" },
        { "--ink", "--paper-deep" },
        { "--ink", "--card" },
        { "--muted", "--paper-deep" },
        { "--accent", "--paper-deep" },
        { "--ink", "--surface" },
        { "--muted", "--paper" },
        { "--muted", "--card" },
        { "--muted", "--surface" },
        { "--accent", "--paper" },
        { "--accent", "--card" },
        { "--accent", "--surface" },
        { "--on-accent", "--accent" },
        { "--nav-ink", "--nav-bg" },
        { "--nav-active", "--nav-bg" },
        { "--button-ink", "--button-bg" },
        { "--warning-ink", "--warning-bg" },
        { "--accent", "--warning-bg" },
        { "--error", "--card" },
        { "--error", "--error-bg" },
    };

    /// <summary>Bordes de controles, foco y dados sobre la mesa: 3:1.</summary>
    public static TheoryData<string, string> GraphicPairs => new()
    {
        { "--line", "--paper" },
        { "--line", "--card" },
        { "--focus", "--paper" },
        { "--focus", "--card" },
        { "--nav-focus", "--nav-bg" },
        { "--gold", "--nav-bg" },
    };

    /// <summary>
    /// Los colores con los que el módulo 3D pinta los dados (normales y extra por penetración) se
    /// distinguen de la mesa de los dos modos (3:1) y sus números se leen (4,5:1). Se leen del propio
    /// módulo para probar lo que de verdad se dibuja (revisión de F007).
    /// </summary>
    /// <remarks>
    /// F008: los dados extra ya no tienen un color propio (llevan el aspecto del jugador) sino un
    /// aura de color <c>AURA_COLOR</c>, que es lo que debe destacar sobre la mesa; el color del dado y
    /// de los números lo comprueban las reglas de <c>DiceLook</c> (FR-003).
    /// </remarks>
    [Theory]
    [InlineData("DIE_COLOR", "DIE_INK")]
    [InlineData("AURA_COLOR", null)]
    public void Dice_Stand_Out_On_The_Table_In_Both_Modes__FR006_FR012(string body, string? ink)
    {
        var dice = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "js", "dice3d.js"));
        string Constant(string name) => Regex.Match(dice, $@"const {name} = '(?<value>#[0-9a-fA-F]{{6}})'").Groups["value"].Value;
        var color = Constant(body);
        Assert.False(string.IsNullOrEmpty(color), $"Falta la constante {body} en dice3d.js.");

        foreach (var selector in new[] { LightBlock, DarkBlock })
        {
            foreach (var table in new[] { "--table", "--table-light" })
            {
                var ratio = Contrast(color, Variables(selector)[table]);
                Assert.True(ratio >= 3, $"{selector}: {body} ({color}) sobre {table} tiene {ratio:0.00}:1; hace falta 3:1.");
            }
        }

        if (ink is not null)
        {
            Assert.True(Contrast(Constant(ink), color) >= 4.5, $"{ink} sobre {body} no llega a 4,5:1.");
        }
    }

    [Theory]
    [MemberData(nameof(TextPairs))]
    public void Text_Has_4_5_Contrast_In_Both_Modes__FR006(string text, string background)
    {
        AssertContrast(LightBlock, text, background, 4.5);
        AssertContrast(DarkBlock, text, background, 4.5);
    }

    [Theory]
    [MemberData(nameof(GraphicPairs))]
    public void Borders_Focus_And_Dice_Have_3_Contrast_In_Both_Modes__FR006_FR012(string color, string background)
    {
        AssertContrast(LightBlock, color, background, 3);
        AssertContrast(DarkBlock, color, background, 3);
    }

    [Fact]
    public void Both_Modes_Define_The_Same_Colors__FR002()
    {
        var light = Variables(LightBlock).Keys.Where(IsColor(LightBlock)).Order();
        var dark = Variables(DarkBlock).Keys.Order();

        Assert.Equal(light, dark);
    }

    [Fact]
    public void Rules_Use_Only_Color_Variables__FR002_FR009()
    {
        var rest = Css.Replace(Block(LightBlock), string.Empty, StringComparison.Ordinal)
            .Replace(Block(DarkBlock), string.Empty, StringComparison.Ordinal);
        rest = Comments().Replace(rest, string.Empty);

        var loose = LooseColors().Matches(rest).Select(match => match.Value).Distinct().ToList();

        Assert.True(loose.Count == 0, "Colores sueltos fuera de las variables: " + string.Join(", ", loose));
    }

    private static Func<string, bool> IsColor(string selector)
    {
        var variables = Variables(selector);
        return name => HexColor().IsMatch(variables[name]);
    }

    private static void AssertContrast(string selector, string foreground, string background, double minimum)
    {
        var variables = Variables(selector);
        Assert.True(variables.ContainsKey(foreground), $"Falta {foreground} en {selector}.");
        Assert.True(variables.ContainsKey(background), $"Falta {background} en {selector}.");

        var ratio = Contrast(variables[foreground], variables[background]);

        Assert.True(ratio >= minimum, $"{selector}: {foreground} ({variables[foreground]}) sobre {background} ({variables[background]}) tiene {ratio:0.00}:1; hace falta {minimum}:1.");
    }

    /// <summary>Contraste de WCAG 2.1 entre dos colores #rrggbb.</summary>
    private static double Contrast(string first, string second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(string hex)
    {
        Assert.Matches(HexColor(), hex);
        double Channel(int index)
        {
            var value = int.Parse(hex.AsSpan(1 + (2 * index), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(0)) + (0.7152 * Channel(1)) + (0.0722 * Channel(2));
    }

    private static Dictionary<string, string> Variables(string selector) =>
        Variable().Matches(Block(selector)).ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value.Trim());

    /// <summary>El primer bloque cuyo selector es exactamente el indicado, con sus llaves.</summary>
    private static string Block(string selector)
    {
        var start = Css.IndexOf(selector + " {", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Falta el bloque {selector} en app.css.");
        var end = Css.IndexOf('}', start);
        return Css[start..(end + 1)];
    }

    [GeneratedRegex(@"(?<name>--[a-z0-9-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex Variable();

    [GeneratedRegex(@"^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comments();

    /// <summary>Hexadecimales, todas las funciones de color de CSS y los 148 nombres de color de CSS.</summary>
    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|\b(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color|color-mix|light-dark)\(|(?<![\w-])(?:aliceblue|antiquewhite|aqua|aquamarine|azure|beige|bisque|black|blanchedalmond|blue|blueviolet|brown|burlywood|cadetblue|chartreuse|chocolate|coral|cornflowerblue|cornsilk|crimson|cyan|darkblue|darkcyan|darkgoldenrod|darkgray|darkgreen|darkgrey|darkkhaki|darkmagenta|darkolivegreen|darkorange|darkorchid|darkred|darksalmon|darkseagreen|darkslateblue|darkslategray|darkslategrey|darkturquoise|darkviolet|deeppink|deepskyblue|dimgray|dimgrey|dodgerblue|firebrick|floralwhite|forestgreen|fuchsia|gainsboro|ghostwhite|gold|goldenrod|gray|green|greenyellow|grey|honeydew|hotpink|indianred|indigo|ivory|khaki|lavender|lavenderblush|lawngreen|lemonchiffon|lightblue|lightcoral|lightcyan|lightgoldenrodyellow|lightgray|lightgreen|lightgrey|lightpink|lightsalmon|lightseagreen|lightskyblue|lightslategray|lightslategrey|lightsteelblue|lightyellow|lime|limegreen|linen|magenta|maroon|mediumaquamarine|mediumblue|mediumorchid|mediumpurple|mediumseagreen|mediumslateblue|mediumspringgreen|mediumturquoise|mediumvioletred|midnightblue|mintcream|mistyrose|moccasin|navajowhite|navy|oldlace|olive|olivedrab|orange|orangered|orchid|palegoldenrod|palegreen|paleturquoise|palevioletred|papayawhip|peachpuff|peru|pink|plum|powderblue|purple|rebeccapurple|red|rosybrown|royalblue|saddlebrown|salmon|sandybrown|seagreen|seashell|sienna|silver|skyblue|slateblue|slategray|slategrey|snow|springgreen|steelblue|tan|teal|thistle|tomato|turquoise|violet|wheat|white|whitesmoke|yellow|yellowgreen)(?![\w-])", RegexOptions.IgnoreCase)]
    private static partial Regex LooseColors();
}
