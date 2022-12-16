namespace Modaularity.AspNetCore;

public class DefaultModuleOption
{
    public Func<IServiceProvider, IEnumerable<Type>, Type> DefaultType { get; set; }
        = (serviceProvider, implementingTypes) => implementingTypes.FirstOrDefault();
}
