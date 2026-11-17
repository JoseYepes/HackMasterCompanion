using System.Globalization;
using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>Formato de los datos para la interfaz: números sin separador de miles, libros como acrónimo y página.</summary>
public static class Display
{
    /// <summary>Texto para un valor que falta.</summary>
    public const string Missing = "—";

    /// <summary>Número entero sin separador de miles, o "—".</summary>
    public static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? Missing;

    /// <summary>Porcentaje, o "—".</summary>
    public static string Percent(int? value) => value is null ? Missing : Number(value) + " %";

    /// <summary>Referencia de libro: "MJ 329".</summary>
    public static string Book(BookRef book) => book.Page is null ? book.Acronym : $"{book.Acronym} {book.Page}";

    /// <summary>Varias referencias separadas por " / ", o "—".</summary>
    public static string Books(IReadOnlyList<BookRef> books) => books.Count == 0 ? Missing : string.Join(" / ", books.Select(Book));

    /// <summary>Lista de textos separados por comas, o "—".</summary>
    public static string List<T>(IReadOnlyList<T> values) => values.Count == 0 ? Missing : string.Join(", ", values);
}
