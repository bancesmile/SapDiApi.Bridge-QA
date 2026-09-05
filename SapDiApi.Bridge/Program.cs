using SapDiApi.Bridge.GraphQL;
using SapDiApi.Bridge.Infrastructure.Filters;
using SapDiApi.Bridge.Infrastructure.Logging;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Infrastructure.Swagger;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Health;
using SapDiApi.Bridge.Services.Sap;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Logging Diario con Serilog (Archivos de Texto con Rotación Diaria)
var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Logs", "bridge-audit-.log");
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: logPath,
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 52_428_800, // 50 MB por archivo máximo
        retainedFileCountLimit: 31,      // Conservar los últimos 31 días (evita archivos gigantescos de 5GB)
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true)
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Seguridad y Sesiones estilo SAP Service Layer
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyConstants.SectionName));
builder.Services.AddSingleton<ISessionManager, SessionManager>();
builder.Services.AddScoped<ISapAuthService, SapAuthService>();
builder.Services.AddScoped<B1SessionAuthFilter>();
builder.Services.AddScoped<ApiKeyAuthFilter>();

// 3. Pool de Conexiones SAP DI API y Mitigación de Concurrencia (SemaphoreSlim)
builder.Services.AddSingleton<ISapCompanyPool, SapCompanyPool>();
builder.Services.AddSingleton<ISapDiApiConnector, SapDiApiConnector>();
builder.Services.AddSingleton<IHealthService, HealthService>();
builder.Services.AddSingleton<IBusinessPartnerService, BusinessPartnerService>();

// 4. GraphQL con Soporte Completo para Filtering, Sorting, Proyecciones y Mutations
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

// 5. Controladores REST API con filtro global de excepciones
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

// 6. CORS Abierto
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 7. OpenAPI / Swagger / Scalar
builder.Services.AddModernSwagger();

var app = builder.Build();

// 8. Pipeline HTTP
app.UseCors("AllowAll");

// Middleware de Reescritura Transparente de Rutas Service Layer (/b1s/v1/* -> /api/v1/*)
// Permite que clientes de Service Layer consuman la API sin duplicar endpoints en la documentación
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null)
    {
        if (path.StartsWith("/b1s/v1/", StringComparison.OrdinalIgnoreCase))
        {
            var remaining = path.Substring(8);
            context.Request.Path = "/api/v1/" + remaining;
        }
        else if (path.Equals("/api/Login", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/api/v1/Login";
        }
        else if (path.Equals("/api/Logout", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/api/v1/Logout";
        }
        else if (path.Equals("/api/status", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/api/health";
        }
        else if (path.StartsWith("/api/v1/Attachments(", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/api/v1/Attachments2(" + path.Substring(20);
        }
        else if (path.Equals("/api/v1/Attachments", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Path = "/api/v1/Attachments2";
        }
    }
    await next();
});

// Middleware de Auditoría Diaria (Registra IP, Máquina, Usuario, Operador X-Audit-User, Endpoint, Método y Tiempo)
app.UseRequestAuditLogging();

// Documentación de APIs
app.UseModernApiDocumentation();

// Redirección de la raíz hacia la documentación moderna Scalar (/scalar/v1)
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.UseRouting();

// Endpoint GraphQL (Banana Cake Pop en /graphql)
app.MapGraphQL("/graphql");

app.MapControllers();

app.Run();

// Exportación para pruebas automatizadas
public partial class Program { }
