using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MediatR
{
    internal sealed class Mediator(IServiceProvider provider) : IMediator
    {
        private readonly IServiceProvider _provider = provider;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            // We perform a generic jump to a strongly typed private method,
            // so we can call Handler.Handle without reflection-invoke.
            var method = typeof(Mediator).GetMethod(nameof(SendInternal), BindingFlags.Instance | BindingFlags.NonPublic)!;
            var generic = method.MakeGenericMethod(request.GetType(), typeof(TResponse));
            return (Task<TResponse>)generic.Invoke(this, new object?[] { request, cancellationToken })!;
        }

        private Task<TResponse> SendInternal<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
            where TRequest : IRequest<TResponse>
        {
            var handler = _provider.GetService<IRequestHandler<TRequest, TResponse>>();
            if (handler is null)
            {
                throw new InvalidOperationException($"No IRequestHandler< {typeof(TRequest).Name}, {typeof(TResponse).Name} > registered.");
            }

            var behaviors = _provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(request, cancellationToken);

            // Behaviors are chained in reverse registration order (outer -> inner)
            for (int i = behaviors.Length - 1; i >= 0; i--)
            {
                var next = pipeline;
                var behavior = behaviors[i];
                pipeline = () => behavior.Handle(request, cancellationToken, next);
            }

            return pipeline();
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));
            return PublishInternal(notification, cancellationToken);
        }

        private async Task PublishInternal<TNotification>(TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification
        {
            var handlers = _provider.GetServices<INotificationHandler<TNotification>>().ToArray();

            if (handlers.Length == 0) return; // Nobody is listening – that's fine.

            // Default: fire in parallel (like MediatR.DefaultPublishStrategy ParallelNoWait, but we await all)
            var tasks = handlers.Select(h => h.Handle(notification, cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}
