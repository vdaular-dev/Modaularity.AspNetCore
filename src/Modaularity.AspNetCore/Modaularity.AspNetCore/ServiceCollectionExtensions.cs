using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Modaularity.Abstractions;
using Modaularity.Catalogs.Composites;
using Modaularity.Catalogs.Empty;
using Modaularity.Catalogs.Folders;
using Modaularity.Configuration.Converters;
using Modaularity.Configuration.Providers;
using Modaularity.Context;
using Modaularity.TypeFinding;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Module = Modaularity.Abstractions.Module;

namespace Modaularity.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModaularity(this IServiceCollection services, Action<ModaularityOptions> configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddHostedService<ModaularityInitializer>();
        services.AddTransient<ModuleProvider>();
        services.TryAddTransient(typeof(IModuleCatalogConfigurationLoader), typeof(ModuleCatalogConfigurationLoader));
        services.AddTransient(typeof(IConfigurationToCatalogConverter), typeof(FolderCatalogConfigurationConverter));
        services.AddTransient(typeof(IConfigurationToCatalogConverter), typeof(AssemblyCatalogConfigurationConverter));
        
        services.AddConfiguration();

        services.AddSingleton(sp =>
        {
            var result = new List<Module>();
            var catalogs = sp.GetServices<IModuleCatalog>();

            foreach (var catalog in catalogs)
            {
                var modules = catalog.GetModules();

                result.AddRange(modules);
            }

            return result.AsEnumerable();
        });

        var aspNetCoreControllerAssemblyLocation = typeof(Controller).Assembly.Location;

        if (string.IsNullOrWhiteSpace(aspNetCoreControllerAssemblyLocation))
            return services;

        var aspNetCoreLocation = Path.GetDirectoryName(aspNetCoreControllerAssemblyLocation);

        if (ModuleLoadContextOptions.Defaults.AdditionalRuntimePaths == null)
            ModuleLoadContextOptions.Defaults.AdditionalRuntimePaths = new();

        if (!ModuleLoadContextOptions.Defaults.AdditionalRuntimePaths.Contains(aspNetCoreLocation))
            ModuleLoadContextOptions.Defaults.AdditionalRuntimePaths.Add(aspNetCoreLocation);

        return services;
    }

    public static IServiceCollection AddModaularity<TType>(this IServiceCollection services, string dllPath = "") where TType : class
    {
        services.AddModaularity();

        if (string.IsNullOrWhiteSpace(dllPath))
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly == null)
                dllPath = Environment.CurrentDirectory;
            else
                dllPath = Path.GetDirectoryName(entryAssembly.Location);
        }

        var typeFinderCriteria = TypeFinderCriteriaBuilder.Create()
            .AssignableTo(typeof(TType))
            .Build();

        var catalog = new FolderModuleCatalog(dllPath, typeFinderCriteria);
        services.AddModuleCatalog(catalog);
        services.AddModuleType<TType>();

        return services;
    }

    private static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton<IModuleCatalog>(serviceProvider =>
        {
            var options = serviceProvider.GetService<IOptions<ModaularityOptions>>().Value;

            if (options.UseConfiguration == false)
                return new EmptyModuleCatalog();

            var loaders = serviceProvider.GetServices<IModuleCatalogConfigurationLoader>().ToList();
            var configuration = serviceProvider.GetService<IConfiguration>();
            var converters = serviceProvider.GetServices<IConfigurationToCatalogConverter>().ToList();
            var catalogs = new List<IModuleCatalog>();

            foreach (var loader in loaders)
            {
                var catalogConfigs = loader.GetCatalogConfigurations(configuration);

                if (catalogConfigs?.Any() != true)
                    continue;

                for (int i = 0; i < catalogConfigs.Count; i++)
                {
                    var item = catalogConfigs[i];
                    var key = $"{options.ConfigurationSection}:{loader.CatalogsKey}:{i}";

                    if (string.IsNullOrWhiteSpace(item.Type))
                        throw new ArgumentException($"Un tipo debe ser provisto por el catálogo en la posición {i + 1}");

                    var foundConverter = converters.FirstOrDefault(converter => converter.CanConvert(item.Type));

                    if (foundConverter == null)
                        throw new ArgumentException($"El tipo provisto por el catálogo de módulos en la posición {i + 1} es desconocido");

                    var catalog = foundConverter.Convert(configuration.GetSection(key));

                    catalogs.Add(catalog);
                }
            }

            return new CompositeModuleCatalog(catalogs.ToArray());
        });

        return services;
    }

    public static IServiceCollection AddModuleCatalog(this IServiceCollection services, IModuleCatalog moduleCatalog)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IModuleCatalog), moduleCatalog));

        return services;
    }

    public static IServiceCollection AddModuleType<T>(this IServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Transient, 
        Action<DefaultModuleOption> configureDefault = null) where T : class
    {
        var serviceDescriptorEnumerable = new ServiceDescriptor(typeof(IEnumerable<T>), sp =>
        {
            var moduleProvider = sp.GetService<ModuleProvider>();
            var result = moduleProvider.GetTypes<T>();

            return result.AsEnumerable();
        }, serviceLifetime);

        var serviceDescriptorSingle = new ServiceDescriptor(typeof(T), sp =>
        {
            var defaultModuleOption = GetDefaultModuleOptions<T>(configureDefault, sp);
            var moduleProvider = sp.GetService<ModuleProvider>();
            var result = moduleProvider.GetTypes<T>();
            var defaultType = defaultModuleOption.DefaultType(sp, result.Select(result => result.GetType()));

            return result.FirstOrDefault(r => r.GetType() == defaultType);
        }, serviceLifetime);

        services.Add(serviceDescriptorEnumerable);
        services.Add(serviceDescriptorSingle);

        return services;
    }

    private static DefaultModuleOption GetDefaultModuleOptions<T>(Action<DefaultModuleOption> configureDefault, IServiceProvider sp) where T : class
    {
        var defaultModuleOption = new DefaultModuleOption();

        if (configureDefault == null)
        {
            var optionsFromMonitor = sp.GetService<IOptionsMonitor<DefaultModuleOption>>().Get(typeof(T).Name);

            if (optionsFromMonitor != null)
                defaultModuleOption = optionsFromMonitor;
        }
        else
            configureDefault(defaultModuleOption);

        return defaultModuleOption;
    }
}
