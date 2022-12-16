using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modaularity.Abstractions;

namespace Modaularity.AspNetCore; 

public class ModuleFrameworkInitializer : IHostedService
{
    private readonly IEnumerable<IModuleCatalog> _moduleCatalogs;
    private readonly ILogger<ModuleFrameworkInitializer> _logger;

    public ModuleFrameworkInitializer(IEnumerable<IModuleCatalog> moduleCatalogs, ILogger<ModuleFrameworkInitializer> logger)
    {
        _moduleCatalogs = moduleCatalogs;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Inicializando {_moduleCatalogs.Count()} catálogos de módulos");

            foreach (var moduleCatalog in _moduleCatalogs)
            {
                try
                {
                    _logger.LogDebug($"Inicializando {moduleCatalog}");

                    await moduleCatalog.Initialize();

                    _logger.LogDebug($"{moduleCatalog} inicializado");
                    _logger.LogTrace($"Se encontraron los siguientes módulos desde {moduleCatalog}");

                    foreach (var module in moduleCatalog.GetModules())
                        _logger.LogTrace(module.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Falló la inicialización de {moduleCatalog}");
                }    
            }

            _logger.LogInformation($"Se han inicializado {_moduleCatalogs.Count()} catálogos de módulos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló la inicialización de catálogos de módulos");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
