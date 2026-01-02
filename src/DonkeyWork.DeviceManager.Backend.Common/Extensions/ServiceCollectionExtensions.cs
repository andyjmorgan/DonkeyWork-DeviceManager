namespace DonkeyWork.DeviceManager.Backend.Common.Extensions;

using DonkeyWork.DeviceManager.Backend.Common.RequestContextServices;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommonWebServices(this IServiceCollection services)
    {
        return services.AddScoped<IRequestContextProvider, RequestContextProvider>();
    }
}