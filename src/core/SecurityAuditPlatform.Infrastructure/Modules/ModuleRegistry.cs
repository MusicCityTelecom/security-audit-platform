using SecurityAuditPlatform.Core.Modules;

namespace SecurityAuditPlatform.Infrastructure.Modules;

public sealed record RegisteredModule(ModuleManifest Manifest, string Directory, IReadOnlyList<ValidationIssue> ValidationIssues);

public interface IModuleRegistry
{
    IReadOnlyList<RegisteredModule> List();
    RegisteredModule? Find(string moduleId);
    void Refresh();
    string RootDirectory { get; }
    void SetRootDirectory(string root);
}

public sealed class FileModuleRegistry : IModuleRegistry
{
    private string _root;
    private readonly ModuleManifestYamlStore _yaml;
    private readonly ModuleManifestValidator _validator;
    private readonly object _gate = new();
    private List<RegisteredModule> _modules = [];

    public FileModuleRegistry(string root, ModuleManifestYamlStore yaml, ModuleManifestValidator validator)
    {
        _root = root;
        _yaml = yaml;
        _validator = validator;
        Refresh();
    }

    public string RootDirectory { get { lock (_gate) return _root; } }

    public void SetRootDirectory(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Module root is required.", nameof(root));
        _root = Path.GetFullPath(root); Directory.CreateDirectory(_root); Refresh();
    }

    public IReadOnlyList<RegisteredModule> List() { lock (_gate) return _modules.ToArray(); }
    public RegisteredModule? Find(string moduleId) { lock (_gate) return _modules.FirstOrDefault(x => x.Manifest.Id.Equals(moduleId, StringComparison.OrdinalIgnoreCase)); }

    public void Refresh()
    {
        var discovered = new List<RegisteredModule>();
        if (Directory.Exists(_root))
        {
            foreach (var manifestPath in Directory.EnumerateFiles(_root, "module.yaml", SearchOption.AllDirectories))
            {
                try
                {
                    var manifest = _yaml.Deserialize(File.ReadAllText(manifestPath));
                    discovered.Add(new RegisteredModule(manifest, Path.GetDirectoryName(manifestPath)!, _validator.Validate(manifest)));
                }
                catch (Exception ex)
                {
                    discovered.Add(new RegisteredModule(
                        new ModuleManifest(1, "invalid.module", Path.GetFileName(Path.GetDirectoryName(manifestPath) ?? "module"), "0.0.0",
                            ModuleCategory.Utility, ModuleRuntime.Windows, Path.GetFileName(manifestPath), [], [],
                            new ModuleDependency([], [], []), NetworkBehavior.Passive, new ModuleLicense("UNKNOWN"), new ModuleSource("invalid")),
                        Path.GetDirectoryName(manifestPath)!, [new ValidationIssue("manifest.parse", ex.Message, true)]));
                }
            }
        }
        lock (_gate) _modules = discovered.OrderBy(x => x.Manifest.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
