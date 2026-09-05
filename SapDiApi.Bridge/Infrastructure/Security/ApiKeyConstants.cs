namespace SapDiApi.Bridge.Infrastructure.Security
{
    public static class ApiKeyConstants
    {
        public const string SectionName = "ApiKeyAuth";
        public const string DefaultHeaderName = "X-Api-Key";
        public const string AuthorizationScheme = "ApiKey";
        public const string SwaggerSecurityDefinitionName = "ApiKey";
    }
}
