namespace HackMaster.DataImport;

/// <summary>Error o aviso de la importación, localizable en la hoja.</summary>
public sealed record ImportMessage(string Sheet, IReadOnlyList<int> Rows, string? Element, string Text)
{
    public ImportMessage(string sheet, int row, string? element, string text)
        : this(sheet, [row], element, text)
    {
    }

    /// <summary>Formato: <c>[pestaña, fila n] «elemento»: mensaje</c>.</summary>
    public override string ToString()
    {
        var location = Rows.Count switch
        {
            0 => $"[{Sheet}]",
            1 => $"[{Sheet}, fila {Rows[0]}]",
            _ => $"[{Sheet}, filas {string.Join(", ", Rows.Take(Rows.Count - 1))} y {Rows[^1]}]",
        };

        return Element is null ? $"{location} {Text}" : $"{location} «{Element}»: {Text}";
    }
}
