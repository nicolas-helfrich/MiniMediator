**MiniMediator -- Lightweight Mediator Pattern for .NET**

**MiniMediator** is a lightweight, dependency-free library that implements the **Mediator pattern** for .NET.\
It is a simple, transparent, and license-free alternative to **MediatR**, focusing on the most common features (Send, Publish, Pipeline Behaviors).

* * * * *

**✨****️**** Key Features**

-   **Drop-in Replacement**:

-   Same interfaces as MediatR (IRequest<T>, IRequestHandler<TReq,TRes>, INotification, INotificationHandler<T>, IPipelineBehavior<TReq,TRes>).
-   No need to change existing requests, handlers, or controllers → just swap out the DI registration.

-   **Request/Response (****Send****)**
-   **Notifications (****Publish****)**
-   **Pipeline Behaviors** (logging, validation, caching, etc.)
-   **MIT Licensed** -- safe for academic, private, and commercial use.

* * * * *

**🛠****️**** Example 1: Request/Response**

**Define a request and a response:**
```csharp
public record Ping(string Message) : IRequest<Pong>;

public record Pong(string Message);
```

**Implement a request handler:**
```csharp
public class PingHandler : IRequestHandler<Ping, Pong>

{

    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)

    {

        return Task.FromResult(new Pong($"Pong received: {request.Message}"));

    }

}
```
**Use it in a controller:**
```csharp
[ApiController]

[Route("api/[controller]")]

public class PingController : ControllerBase

{

    private readonly IMediator _mediator;

    public PingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{msg}")]

    public async Task<IActionResult> Get(string msg, CancellationToken ct)

    {

        var result = await _mediator.Send(new Ping(msg), ct);

 return Ok(result);

    }

}
```
* * * * *

**🛠****️**** Example 2: Notifications (Publish)**

**Define a notification:**
```csharp
public record OrderPaid(Guid OrderId) : INotification;
```

**Handle the notification in multiple places:**
```csharp
public class SendEmailOnOrderPaid : INotificationHandler<OrderPaid>

{

    public Task Handle(OrderPaid notification, CancellationToken ct)

    {

        Console.WriteLine($"📧 Sending email for order {notification.OrderId}");

        return Task.CompletedTask;

    }

}

public class UpdateWarehouseOnOrderPaid : INotificationHandler<OrderPaid>

{

    public Task Handle(OrderPaid notification, CancellationToken ct)

    {

        Console.WriteLine($"🏭 Updating warehouse for order {notification.OrderId}");

        return Task.CompletedTask;

    }

}
```
**Publish the event:**

await _mediator.Publish(new OrderPaid(Guid.NewGuid()));

👉 Both handlers are triggered **in parallel**, and Publish waits until all handlers have finished.

* * * * *

**🛠****️**** Example 3: Pipeline Behavior**

**Define a behavior:**
```csharp

public class LoggingBehavior<TRequest, TResponse>

    : IPipelineBehavior<TRequest, TResponse>

    where TRequest : IRequest<TResponse>

{

    public async Task<TResponse> Handle(

 TRequest request,

        CancellationToken ct,

        RequestHandlerDelegate<TResponse> next)

    {

        Console.WriteLine($"➡️ Handling {typeof(TRequest).Name}");

        var response = await next();

        Console.WriteLine($"⬅️ Handled {typeof(TResponse).Name}");

        return response;

    }

}
```

**Registration happens automatically** -- as soon as the class is in your assembly, AddMiniMediatR() picks it up.

* * * * *

**🛠****️**** Example 4: Startup / Program.cs**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register controllers

builder.Services.AddControllers();

// Register MiniMediator and scan assembly for handlers & behaviors

builder.Services.AddMiniMediatR(typeof(Program).Assembly);

var app = builder.Build();

app.MapControllers();

app.Run();

👉 That's the only change compared to MediatR:

// Old (MediatR):

// builder.Services.AddMediatR(typeof(Program).Assembly);

// New (MiniMediator):

builder.Services.AddMiniMediatR(typeof(Program).Assembly);

Everything else -- your handlers, notifications, behaviors -- remains **unchanged**.
```
* * * * *

**⚠****️**** Limitations**

MiniMediator is intentionally minimal:

-   No streaming requests (IStreamRequestHandler).
-   No built-in pre/post processors (but can be modeled via Behaviors).
-   No configurable publish strategies (defaults to parallel Task.WhenAll).
-   No source generator optimizations.

For most small/medium apps and research projects, this is **more than enough**.

* * * * *

**📜**** License**

MiniMediator is licensed under the **MIT License**.\
It is an **independent project** and is **not affiliated with MediatR or its authors**.
