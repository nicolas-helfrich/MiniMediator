using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MediatR
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Mini-Mediator and scans the specified assemblies for handlers & behaviors.
        /// </summary>
        public static IServiceCollection AddMiniMediatR(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies is null || assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }

            services.AddScoped<IMediator, Mediator>();

            foreach (var type in assemblies.SelectMany(a => SafeGetTypes(a)).Where(t => t.IsClass && !t.IsAbstract))
            {
                foreach (var itf in type.GetInterfaces())
                {
                    if (!itf.IsGenericType) continue;
                    var def = itf.GetGenericTypeDefinition();

                    if (def == typeof(IRequestHandler<,>) || def == typeof(INotificationHandler<>) || def == typeof(IPipelineBehavior<,>))
                    {
                        services.AddTransient(itf, type);
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// Convenience overload: Derives the assembly from a marker type.
        /// </summary>
        public static IServiceCollection AddMiniMediatRFrom(this IServiceCollection services, params Type[] markerTypes)
        {
            var assemblies = (markerTypes?.Length > 0 ? markerTypes.Select(t => t.Assembly) : new[] { Assembly.GetCallingAssembly() }).Distinct().ToArray();
            return services.AddMiniMediatR(assemblies);
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
        }
    }
}
