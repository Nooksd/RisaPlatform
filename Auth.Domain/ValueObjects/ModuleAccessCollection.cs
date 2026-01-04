namespace Auth.Domain.ValueObjects;

public sealed record ModuleAccessCollection
{
    private readonly Dictionary<string, int> _accessLevels;

    public IReadOnlyDictionary<string, int> AccessLevels => _accessLevels;

    private ModuleAccessCollection(Dictionary<string, int> accessLevels)
    {
        _accessLevels = accessLevels;
    }

    public static ModuleAccessCollection Create(Dictionary<string, int> accessLevels)
    {
        var validated = new Dictionary<string, int>();

        foreach (var (module, level) in accessLevels)
        {
            if (level < 0 || level > 3)
                throw new ArgumentException($"Invalid access level {level} for module {module}");

            validated[module] = level;
        }

        return new ModuleAccessCollection(validated);
    }

    public static ModuleAccessCollection Empty() => new(new Dictionary<string, int>());

    public static ModuleAccessCollection FullAccess()
    {
        var modules = Enum.GetNames<Enums.SystemModule>();
        var access = modules.ToDictionary(m => m, _ => 3);
        return new ModuleAccessCollection(access);
    }

    public int GetAccessLevel(string module)
    {
        return _accessLevels.GetValueOrDefault(module, 0);
    }

    public bool CanCreate(ModuleAccessCollection targetAccess)
    {
        foreach (var (module, level) in targetAccess.AccessLevels)
        {
            if (GetAccessLevel(module) < level)
                return false;
        }
        return true;
    }
}
