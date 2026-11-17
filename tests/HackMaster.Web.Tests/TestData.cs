using HackMaster.Domain;

namespace HackMaster.Web.Tests;

/// <summary>
/// Datos de juego pequeños para las pruebas de interfaz. Los nombres imitan la hoja, pero los
/// valores y los resúmenes son inventados y solo sirven para probar.
/// </summary>
internal static class TestData
{
    public static Skill Skill(
        string name,
        string type = "Académicas",
        int? cost = null,
        int? price = null,
        int? difficulty = null,
        string[]? books = null,
        string[]? classes = null,
        string? summary = null,
        Prerequisite? prerequisite = null,
        string? masteryDie = "d8",
        Ability[]? abilities = null,
        IReadOnlyList<string>? pending = null,
        IReadOnlyDictionary<string, string>? raw = null) => new()
        {
            Slug = TextNormalizer.ToSlug(name),
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Type = type,
            Cost = cost,
            Price = price,
            Difficulty = difficulty,
            MasteryDie = masteryDie,
            Abilities = abilities ?? [Ability.INT],
            Books = (books ?? ["MJ 100"]).Select(ParseBook).ToList(),
            Classes = classes ?? ["Todas"],
            Summary = summary,
            SearchSummary = summary is null ? null : TextNormalizer.Normalize(summary),
            Prerequisite = prerequisite ?? Prerequisite.None,
            Pending = pending ?? (summary is null ? ["summary"] : []),
            Raw = raw ?? new SortedDictionary<string, string>(),
        };

    private static BookRef ParseBook(string text)
    {
        var parts = text.Split(' ', 2);
        return new BookRef(parts[0], parts.Length > 1 ? parts[1] : null);
    }

    public static readonly Skill Botanica = Skill("Botánica", cost: 3, price: 250, difficulty: 10, books: ["MJ 120"], summary: "Prueba: conocer plantas.");

    public static readonly Skill Herbalismo = Skill(
        "Herbalismo",
        type: "Técnicas",
        cost: 5,
        price: 1403,
        difficulty: -5,
        books: ["CG 40"],
        classes: ["Grupo de clérigos"],
        summary: "Prueba: remedios con botánica y hierbas.",
        prerequisite: new Prerequisite
        {
            Segments =
            [
                new PrerequisiteSegment("Botánica", PrerequisiteSegmentKind.Skill, "botanica"),
                new PrerequisiteSegment(" o ", PrerequisiteSegmentKind.Text),
                new PrerequisiteSegment("Herbalista", PrerequisiteSegmentKind.Suite, "herbalista"),
                new PrerequisiteSegment(", ", PrerequisiteSegmentKind.Text),
                new PrerequisiteSegment("Sabiduría 12+", PrerequisiteSegmentKind.Condition),
            ],
        });

    public static readonly Skill Cerrajeria = Skill("Cerrajería", type: "Técnicas", cost: 4, difficulty: 25, books: ["MJ 130", "GMG 71"], classes: ["Grupo de ladrones"], summary: "Prueba: abrir cerraduras.");

    public static readonly Skill Karate = Skill("Kárate", type: "Artes marciales", price: 500, books: ["CG 50"], classes: ["Monje"], summary: "Prueba: lucha sin armas.");

    public static readonly Skill Zoologia = Skill("Zoología", cost: 2, price: 100, difficulty: 0, books: ["MJ 140"], summary: "Prueba: conocer animales.");

    public static readonly Skill Liderazgo = Skill(
        "Liderazgo básico",
        type: "Sociales",
        cost: 2,
        price: 90,
        difficulty: 5,
        books: ["MJ 150"],
        masteryDie: null,
        summary: "Prueba: dirigir a otros.",
        pending: ["masteryDie"],
        raw: new SortedDictionary<string, string> { ["masteryDie"] = "5" });

