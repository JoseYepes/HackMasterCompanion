using System.Text.RegularExpressions;
using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>Resultado de analizar un requisito.</summary>
public sealed record PrerequisiteParseResult(
    Prerequisite Prerequisite,
    IReadOnlyList<string> Unresolved,
    bool HasUnverified);

/// <summary>
/// Analiza el texto de un requisito y enlaza los nombres de habilidades y conjuntos. No interpreta
/// la lógica "y/o": conserva el texto de la fuente partido en fragmentos.
/// </summary>
public static partial class PrerequisiteParser
{
    /// <summary>Separadores entre partes de un requisito: coma, punto y coma, dos puntos, " o " e " y ".</summary>
    [GeneratedRegex(@"(\s*[,;:]\s*|\s+[oy]\s+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Separator();

    /// <summary>Característica mínima: "Sabiduría 14+", "FUE 17".</summary>
    [GeneratedRegex(@"^(fuerza|destreza|constitucion|inteligencia|sabiduria|carisma|apariencia|fue|des|con|int|sab|car|asp) \d+\+?$")]
    private static partial Regex MinimumAbility();

    /// <summary>Competencia con un arma: "Competencia básica en el arma".</summary>
    [GeneratedRegex(@"^competencia\b")]
    private static partial Regex Proficiency();

    /// <summary>Nivel de maestría: "otra maniobra especial al 101%+", "2 más al 51%+".</summary>
    [GeneratedRegex(@"\d+ ?%\+?$")]
    private static partial Regex MasteryLevel();

    /// <summary>Cuantificador: "Al menos dos de".</summary>
    [GeneratedRegex(@"^al menos \w+ de$")]
    private static partial Regex AtLeast();

    public static PrerequisiteParseResult Parse(string? text, Func<string, (ElementKind Kind, string Slug)?> resolve)
    {
        var trimmed = (text ?? string.Empty).Trim();
        var normalized = TextNormalizer.Normalize(trimmed);

        if (normalized is "" or "n")
        {
            return new PrerequisiteParseResult(Prerequisite.None, [], false);
        }

        if (normalized is "s" or "?")
        {
            var unverified = new PrerequisiteSegment(trimmed, PrerequisiteSegmentKind.Unverified);
            return new PrerequisiteParseResult(new Prerequisite { Segments = [unverified] }, [], true);
        }

        // Los índices pares de "parts" son las partes; los impares, los separadores que las unen.
        var parts = Separator().Split(trimmed);
        var atomCount = (parts.Length + 1) / 2;
        var segments = new List<PrerequisiteSegment>();
        var unresolved = new List<string>();
        var hasUnverified = false;

        var index = 0;
        while (index < atomCount)
        {
            // Un nombre puede contener separadores ("Tasación: gemas"): se prueba primero el tramo más largo.
            var end = index;
            PrerequisiteSegment? segment = null;
            for (var last = atomCount - 1; last >= index && segment is null; last--)
            {
                var candidate = string.Concat(parts[(index * 2)..((last * 2) + 1)]);
                if (candidate.Length > 0 && resolve(candidate) is { } found)
                {
                    segment = new PrerequisiteSegment(candidate, ToSegmentKind(found.Kind), found.Slug);
                    end = last;
                }
            }

            var atom = parts[index * 2];
            if (segment is null && atom.Length > 0)
            {
                var kind = Classify(atom);
                segment = new PrerequisiteSegment(atom, kind ?? PrerequisiteSegmentKind.Text);
                hasUnverified |= kind == PrerequisiteSegmentKind.Unverified;
                if (kind is null)
                {
                    unresolved.Add(atom);
                }
            }

            if (segment is not null)
            {
                segments.Add(segment);
            }

            if (end < atomCount - 1)
            {
                segments.Add(new PrerequisiteSegment(parts[(end * 2) + 1], PrerequisiteSegmentKind.Text));
            }

            index = end + 1;
        }

        return new PrerequisiteParseResult(new Prerequisite { Segments = segments }, unresolved, hasUnverified);
    }

    /// <summary>Clasifica una parte que no es un nombre conocido; nulo si no se reconoce.</summary>
    private static PrerequisiteSegmentKind? Classify(string atom)
    {
        var normalized = TextNormalizer.Normalize(atom);

        if (normalized == "?" || normalized.EndsWith("(sin verificar)", StringComparison.Ordinal))
        {
            return PrerequisiteSegmentKind.Unverified;
        }

        var isCondition = normalized == "na"
            || MinimumAbility().IsMatch(normalized)
            || Proficiency().IsMatch(normalized)
            || MasteryLevel().IsMatch(normalized)
            || AtLeast().IsMatch(normalized);

        return isCondition ? PrerequisiteSegmentKind.Condition : null;
    }

    private static PrerequisiteSegmentKind ToSegmentKind(ElementKind kind) => kind switch
    {
        ElementKind.Skill => PrerequisiteSegmentKind.Skill,
        ElementKind.Suite => PrerequisiteSegmentKind.Suite,
        ElementKind.OfficialSuite => PrerequisiteSegmentKind.OfficialSuite,
        _ => PrerequisiteSegmentKind.Text,
    };
}
