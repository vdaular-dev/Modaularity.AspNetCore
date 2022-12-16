using Microsoft.Extensions.DependencyInjection;
using Modaularity.Abstractions;

namespace Modaularity.AspNetCore;

public static class ModuleExtensions
{
    public static object Create (this Module module, IServiceProvider serviceProvider, params object[] parameters)
        => ActivatorUtilities.CreateInstance(serviceProvider, module, parameters);

    public static T Create<T>(this Module module, IServiceProvider serviceProvider, params object[] parameters) where T : class
        => ActivatorUtilities.CreateInstance(serviceProvider, module, parameters) as T;
}
