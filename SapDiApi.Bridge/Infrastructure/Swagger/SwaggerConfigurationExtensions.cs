using Microsoft.OpenApi.Models;
using SapDiApi.Bridge.Infrastructure.Security;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SapDiApi.Bridge.Infrastructure.Swagger
{
    public static class SwaggerConfigurationExtensions
    {
        public static IServiceCollection AddModernSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "BridgeSap API Gateway (REST & GraphQL)",
                    Version = "v1",
                    Description = @"
### Servicio de Integración para SAP

Esta API proporciona dos formas flexibles y eficientes para interactuar con SAP Business One:

1. **REST API (Service Layer Mode):** Endpoints estándar compatibles con SAP Service Layer (`/api/v1/Login`, `/api/v1/BusinessPartners`, `/api/v1/Attachments2`, `/api/health`).
2. **GraphQL IDE & API:** Disponible en [`/graphql`](/graphql) (Banana Cake Pop) con soporte completo de proyecciones, filtros dinámicos, ordenamiento y mutaciones (`Query` y `Mutation`).

#### Autenticación
* **B1SESSION:** Inicie sesión mediante `POST /api/v1/Login` (o mutación `login` en GraphQL) y envíe el token retornado en la cabecera `B1SESSION` en cada petición.
* **X-Api-Key:** Cabecera alternativa para integración de sistemas maestros.
",
                    Contact = new OpenApiContact
                    {
                        Name = "BridgeSap Integraciones",
                        Email = "soporte@bridgesap.local"
                    }
                });

                // 1. Definición de Seguridad B1SESSION (Service Layer estándar)
                options.AddSecurityDefinition("B1SESSION", new OpenApiSecurityScheme
                {
                    Name = "B1SESSION",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Ingresa tu SessionId obtenido en 'POST /api/v1/Login'. Ej: `c3e9810a3d23455bb5d1123456789abc`"
                });

                // 2. Definición de Seguridad para X-Api-Key (Llave maestra)
                options.AddSecurityDefinition("X-Api-Key", new OpenApiSecurityScheme
                {
                    Name = ApiKeyConstants.DefaultHeaderName,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = "Llave maestra opcional del sistema. Ej: `BridgeSap-SecretKey-2026-Test`"
                });

                options.OperationFilter<B1SessionSecurityRequirementsOperationFilter>();

                var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
            });

            return services;
        }

        public static IApplicationBuilder UseModernApiDocumentation(this WebApplication app)
        {
            // 1. Generador OpenAPI JSON (necesario como fuente de datos para Scalar)
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            // 2. SCALAR API REFERENCE (Documentación interactiva disponible en /doc)
            app.MapScalarApiReference("doc", options =>
            {
                options.WithTitle("BridgeSap API Gateway (Docs)")
                       .WithTheme(ScalarTheme.DeepSpace)
                       .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                       .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
            });

            return app;
        }
    }

    public class B1SessionSecurityRequirementsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var isProtected = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<B1SessionAuthAttribute>().Any() == true
                           || context.MethodInfo.GetCustomAttributes(true).OfType<B1SessionAuthAttribute>().Any()
                           || context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<ApiKeyAuthAttribute>().Any() == true
                           || context.MethodInfo.GetCustomAttributes(true).OfType<ApiKeyAuthAttribute>().Any();

            if (isProtected)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();

                var b1Scheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "B1SESSION"
                    }
                };

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [b1Scheme] = new List<string>()
                });

                operation.Responses.TryAdd("401", new OpenApiResponse
                {
                    Description = "No Autorizado - Se requiere sesión activa 'B1SESSION' obtenida vía Login"
                });
            }
        }
    }
}
