using System.Text;
using System.Text.Json;
using HackMaster.Domain;

namespace HackMaster.DataImport;

/// <summary>
/// Escribe los archivos de datos y el listado de dudas. El resultado depende solo del modelo: sin
/// fechas ni horas, con orden fijo, UTF-8 sin BOM y finales de línea LF.
/// </summary>
public static class DataWriter
{
    public const string SkillsFileName = "skills.json";
    public const string SuitesFileName = "suites.json";
    public const string StylesFileName = "styles.json";
    public const string DoubtsFileName = "doubts.json";
    public const string DoubtsDocument = "dudas.md";

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Contenido de cada archivo, por nombre.</summary>
    public static IReadOnlyDictionary<string, string> Render(GameModel model) => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [SkillsFileName] = Json(new SkillsFile(GameDataJson.CurrentSchemaVersion, model.Skills)),
        [SuitesFileName] = Json(new SuitesFile(GameDataJson.CurrentSchemaVersion, model.Suites)),
        [StylesFileName] = Json(new StylesFile(GameDataJson.CurrentSchemaVersion, model.Styles)),
        [DoubtsFileName] = Json(new DoubtsFile(GameDataJson.CurrentSchemaVersion, model.Doubts)),
        [DoubtsDocument] = RenderDoubtsDocument(model.Doubts),
    };

    /// <summary>
    /// Escribe todo en una carpeta temporal y solo después lo copia al destino, de modo que un fallo
    /// al generar o al preparar las carpetas no deja archivos a medias.
    /// </summary>
    public static void Write(GameModel model, string dataDirectory, string doubtsDocumentPath)
    {
        var files = Render(model);
        var temporary = Path.Combine(Path.GetTempPath(), "hackmaster-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);

        try
        {
            foreach (var (name, content) in files)
            {
                File.WriteAllText(Path.Combine(temporary, name), content, Utf8WithoutBom);
            }

            var doubtsDirectory = Path.GetDirectoryName(Path.GetFullPath(doubtsDocumentPath))!;
            Directory.CreateDirectory(doubtsDirectory);
            Directory.CreateDirectory(dataDirectory);

            foreach (var name in files.Keys)
            {
                var destination = name == DoubtsDocument ? doubtsDocumentPath : Path.Combine(dataDirectory, name);
                File.Copy(Path.Combine(temporary, name), destination, overwrite: true);
            }
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
    }

    private static string Json<T>(T file) => JsonSerializer.Serialize(file, GameDataJson.Options) + "\n";

    private static string RenderDoubtsDocument(IReadOnlyList<Doubt> doubts)
    {
        var builder = new StringBuilder();
        builder.Append("# Dudas abiertas\n\n");
        builder.Append("> Archivo generado por `tools/DataImport` a partir de la pestaña \"Dudas\" de la hoja de cálculo y\n");
        builder.Append("> de lo que detecta la importación. No lo edites a mano: cambia la hoja y vuelve a importar.\n");
        builder.Append(">\n");
        builder.Append("> Las dudas de los conjuros del Grimorio están en `dudas-conjuros.md`, que genera la importación\n");
        builder.Append("> de la hoja de conjuros.\n\n");

        builder.Append("## Dudas de la hoja\n\n");
        var fromSheet = doubts.Where(doubt => doubt.Origin == DoubtOrigin.Sheet).ToList();
        if (fromSheet.Count == 0)
        {
            builder.Append("Ninguna.\n\n");
        }

        foreach (var doubt in fromSheet)
        {
            builder.Append("### ").Append(OneLine(doubt.Subject)).Append("\n\n");
            if (doubt.Source is not null)
            {
                builder.Append("- **Fuente**: ").Append(OneLine(doubt.Source)).Append('\n');
            }

            builder.Append("- **Duda**: ").Append(OneLine(doubt.Question)).Append('\n');
            if (doubt.Provisional is not null)
            {
                builder.Append("- **Decisión provisional**: ").Append(OneLine(doubt.Provisional)).Append('\n');
            }

            builder.Append('\n');
        }

        builder.Append("## Dudas detectadas en la importación\n\n");
        var detected = doubts.Where(doubt => doubt.Origin == DoubtOrigin.Import).ToList();
        if (detected.Count == 0)
        {
            builder.Append("Ninguna.\n");
        }

        foreach (var doubt in detected)
        {
            builder.Append("- **").Append(OneLine(doubt.Subject)).Append("**: ").Append(OneLine(doubt.Question)).Append('\n');
        }

        return builder.ToString();
    }

    private static string OneLine(string text) => string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
