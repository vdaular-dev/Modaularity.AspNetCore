using Microsoft.Extensions.DependencyInjection;
using Modaularity.Abstractions;

namespace Modaularity.AspNetCore;

public static class ServiceProviderExtensions
{
    public static object Create(this IServiceProvider serviceProvider, Module module)
        => ActivatorUtilities.CreateInstance(serviceProvider, module);

    public static T Create<T>(this IServiceProvider serviceProvider, Module module) where T : class
        => ActivatorUtilities.CreateInstance(serviceProvider, module) as T;
}
