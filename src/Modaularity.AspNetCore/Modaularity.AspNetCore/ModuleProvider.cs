using Microsoft.Extensions.DependencyInjection;
using Modaularity.Abstractions;

namespace Modaularity.AspNetCore;

public class ModuleProvider
{
    private readonly IEnumerable<IModuleCatalog> _moduleCatalogs;
    private readonly IServiceProvider _serviceProvider;

    public ModuleProvider(IEnumerable<IModuleCatalog> moduleCatalogs, IServiceProvider serviceProvider)
    {
        _moduleCatalogs = moduleCatalogs;
        _serviceProvider = serviceProvider;
    }

    public List<Module> GetByTag(string tag)
    {
        var result = new List<Module>();

        foreach (var moduleCatalog in _moduleCatalogs)
        {
            var modulesByTag = moduleCatalog.GetByTag(tag);
            result.AddRange(modulesByTag);
        }

        return result;
    }

    public List<Module> GetModules()
    {
        var result = new List<Module>();

        foreach (var moduleCatalog in _moduleCatalogs)
            result.AddRange(moduleCatalog.GetModules());

        return result;
    }

    public Module Get(string name, Version version)
    {
        foreach (var moduleCatalog in _moduleCatalogs)
        {
            var result = moduleCatalog.Get(name, version);

            if (result != null)
                return result;
        }

        return null;
    }

    public List<T> GetTypes<T>() where T : class 
    { 
        var result = new List<T>();
        var catalogs = _serviceProvider.GetServices<IModuleCatalog>();

        foreach (var moduleCatalog in _moduleCatalogs)
        {
            var modules = moduleCatalog.GetModules();

            foreach (var module in modules.Where(x => typeof(T).IsAssignableFrom(x)))
            {
                var op = module.Create<T>(_serviceProvider);

                result.Add(op);
            }
        }

        return result;
    }
}
