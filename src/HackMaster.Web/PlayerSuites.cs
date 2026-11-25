using HackMaster.Domain;
using HackMaster.Web.Account;

namespace HackMaster.Web;

/// <summary>Resultado de guardar un conjunto: el conjunto guardado o el motivo por el que no se guardó.</summary>
public sealed record SuiteSaveResult(PlayerSuite? Saved, string? Problem);

/// <summary>
/// Conjuntos de habilidades creados por el jugador, guardados en el dispositivo (clave
/// <c>hackmaster.suites</c>). Se muestran junto a los 36 de la hoja, sin distinguirse (aclaración
/// de la spec); sus valores se calculan al mostrarlos con los datos vigentes (research §8).
/// </summary>
public sealed class PlayerSuites(GameData data, BrowserStore store, AccountSession? session = null)
{
    private const string StorageKey = "hackmaster.suites";

    /// <summary>Texto del requisito de un conjunto, como en la hoja (FR-012).</summary>
    public const string PrerequisiteText = "Las de las habilidades que lo componen";

    /// <summary>Palabras de las direcciones de conjuntos que no pueden ser el slug de un conjunto.</summary>
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.Ordinal) { "nuevo", "oficiales" };

    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private PlayerSuitesFile _file = PlayerSuitesFile.Empty;
    private bool _loaded;
    private bool _subscribed;

    /// <summary>Motivo por el que no se pudieron leer los conjuntos guardados; mientras exista no se guarda nada, para no perderlos.</summary>
    public string? LoadError { get; private set; }

    /// <summary>Conjuntos del jugador, en el orden en que se guardaron.</summary>
    public IReadOnlyList<PlayerSuite> Suites => _file.Suites;

    /// <summary>Los conjuntos de habilidades de la hoja y los del jugador, ordenados por nombre.</summary>
    public IReadOnlyList<SkillSuite> CustomSuites =>
        [.. data.Suites.Where(suite => suite.Kind == SuiteKind.Custom)
            .Concat(_file.Suites.Select(ToSkillSuite))
            .OrderBy(suite => suite.SearchName, StringComparer.Ordinal)];

    /// <summary>Lee los conjuntos guardados la primera vez; las siguientes no hace nada.</summary>
    public async Task LoadAsync()
    {
        Subscribe();
        await _loadGate.WaitAsync();
        try
        {
            if (_loaded)
            {
                return;
            }

            var read = StoredData.ReadSuites(await store.GetAsync(StorageKey));
            _file = read.Value ?? PlayerSuitesFile.Empty;
            LoadError = read.Error;
            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    /// <summary>
    /// Olvida lo leído para volver a leerlo la próxima vez: al cambiar de cuenta (lo de la anterior no
    /// se ve, F006 FR-009) o cuando llega algo de la cuenta.
    /// </summary>
    public void Invalidate()
    {
        _file = PlayerSuitesFile.Empty;
        LoadError = null;
        _loaded = false;
    }

    private void Subscribe()
    {
        if (_subscribed || session is null)
        {
            return;
        }

        _subscribed = true;
        session.Changed += Invalidate;
    }

    /// <summary>
    /// Vuelve a leer lo guardado justo antes de escribir, para no borrar lo que haya guardado otra
    /// pestaña mientras tanto (SC-005). Si no hay nada guardado (datos del sitio borrados), se
    /// conserva lo que hay en memoria y se vuelve a escribir.
    /// </summary>
    private async Task RefreshAsync()
    {
        Subscribe();
        await _loadGate.WaitAsync();
        try
        {
            var json = await store.GetAsync(StorageKey);
            if (json is null)
            {
                _loaded = true;
                return;
            }

            var read = StoredData.ReadSuites(json);
            _file = read.Value ?? _file;
            LoadError = read.Error;
            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public PlayerSuite? Find(string slug) => _file.Suites.FirstOrDefault(suite => suite.Slug == slug);

    public bool IsPlayerSuite(string slug) => Find(slug) is not null;

    /// <summary>Un conjunto de habilidades (de la hoja o del jugador) por su slug.</summary>
    public SkillSuite? FindCustomSuite(string slug) =>
        Find(slug) is { } mine ? ToSkillSuite(mine) : data.FindSuite(SuiteKind.Custom, slug);

    /// <summary>Nombres de los conjuntos del jugador que usan este como componente.</summary>
    public IReadOnlyList<string> UsedBy(string slug)
    {
        var reference = new SuiteComponentRef(ElementKind.Suite, slug);
        return [.. _file.Suites.Where(suite => suite.Components.Contains(reference)).Select(suite => suite.Name)];
    }

    /// <summary>Componentes de un conjunto del jugador que ya no existen en los datos.</summary>
    public IReadOnlyList<SuiteComponentRef> MissingComponents(PlayerSuite suite) =>
        [.. suite.Components.Where(component => NameOf(component) is null)];

    /// <summary>Nombre de un componente, o nulo si no existe.</summary>
    public string? NameOf(SuiteComponentRef component) => component.Kind switch
    {
        ElementKind.Skill => data.FindSkill(component.Slug)?.Name,
        ElementKind.Suite => Find(component.Slug)?.Name ?? data.FindSuite(SuiteKind.Custom, component.Slug)?.Name,
        ElementKind.OfficialSuite => data.FindSuite(SuiteKind.Official, component.Slug)?.Name,
        _ => null,
    };

    /// <summary>Valores de un componente para el cálculo, o nulo si no existe.</summary>
    public SuiteComponentValues? ValuesOf(SuiteComponentRef component) => ValuesOf(component, []);

    /// <summary>Calcula los valores de un conjunto con estos componentes; los que no existen dejan pendientes los valores.</summary>
    public SuiteCalculation Calculate(IReadOnlyList<SuiteComponentRef> components) => Calculate(components, []);

    /// <summary>Motivo por el que estos componentes no se pueden guardar, o nulo.</summary>
    public string? CheckComposition(IReadOnlyList<SuiteComponentRef> components, string? originalSlug) =>
        CheckComposition(components, originalSlug is null ? null : new SuiteComponentRef(ElementKind.Suite, originalSlug));

    /// <summary>
    /// Motivo por el que no se puede guardar con este nombre y estos componentes, o nulo. Busca
    /// ciclos con el slug nuevo y con el anterior: un conjunto borrado deja su referencia en los que
    /// lo usaban, y reutilizar su nombre podría cerrar un ciclo.
    /// </summary>
    public string? CheckSave(string name, IReadOnlyList<SuiteComponentRef> components, string? originalSlug)
    {
        var problem = CheckName(name, originalSlug) ?? CheckComposition(components, originalSlug);
        var slug = TextNormalizer.ToSlug(name.Trim());
        return problem ?? (slug == originalSlug ? null : CheckComposition(components, new SuiteComponentRef(ElementKind.Suite, slug)));
    }

    private string? CheckComposition(IReadOnlyList<SuiteComponentRef> components, SuiteComponentRef? self) =>
        SuiteCalculator.CheckComposition(
            components,
            self,
            reference => reference.Kind == ElementKind.Suite && Find(reference.Slug) is { } child ? child.Components : []);

    /// <summary>Motivo por el que el nombre no vale, o nulo. El slug no puede coincidir con el de otro conjunto de habilidades.</summary>
    public string? CheckName(string name, string? originalSlug)
    {
        var slug = TextNormalizer.ToSlug(name.Trim());
        if (slug.Length == 0)
        {
            return "Escribe un nombre para el conjunto.";
        }

        if (ReservedSlugs.Contains(slug))
        {
            return $"«{name.Trim()}» no se puede usar como nombre de conjunto.";
        }

        if (slug != originalSlug && (data.FindSuite(SuiteKind.Custom, slug) is not null || Find(slug) is not null))
        {
            return $"Ya hay un conjunto de habilidades que se llama «{name.Trim()}» o muy parecido.";
        }

        return null;
    }

    /// <summary>Crea o modifica un conjunto. Si cambia de nombre, actualiza los conjuntos del jugador que lo usan.</summary>
    public async Task<SuiteSaveResult> SaveAsync(
        string name,
        IReadOnlyList<SuiteComponentRef> components,
        IReadOnlyList<Ability> abilities,
        string? originalSlug,
        DateTimeOffset now)
    {
        if (session is not null)
        {
            await session.DataGate.WaitAsync();
        }

        try
        {
            return await SaveLockedAsync(name, components, abilities, originalSlug, now);
        }
        finally
        {
            session?.DataGate.Release();
        }
    }

    private async Task<SuiteSaveResult> SaveLockedAsync(
        string name,
        IReadOnlyList<SuiteComponentRef> components,
        IReadOnlyList<Ability> abilities,
        string? originalSlug,
        DateTimeOffset now)
    {
        await RefreshAsync();
        if (LoadError is not null)
        {
            return new SuiteSaveResult(null, $"No se guarda para no borrar los conjuntos que no se pudieron leer ({LoadError}).");
        }

        var problem = CheckSave(name, components, originalSlug);
        if (problem is not null)
        {
            return new SuiteSaveResult(null, problem);
        }

        var slug = TextNormalizer.ToSlug(name.Trim());
        var original = originalSlug is null ? null : Find(originalSlug);
        var saved = new PlayerSuite
        {
            Slug = slug,
            Name = name.Trim(),
            Components = components,
            Abilities = abilities,
            CreatedAt = original?.CreatedAt ?? now,
            UpdatedAt = now,
        };

        var suites = _file.Suites.Where(suite => suite.Slug != originalSlug).Append(saved);
        if (originalSlug is not null && originalSlug != slug)
        {
            var oldReference = new SuiteComponentRef(ElementKind.Suite, originalSlug);
            var newReference = new SuiteComponentRef(ElementKind.Suite, slug);
            suites = suites.Select(suite => suite.Components.Contains(oldReference)
                ? suite with { Components = [.. suite.Components.Select(component => component == oldReference ? newReference : component)] }
                : suite);
        }

        _file = _file with { Suites = [.. suites] };
        await store.SetAsync(StorageKey, StoredData.Write(_file));
        return new SuiteSaveResult(saved, null);
    }

    public async Task DeleteAsync(string slug)
    {
        if (session is not null)
        {
            await session.DataGate.WaitAsync();
        }

        try
        {
            await RefreshAsync();
            if (LoadError is not null)
            {
                return;
            }

            _file = _file with { Suites = [.. _file.Suites.Where(suite => suite.Slug != slug)] };
            await store.SetAsync(StorageKey, StoredData.Write(_file));
        }
        finally
        {
            session?.DataGate.Release();
        }
    }

    /// <summary>Un conjunto del jugador con la forma de un conjunto de la hoja y sus valores calculados.</summary>
    public SkillSuite ToSkillSuite(PlayerSuite suite) => ToSkillSuite(suite, []);

    private SkillSuite ToSkillSuite(PlayerSuite suite, HashSet<string> visiting)
    {
        visiting.Add(suite.Slug);
        var calculation = Calculate(suite.Components, visiting);
        visiting.Remove(suite.Slug);

        var pending = new List<string>();
        var raw = new SortedDictionary<string, string>(StringComparer.Ordinal);
        void Pending(string field, string? reason)
        {
            if (reason is not null)
            {
                pending.Add(field);
                raw[field] = reason;
            }
        }

        if (MissingComponents(suite).Count > 0)
        {
            pending.Add("components");
        }

        Pending("cost", calculation.CostPending);
        Pending("price", calculation.PricePending);
        Pending("difficulty", calculation.DifficultyPending);
        Pending("masteryDie", calculation.MasteryDiePending);
        Pending("classes", calculation.ClassesPending);

        return new SkillSuite
        {
            Slug = suite.Slug,
            Name = suite.Name,
            SearchName = TextNormalizer.Normalize(suite.Name),
            Kind = SuiteKind.Custom,
            Cost = calculation.Cost,
            Abilities = suite.Abilities,
            MasteryDie = calculation.MasteryDie,
            Price = calculation.Price,
            Difficulty = calculation.Difficulty,
            Classes = calculation.Classes,
            Components =
            [
                .. suite.Components
                    .Select(component => (component, name: NameOf(component)))
                    .Where(item => item.name is not null)
                    .Select(item => new SuiteComponent(item.name!, item.component.Kind, item.component.Slug)),
            ],
            Prerequisite = new Prerequisite { Segments = [new PrerequisiteSegment(PrerequisiteText, PrerequisiteSegmentKind.Text)] },
            Pending = pending,
            Raw = raw,
        };
    }

    private SuiteCalculation Calculate(IReadOnlyList<SuiteComponentRef> components, HashSet<string> visiting)
    {
        var values = components.Select(component => ValuesOf(component, visiting)).ToList();
        var calculation = SuiteCalculator.Calculate([.. values.OfType<SuiteComponentValues>()]);
        if (values.All(value => value is not null))
        {
            return calculation;
        }

        const string Missing = "Algún componente ya no existe en los datos.";
        return calculation with
        {
            Cost = null,
            CostPending = Missing,
            Price = null,
            PricePending = Missing,
            Difficulty = null,
            DifficultyPending = Missing,
            MasteryDie = null,
            MasteryDiePending = Missing,
            Classes = [],
            ClassesPending = Missing,
        };
    }

    private SuiteComponentValues? ValuesOf(SuiteComponentRef component, HashSet<string> visiting)
    {
        switch (component.Kind)
        {
            case ElementKind.Skill:
                return data.FindSkill(component.Slug) is { } skill ? SuiteComponentValues.From(skill) : null;
            case ElementKind.OfficialSuite:
                return data.FindSuite(SuiteKind.Official, component.Slug) is { } official ? SuiteComponentValues.From(official) : null;
            case ElementKind.Suite when Find(component.Slug) is { } mine:
                // Un ciclo no se puede guardar (CheckComposition), pero se protege por si los datos guardados lo tuvieran.
                return visiting.Contains(mine.Slug) ? null : SuiteComponentValues.From(ToSkillSuite(mine, visiting));
            case ElementKind.Suite:
                return data.FindSuite(SuiteKind.Custom, component.Slug) is { } sheet ? SuiteComponentValues.From(sheet) : null;
            default:
                return null;
        }
    }
}
