using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Health;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Sap;
using Xunit;

namespace SapDiApi.Bridge.Tests.Controllers
{
    public class BridgeApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public BridgeApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var testSapConnector = new TestSapDiApiConnector();
                    services.AddSingleton<ISapDiApiConnector>(testSapConnector);
                    services.AddSingleton<IBusinessPartnerService>(new TestBusinessPartnerService(testSapConnector));
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetHealth_Returns200_WithHealthyStatus()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<HealthStatusDto>>();
            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.NotNull(content.Data);
            Assert.Equal("Healthy", content.Data.Status);
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200AndSessionId()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123",
                AuditUser = "juan.perez@empresa.com",
                AuditApp = "PortalProveedores"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(content);
            Assert.False(string.IsNullOrWhiteSpace(content.SessionId));
            Assert.Equal(30, content.SessionTimeout);
        }

        [Fact]
        public async Task CreateBusinessPartner_WithValidPayload_Returns201Created()
        {
            // 1. Iniciar Sesión
            var loginDto = new LoginRequestDto { CompanyDB = "SBODEMO_TEST", UserName = "manager", Password = "manager123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResult);

            // 2. Crear Socio de Negocio
            var newBp = new BusinessPartnerDto
            {
                CardCode = "P000578",
                CardName = "GENTE CON TALENTO, S.A.",
                CardType = "cSupplier",
                GroupCode = 111,
                FederalTaxID = "000000000000"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/BusinessPartners")
            {
                Content = new StringContent(JsonSerializer.Serialize(newBp), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("B1SESSION", loginResult.SessionId);

            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task UpdateBusinessPartner_Returns200OK()
        {
            // 1. Iniciar Sesión
            var loginDto = new LoginRequestDto { CompanyDB = "SBODEMO_TEST", UserName = "manager", Password = "manager123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResult);

            // 2. Actualizar Socio de Negocio
            var updateBp = new BusinessPartnerDto
            {
                CardName = "GENTE CON TALENTO ACTUALIZADO, S.A.",
                Phone1 = "2222-3333"
            };

            var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/BusinessPartners('P000578')")
            {
                Content = new StringContent(JsonSerializer.Serialize(updateBp), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("B1SESSION", loginResult.SessionId);

            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateAttachment_Returns201Created()
        {
            // 1. Iniciar Sesión
            var loginDto = new LoginRequestDto { CompanyDB = "SBODEMO_TEST", UserName = "manager", Password = "manager123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResult);

            // 2. Crear Anexo
            var attachmentDto = new AttachmentDto
            {
                Lines = new List<AttachmentLineDto>
                {
                    new() { SourcePath = @"C:\Temp", FileName = "Contrato.pdf", U_TipoDoc = "CONTRATO" }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Attachments")
            {
                Content = new StringContent(JsonSerializer.Serialize(attachmentDto), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("B1SESSION", loginResult.SessionId);

            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetBusinessPartners_WithoutSession_Returns401Unauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/BusinessPartners");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetBusinessPartnerByCardCode_ReturnsExpectedCustomer()
        {
            // 1. Iniciar Sesión
            var loginDto = new LoginRequestDto { CompanyDB = "SBODEMO_TEST", UserName = "manager", Password = "manager123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResult);

            // 2. Consultar socio específico por CardCode
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/BusinessPartners('C00001')");
            request.Headers.Add("B1SESSION", loginResult.SessionId);

            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<BusinessPartnerDto>>();
            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.Equal("C00001", content.Data?.CardCode);
            Assert.NotEmpty(content.Data?.BPAddresses!);
        }

        [Fact]
        public async Task Concurrency_MultipleSimultaneousRequests_AreHandledSuccessfully()
        {
            // 1. Iniciar Sesión
            var loginDto = new LoginRequestDto { CompanyDB = "SBODEMO_TEST", UserName = "manager", Password = "manager123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResult);

            // 2. Disparar 15 peticiones concurrentes simultáneas
            var tasks = Enumerable.Range(1, 15).Select(async i =>
            {
                var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/BusinessPartners('C00001')");
                req.Headers.Add("B1SESSION", loginResult.SessionId);
                req.Headers.Add("X-Audit-User", $"operador_{i}@empresa.com");
                req.Headers.Add("X-Audit-App", "StressTestHarness");

                var resp = await _client.SendAsync(req);
                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            });

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task GraphQL_Query_ReturnsProjectedFields()
        {
            // Arrange GraphQL Query
            var graphQlQuery = new
            {
                query = @"
                query {
                    businessPartners(where: { cardType: { eq: ""cCustomer"" } }) {
                        cardCode
                        cardName
                    }
                }"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
            {
                Content = new StringContent(JsonSerializer.Serialize(graphQlQuery), Encoding.UTF8, "application/json")
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rawJson = await response.Content.ReadAsStringAsync();
            Assert.Contains("businessPartners", rawJson);
        }

        [Fact]
        public async Task Logout_TerminatesSession_AndBlocksSubsequentCalls()
        {
            // 1. Iniciar sesión
            var loginDto = new LoginRequestDto { CompanyDB = "SBODEMO_TEST", UserName = "manager", Password = "manager123" };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/Login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
            Assert.NotNull(loginResult);

            // 2. Cerrar sesión
            var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Logout");
            logoutRequest.Headers.Add("B1SESSION", loginResult.SessionId);
            var logoutResponse = await _client.SendAsync(logoutRequest);
            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

            // 3. Intento de llamada post-logout
            var pingRequest = new HttpRequestMessage(HttpMethod.Get, "/api/test/secure-ping");
            pingRequest.Headers.Add("B1SESSION", loginResult.SessionId);
            var pingResponse = await _client.SendAsync(pingRequest);
            Assert.Equal(HttpStatusCode.Unauthorized, pingResponse.StatusCode);
        }

        // Clases de prueba aisladas exclusivamente en el proyecto de pruebas
        private class TestSapDiApiConnector : ISapDiApiConnector
        {
            public (bool Connected, string ErrorMessage) Connect(UserSession session, string password)
            {
                return (true, string.Empty);
            }

            public Task<BusinessPartnerDto?> GetBusinessPartnerAsync(UserSession session, string cardCode)
            {
                return Task.FromResult<BusinessPartnerDto?>(new BusinessPartnerDto
                {
                    CardCode = cardCode,
                    CardName = "Cliente Test SAP",
                    CardType = "cCustomer",
                    BPAddresses = new List<BPAddressDto>
                    {
                        new() { AddressName = "Fiscal", Street = "Av. Principal 100", AddressType = "bo_BillTo" }
                    }
                });
            }

            public Task<(bool Success, string CardCode, string? ErrorMessage)> CreateBusinessPartnerAsync(UserSession session, BusinessPartnerDto bpDto)
            {
                return Task.FromResult<(bool Success, string CardCode, string? ErrorMessage)>((true, bpDto.CardCode, null));
            }

            public Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateBusinessPartnerAsync(UserSession session, string cardCode, BusinessPartnerDto bpDto)
            {
                return Task.FromResult<(bool Success, string CardCode, string? ErrorMessage)>((true, cardCode, null));
            }

            public Task<(bool Success, int AttachmentEntry, string? ErrorMessage)> CreateOrUpdateAttachmentAsync(UserSession session, AttachmentDto dto)
            {
                return Task.FromResult<(bool Success, int AttachmentEntry, string? ErrorMessage)>((true, 100, null));
            }
        }

        private class TestBusinessPartnerService : IBusinessPartnerService
        {
            private readonly ISapDiApiConnector _sapConnector;

            public TestBusinessPartnerService(ISapDiApiConnector sapConnector)
            {
                _sapConnector = sapConnector;
            }

            public IQueryable<BusinessPartnerDto> GetBusinessPartnersQueryable()
            {
                return new List<BusinessPartnerDto>
                {
                    new() { CardCode = "C00001", CardName = "Cliente Test SAP", CardType = "cCustomer" }
                }.AsQueryable();
            }

            public Task<BusinessPartnerDto?> GetByCardCodeAsync(string cardCode, UserSession? session = null)
            {
                return _sapConnector.GetBusinessPartnerAsync(session ?? new UserSession(), cardCode);
            }

            public Task<IEnumerable<BusinessPartnerDto>> GetFilteredAsync(BusinessPartnerFilterDto filter, UserSession? session = null)
            {
                return Task.FromResult<IEnumerable<BusinessPartnerDto>>(new List<BusinessPartnerDto>
                {
                    new() { CardCode = "C00001", CardName = "Cliente Test SAP", CardType = "cCustomer" }
                });
            }

            public Task<(bool Success, string CardCode, string? ErrorMessage)> CreateAsync(BusinessPartnerDto dto, UserSession? session = null)
            {
                return _sapConnector.CreateBusinessPartnerAsync(session ?? new UserSession(), dto);
            }

            public Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateAsync(string cardCode, BusinessPartnerDto dto, UserSession? session = null)
            {
                return _sapConnector.UpdateBusinessPartnerAsync(session ?? new UserSession(), cardCode, dto);
            }
        }
    }
}
