using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SapDiApi.Bridge.Models.Companies;
using SapDiApi.Bridge.Services.Companies;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Companies
{
    public class CompanyResolverServiceTests : IDisposable
    {
        private readonly List<string> _tempDatabases = new();

        private CompanyResolverService CreateService(
            string? defaultCompanyDb = null)
        {
            var dbPath = Path.Combine(
                Path.GetTempPath(),
                $"bridge_qa_{Guid.NewGuid():N}.db");

            _tempDatabases.Add(dbPath);

            var settings =
                new Dictionary<string, string?>
                {
                    ["Database:MetadataDbPath"] = dbPath,
                    ["SapSettings:DefaultCompanyDB"] =
                        defaultCompanyDb
                };

            var configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(settings)
                    .Build();

            var serviceProvider =
                new ServiceCollection()
                    .BuildServiceProvider();

            return new CompanyResolverService(
                configuration,
                serviceProvider,
                NullLogger<CompanyResolverService>.Instance);
        }

        private static CompanyDto CreateCompany(
            string code = "QA_TEST",
            string database = "SBO_QA_TEST",
            bool active = true)
        {
            return new CompanyDto
            {
                CompanyCode = code,
                SapDatabase = database,
                CompanyName = "Sociedad QA Test",
                Localization = "GT",
                IsActive = active
            };
        }

        // =====================================================
        // UT-011
        // Registrar sociedad
        // =====================================================
        [Fact]
        public async Task UpsertCompany_WithValidCompany_ReturnsTrue()
        {
            var service = CreateService();

            var result =
                await service.UpsertCompanyAsync(
                    CreateCompany());

            Assert.True(result);
        }

        // =====================================================
        // UT-012
        // Resolver por CompanyId
        // =====================================================
        [Fact]
        public async Task ResolveCompany_ById_ReturnsCompany()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany());

            var saved =
                service.GetByCode("QA_TEST");

            Assert.NotNull(saved);

            var result =
                service.ResolveCompany(
                    saved.CompanyId.ToString());

            Assert.True(result.Found);
            Assert.NotNull(result.Company);
            Assert.Equal(
                "SBO_QA_TEST",
                result.SapDatabase);
        }

        // =====================================================
        // UT-013
        // Resolver por CompanyCode
        // =====================================================
        [Fact]
        public async Task ResolveCompany_ByCode_ReturnsCompany()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany());

            var result =
                service.ResolveCompany("QA_TEST");

            Assert.True(result.Found);
            Assert.NotNull(result.Company);
            Assert.Equal(
                "SBO_QA_TEST",
                result.SapDatabase);
        }

        // =====================================================
        // UT-014
        // Resolver por SapDatabase
        // =====================================================
        [Fact]
        public async Task ResolveCompany_ByDatabase_ReturnsCompany()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany());

            var result =
                service.ResolveCompany("SBO_QA_TEST");

            Assert.True(result.Found);
            Assert.NotNull(result.Company);
            Assert.Equal(
                "QA_TEST",
                result.Company.CompanyCode);
        }

        // =====================================================
        // UT-015
        // Sociedad inexistente
        // =====================================================
        [Fact]
        public void ResolveCompany_UnknownIdentifier_ReturnsNotFound()
        {
            var service = CreateService();

            var result =
                service.ResolveCompany(
                    "SOCIEDAD_NO_EXISTENTE");

            Assert.False(result.Found);
            Assert.Null(result.Company);

            Assert.Equal(
                "SOCIEDAD_NO_EXISTENTE",
                result.SapDatabase);
        }

        // =====================================================
        // UT-016
        // Sociedad por defecto
        // =====================================================
        [Fact]
        public void ResolveCompany_EmptyIdentifier_UsesDefaultCompany()
        {
            var service =
                CreateService("SBO_DEFAULT_QA");

            var result =
                service.ResolveCompany(null);

            Assert.True(result.Found);

            Assert.Equal(
                "SBO_DEFAULT_QA",
                result.SapDatabase);
        }

        // =====================================================
        // UT-017
        // Sociedad invalida
        // =====================================================
        [Fact]
        public async Task UpsertCompany_WithoutCode_ReturnsFalse()
        {
            var service = CreateService();

            var company = CreateCompany();
            company.CompanyCode = "";

            var result =
                await service.UpsertCompanyAsync(company);

            Assert.False(result);
        }

        // =====================================================
        // UT-018
        // Solo sociedades activas
        // =====================================================
        [Fact]
        public async Task GetAll_OnlyActive_ReturnsOnlyActiveCompanies()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany(
                    "ACTIVA",
                    "SBO_ACTIVA",
                    true));

            await service.UpsertCompanyAsync(
                CreateCompany(
                    "INACTIVA",
                    "SBO_INACTIVA",
                    false));

            var companies =
                service.GetAll(
                    onlyActive: true)
                    .ToList();

            Assert.Single(companies);
            Assert.Equal(
                "ACTIVA",
                companies[0].CompanyCode);
        }

        // =====================================================
        // UT-019
        // Listado publico no expone SapDatabase
        // =====================================================
        [Fact]
        public async Task GetPublicCompanies_DoesNotExposeSapDatabase()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany());

            var companies =
                service.GetPublicCompanies()
                    .ToList();

            Assert.Single(companies);

            var json =
                JsonSerializer.Serialize(companies);

            Assert.DoesNotContain(
                "SapDatabase",
                json,
                StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================
        // UT-020
        // CompanyCode case-insensitive
        // =====================================================
        [Fact]
        public async Task GetByCode_IsCaseInsensitive()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany("QA_TEST"));

            var result =
                service.GetByCode("qa_test");

            Assert.NotNull(result);

            Assert.Equal(
                "QA_TEST",
                result.CompanyCode);
        }

        // =====================================================
        // UT-021
        // Actualizar sociedad existente
        // =====================================================
        [Fact]
        public async Task UpsertCompany_ExistingDatabase_UpdatesCompany()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany(
                    "QA_TEST",
                    "SBO_QA_TEST"));

            var updated =
                CreateCompany(
                    "QA_NUEVO",
                    "SBO_QA_TEST");

            updated.CompanyName =
                "Sociedad Actualizada";

            var result =
                await service.UpsertCompanyAsync(
                    updated);

            Assert.True(result);

            var companies =
                service.GetAll(false)
                    .ToList();

            Assert.Single(companies);

            Assert.Equal(
                "QA_NUEVO",
                companies[0].CompanyCode);

            Assert.Equal(
                "Sociedad Actualizada",
                companies[0].CompanyName);
        }

        // =====================================================
        // UT-022
        // Recargar cache
        // =====================================================
        [Fact]
        public async Task ReloadCache_PreservesStoredCompany()
        {
            var service = CreateService();

            await service.UpsertCompanyAsync(
                CreateCompany());

            await service.ReloadCacheAsync();

            var result =
                service.GetByCode("QA_TEST");

            Assert.NotNull(result);

            Assert.Equal(
                "SBO_QA_TEST",
                result.SapDatabase);
        }

        public void Dispose()
        {
            foreach (var dbPath in _tempDatabases)
            {
                if (File.Exists(dbPath))
                {
                    try
                    {
                        File.Delete(dbPath);
                    }
                    catch
                    {
                        // Archivo temporal de QA.
                        // No afectar el resultado del test.
                    }
                }
            }
        }
    }
}