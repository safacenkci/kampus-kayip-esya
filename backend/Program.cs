using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using KampusKayipEsya.Api.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter());
    });

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        };
    });

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new DualApiPrefixConvention());
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

    builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc(options =>
        {
            options.Conventions.Add(new ApiVersion1Convention());
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString,
            name: "postgres",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));

    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length == 0)
    {
        allowedOrigins = ["http://localhost:4200"];
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseExceptionHandler();
    app.Use(async (context, next) =>
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        context.Items["CorrelationId"] = correlationId;
        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            var id = httpContext.Items["CorrelationId"] as string;
            if (!string.IsNullOrWhiteSpace(id))
            {
                httpContext.Response.Headers["X-Correlation-Id"] = id;
            }

            return Task.CompletedTask;
        }, context);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next();
        }
    });
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("Frontend");

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = static _ => false,
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = static check => check.Tags.Contains("ready"),
    });
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

file sealed class DualApiPrefixConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        ArgumentNullException.ThrowIfNull(application);

        foreach (var controller in application.Controllers)
        {
            var extra = new List<SelectorModel>();
            foreach (var selector in controller.Selectors)
            {
                var existing = selector.AttributeRouteModel;
                if (existing?.Template is not { } template)
                {
                    continue;
                }

                if (!template.StartsWith("api/", StringComparison.OrdinalIgnoreCase)
                    || template.StartsWith("api/v{", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                extra.Add(new SelectorModel(selector)
                {
                    AttributeRouteModel = new AttributeRouteModel(existing)
                    {
                        Template = "api/v{version:apiVersion}/" + template[4..],
                    },
                });
            }

            foreach (var selector in extra)
            {
                controller.Selectors.Add(selector);
            }
        }
    }
}

file sealed class ApiVersion1Convention : IControllerConvention
{
    public bool Apply(IControllerConventionBuilder controller, ControllerModel controllerModel)
    {
        ArgumentNullException.ThrowIfNull(controller);
        controller.HasApiVersion(1.0);
        return true;
    }
}