    public static readonly Skill Rastrear = Skill(
        "Rastrear",
        type: "Exploración",
        cost: 3,
        price: 300,
        difficulty: 15,
        books: ["MJ 160"],
        prerequisite: new Prerequisite { Segments = [new PrerequisiteSegment("?", PrerequisiteSegmentKind.Unverified)] });

    public static readonly Skill Lucha = Skill("Lucha", type: "Artes marciales", cost: 1, price: 50, difficulty: 0, books: ["CG 60"], classes: ["Todas"], summary: "Prueba: agarrar y derribar.");

    public static IReadOnlyList<Skill> Skills { get; } = [Botanica, Cerrajeria, Herbalismo, Karate, Liderazgo, Lucha, Rastrear, Zoologia];

    public static readonly SkillSuite Herbalista = new()
    {
        Slug = "herbalista",
        Name = "Herbalista",
        SearchName = "herbalista",
        Kind = SuiteKind.Custom,
        Cost = 6,
        Price = 400,
        Difficulty = 10,
        Books = [new BookRef("HJ", "4.05")],
        Classes = ["Todas"],
        Components =
        [
            new SuiteComponent("Botánica", ElementKind.Skill, "botanica"),
            new SuiteComponent("Zoología", ElementKind.Skill, "zoologia"),
        ],
    };

    public static readonly SkillSuite Explorador = new()
    {
        Slug = "explorador",
        Name = "Explorador",
        SearchName = "explorador",
        Kind = SuiteKind.Custom,
        Cost = 8,
        Price = 700,
        Difficulty = 20,
        Books = [new BookRef("HJ", "4.05")],
        Classes = ["Todas"],
        Components =
        [
            new SuiteComponent("Rastrear", ElementKind.Skill, "rastrear"),
            new SuiteComponent("Herbalista", ElementKind.Suite, "herbalista"),
        ],
    };

    public static readonly SkillSuite Cartografo = new()
    {
        Slug = "cartografo",
        Name = "Cartógrafo",
        SearchName = "cartografo",
        Kind = SuiteKind.Official,
        Price = null,
        Books = [new BookRef("MJ", "200")],
        Pending = ["components", "price"],
        Raw = new SortedDictionary<string, string> { ["price"] = "?" },
    };

    public static IReadOnlyList<SkillSuite> Suites { get; } = [Cartografo, Explorador, Herbalista];

    public static readonly CombatStyle Yunque = new()
    {
        Slug = "estilo-del-yunque",
        Name = "Estilo del Yunque",
        SearchName = "estilo del yunque",
        Abilities = [Ability.FUE, Ability.CON],
        MasteryDie = "d10",
        Price = 800,
        Difficulty = 30,
        Books = [new BookRef("CG", "70")],
        Prerequisite = new Prerequisite { Segments = [new PrerequisiteSegment("Lucha", PrerequisiteSegmentKind.Skill, "lucha")] },
    };

    public static IReadOnlyList<CombatStyle> Styles { get; } = [Yunque];

    public static IReadOnlyList<Doubt> Doubts { get; } =
    [
        new Doubt
        {
            Id = "sheet-skill-cerrajeria-1",
            Origin = DoubtOrigin.Sheet,
            Subject = "Cerrajería",
            Target = new DoubtTarget(ElementKind.Skill, "cerrajeria"),
            Source = "MJ 130",
            Question = "Prueba: ¿la dificultad es 25 o 30?",
            Provisional = "Prueba: se dejó 25.",
        },
        new Doubt
        {
            Id = "sheet-general-2",
            Origin = DoubtOrigin.Sheet,
            Subject = "General",
            Source = "CG",
            Question = "Prueba: falta una tabla.",
        },
        new Doubt
        {
            Id = "import-skill-liderazgo-basico-masteryDie",
            Origin = DoubtOrigin.Import,
            Subject = "Liderazgo básico",
            Target = new DoubtTarget(ElementKind.Skill, "liderazgo-basico"),
            Question = "Dado de maestría «5» no es un dado existente.",
        },
    ];

    public static GameData Create() => new(Skills, Suites, Styles, Doubts);
}
