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
### Servicio de Integración para SAP Business One (DI API & Service Layer Mode)

Esta API proporciona dos formas flexibles y eficientes para interactuar con SAP Business One:

1. **REST API (Service Layer Mode):** Endpoints estándar compatibles con SAP Service Layer (`/api/v1/Login`, `/api/v1/BusinessPartners`, `/api/v1/Attachments2`, `/api/health`).
2. **GraphQL IDE & API:** Disponible en [`/graphql`](/graphql) (Banana Cake Pop) con soporte completo de proyecciones, filtros dinámicos, ordenamiento y mutaciones (`Query` y `Mutation`).

#### 🔐 Autenticación
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
                    options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        public static IApplicationBuilder UseModernApiDocumentation(this WebApplication app)
        {
            // 1. Generador OpenAPI JSON
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
            });

            // 2. SCALAR API REFERENCE (Modo Moderno interactivo)
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("BridgeSap API Reference (Service Layer)")
                       .WithTheme(ScalarTheme.Default)
                       .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                       .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
            });

            // 3. SWAGGER UI en Tema Blanco Minimalista
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "BridgeSap REST API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "BridgeSap - API Docs (Tema Blanco)";
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DefaultModelsExpandDepth(1);
                c.DisplayRequestDuration();
                c.EnablePersistAuthorization();

                // Tema Blanco Moderno, Minimalista y Compacto
                c.HeadContent = @"
                    <style>
                        :root {
                            --bg-main: #ffffff;
                            --surface: #f8fafc;
                            --border: #e2e8f0;
                            --primary: #0284c7;
                            --primary-hover: #0369a1;
                            --text: #0f172a;
                            --text-muted: #64748b;
                        }
                        body {
                            background-color: var(--bg-main) !important;
                            color: var(--text) !important;
                            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif !important;
                        }
                        .swagger-ui .topbar {
                            background-color: #ffffff !important;
                            border-bottom: 1px solid var(--border) !important;
                            padding: 10px 0 !important;
                        }
                        .swagger-ui .topbar a span {
                            color: var(--primary) !important;
                            font-weight: 700 !important;
                        }
                        .swagger-ui .info {
                            margin: 24px 0 20px 0 !important;
                        }
                        .swagger-ui .info .title {
                            color: var(--text) !important;
                            font-size: 26px !important;
                            font-weight: 800 !important;
                            letter-spacing: -0.5px;
                        }
                        .swagger-ui .info p, .swagger-ui .info li {
                            color: var(--text-muted) !important;
                            font-size: 13.5px !important;
                        }
                        .swagger-ui .scheme-container {
                            background: var(--surface) !important;
                            box-shadow: none !important;
                            border: 1px solid var(--border) !important;
                            border-radius: 8px;
                            padding: 12px 18px !important;
                            margin-bottom: 20px !important;
                        }
                        .swagger-ui .opblock-tag {
                            color: var(--text) !important;
                            border-bottom: 1px solid var(--border) !important;
                            font-size: 15px !important;
                            font-weight: 700 !important;
                            margin: 18px 0 10px 0 !important;
                            padding: 8px 0 !important;
                        }
                        .swagger-ui .opblock {
                            border-radius: 8px !important;
                            margin: 0 0 10px 0 !important;
                            box-shadow: 0 1px 2px rgba(0,0,0,0.04) !important;
                            border: 1px solid var(--border) !important;
                            background: #ffffff !important;
                        }
                        .swagger-ui .opblock .opblock-summary {
                            padding: 8px 14px !important;
                        }
                        .swagger-ui .opblock .opblock-summary-method {
                            border-radius: 6px !important;
                            font-weight: 700 !important;
                            font-size: 12px !important;
                            min-width: 60px !important;
                            text-align: center !important;
                        }
                        .swagger-ui .opblock .opblock-summary-path {
                            font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace !important;
                            font-size: 13px !important;
                            font-weight: 600 !important;
                            color: var(--text) !important;
                        }
                        .swagger-ui .opblock .opblock-summary-description {
                            font-size: 12.5px !important;
                            color: var(--text-muted) !important;
                        }
                        .swagger-ui .opblock-body {
                            background: #fbfcfe !important;
                            border-top: 1px solid var(--border) !important;
                        }
                        .swagger-ui .btn.authorize {
                            color: #059669 !important;
                            border-color: #059669 !important;
                            background: #ecfdf5 !important;
                            border-radius: 6px !important;
                            font-size: 12px !important;
                            font-weight: 600 !important;
                            padding: 6px 14px !important;
                        }
                        .swagger-ui .btn.authorize svg {
                            fill: #059669 !important;
                        }
                        .swagger-ui .btn.execute {
                            background-color: var(--primary) !important;
                            border-color: var(--primary) !important;
                            border-radius: 6px !important;
                            color: #ffffff !important;
                            font-weight: 600 !important;
                        }
                        .swagger-ui .model-box {
                            background: var(--surface) !important;
                            border: 1px solid var(--border) !important;
                            border-radius: 6px !important;
                        }
                        .swagger-ui table thead tr td, .swagger-ui table thead tr th {
                            color: var(--text-muted) !important;
                            font-size: 12px !important;
                            border-bottom: 1px solid var(--border) !important;
                        }
                        .swagger-ui select, .swagger-ui input[type=text] {
                            background: #ffffff !important;
                            color: var(--text) !important;
                            border: 1px solid #cbd5e1 !important;
                            border-radius: 6px !important;
                            padding: 6px 10px !important;
                        }
                        .swagger-ui .response-col_status {
                            font-weight: 700 !important;
                            color: var(--text) !important;
                        }
                        .swagger-ui .dialog-ux .modal-ux {
                            background: #ffffff !important;
                            border: 1px solid var(--border) !important;
                            border-radius: 12px !important;
                            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1) !important;
                        }
                        .swagger-ui .dialog-ux .modal-ux-header {
                            border-bottom: 1px solid var(--border) !important;
                        }
                    </style>";
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
