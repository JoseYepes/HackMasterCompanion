using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>Bloque de un conjunto de habilidades: su fila de título y las filas de sus componentes.</summary>
internal sealed record CustomSuiteBlock(RawRow Title, List<RawRow> Components);

/// <summary>Contenido de la pestaña "Conjuntos de habilidades" separado por tipo.</summary>
internal sealed record SuiteSheet(List<CustomSuiteBlock> Custom, List<RawRow> Official, List<RawRow> Styles);

/// <summary>
/// Separa los bloques de la pestaña "Conjuntos de habilidades". La pestaña no tiene cabecera
/// única: cada conjunto es una fila de título seguida de una fila de cabecera ("Habilidad", "PC"…)
/// y de sus componentes; los conjuntos oficiales y los estilos de combate son secciones con un
/// título, una cabecera ("Habilidad", "Tipo"…) y una fila por elemento.
/// </summary>
internal static class SuiteSheetParser
{
    private enum Section
    {
        None,
        Custom,
        Official,
        Styles,
    }

    public static SuiteSheet Parse(IReadOnlyList<RawRow> rows, List<ImportMessage> errors)
    {
        var sheet = new SuiteSheet([], [], []);
        var named = rows.Where(row => row["B"].Length > 0).OrderBy(row => row.RowNumber).ToList();
        var section = Section.None;

        for (var index = 0; index < named.Count; index++)
        {
            var row = named[index];
            if (IsHeader(row))
            {
                continue;
            }

            var next = index + 1 < named.Count ? named[index + 1] : null;
            if (next is not null && IsHeader(next))
            {
                if (TextNormalizer.Normalize(next["C"]) == "tipo")
                {
                    section = TextNormalizer.Normalize(row["B"]).Contains("estilo", StringComparison.Ordinal)
                        ? Section.Styles
                        : Section.Official;
                }
                else
                {
                    section = Section.Custom;
                    sheet.Custom.Add(new CustomSuiteBlock(row, []));
                }

                continue;
            }

            switch (section)
            {
                case Section.Custom:
                    sheet.Custom[^1].Components.Add(row);
                    break;
                case Section.Official:
                    sheet.Official.Add(row);
                    break;
                case Section.Styles:
                    sheet.Styles.Add(row);
                    break;
                default:
                    errors.Add(new ImportMessage(row.Sheet, row.RowNumber, row["B"], "la fila no pertenece a ningún conjunto ni sección."));
                    break;
            }
        }

        return sheet;
    }

    private static bool IsHeader(RawRow row) => TextNormalizer.Normalize(row["B"]) == "habilidad";
}
