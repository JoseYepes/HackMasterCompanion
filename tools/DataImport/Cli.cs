using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>Línea de comandos de la importación (contrato en specs/002-consulta-habilidades/contracts).</summary>
public static class Cli
{
    public const int Success = 0;
    public const int DataErrors = 1;
    public const int UsageError = 2;

    private const string DefaultDoubtsDocument = "docs/dudas.md";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length is < 2 or > 3)
        {
            error.WriteLine("Uso: DataImport <hoja.xlsx> <carpeta-datos> [<archivo-dudas>]");
            error.WriteLine("Ejemplo (PowerShell): dotnet run --project tools/DataImport -- \"$env:HACKMASTER_EXCEL\" data/");
            return UsageError;
        }

        var workbookPath = args[0];
        var dataDirectory = args[1];
        var doubtsDocument = args.Length == 3 ? args[2] : DefaultDoubtsDocument;

        if (!File.Exists(workbookPath))
        {
            error.WriteLine($"No se encuentra la hoja de cálculo: {workbookPath}");
            error.WriteLine("Indica su ruta como primer argumento; lo habitual es guardarla en la variable de entorno HACKMASTER_EXCEL.");
            return UsageError;
        }

        WorkbookReadResult read;
        try
        {
            // FileShare.ReadWrite permite importar con la hoja abierta en Excel.
            using var stream = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            read = WorkbookReader.Read(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
        {
            error.WriteLine($"No se puede leer la hoja de cálculo: {exception.Message}");
            return UsageError;
        }

        if (read.Workbook is null)
        {
            error.WriteLine($"La hoja no tiene la estructura esperada: {Count(read.Errors.Count, "error", "errores")}. No se ha modificado ningún archivo.");
            WriteMessages(error, read.Errors);
            return UsageError;
        }

        var result = Importer.Import(read.Workbook);
        if (result.Model is null)
        {
            error.WriteLine($"La importación ha fallado: {Count(result.Errors.Count, "error", "errores")}. No se ha modificado ningún archivo.");
            WriteMessages(error, result.Errors);
            return DataErrors;
        }

        DataWriter.Write(result.Model, dataDirectory, doubtsDocument);

        var model = result.Model;
        output.WriteLine("Importación correcta.");
        output.WriteLine($"  Habilidades: {model.Skills.Count}");
        output.WriteLine($"  Conjuntos de habilidades: {model.Suites.Count(suite => suite.Kind == SuiteKind.Custom)}");
        output.WriteLine($"  Conjuntos oficiales: {model.Suites.Count(suite => suite.Kind == SuiteKind.Official)}");
        output.WriteLine($"  Estilos de combate: {model.Styles.Count}");
        output.WriteLine(
            $"  Dudas: {model.Doubts.Count(doubt => doubt.Origin == DoubtOrigin.Sheet)} de la hoja, " +
            $"{model.Doubts.Count(doubt => doubt.Origin == DoubtOrigin.Import)} detectadas");

        if (result.Warnings.Count == 0)
        {
            output.WriteLine("Sin avisos.");
        }
        else
        {
            output.WriteLine($"Avisos ({result.Warnings.Count}):");
            WriteMessages(output, result.Warnings);
        }

        return Success;
    }

    private static void WriteMessages(TextWriter writer, IEnumerable<ImportMessage> messages)
    {
        foreach (var message in messages)
        {
            writer.WriteLine($"  {message}");
        }
    }

    private static string Count(int count, string singular, string plural) => count == 1 ? $"1 {singular}" : $"{count} {plural}";
}
