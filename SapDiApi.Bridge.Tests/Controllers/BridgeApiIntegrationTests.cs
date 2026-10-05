using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Companies;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Models.Health;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Services.ApprovalRequests;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Drafts;
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

                    services.AddSingleton<IBusinessPartnerService>(
                        new TestBusinessPartnerService(testSapConnector));
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetHealth_Returns200_WithHealthyStatus()
        {
            var response = await _client.GetAsync("/api/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content =
                await response.Content.ReadFromJsonAsync<ApiResponse<HealthStatusDto>>();

            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.NotNull(content.Data);
            Assert.Equal("Healthy", content.Data.Status);
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200AndSessionId()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123",
                AuditUser = "juan.perez@empresa.com",
                AuditApp = "PortalProveedores"
            };

            var response =
                await _client.PostAsJsonAsync("/api/v1/Login", loginDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content =
                await response.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(content);
            Assert.False(string.IsNullOrWhiteSpace(content.SessionId));
            Assert.Equal(30, content.SessionTimeout);
        }

        [Fact]
        public async Task CreateBusinessPartner_WithValidPayload_Returns201Created()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync("/api/v1/Login", loginDto);

            var loginResult =
                await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var newBp = new BusinessPartnerDto
            {
                CardCode = "P000578",
                CardName = "GENTE CON TALENTO, S.A.",
                CardType = "cSupplier",
                GroupCode = 111,
                FederalTaxID = "000000000000"
            };

            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/api/v1/BusinessPartners")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(newBp),
                        Encoding.UTF8,
                        "application/json")
                };

            request.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var response =
                await _client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);
        }

        [Fact]
        public async Task UpdateBusinessPartner_Returns200OK()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var updateBp = new BusinessPartnerDto
            {
                CardName =
                    "GENTE CON TALENTO ACTUALIZADO, S.A.",
                Phone1 = "2222-3333"
            };

            var request =
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    "/api/v1/BusinessPartners('P000578')")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(updateBp),
                        Encoding.UTF8,
                        "application/json")
                };

            request.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var response =
                await _client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);
        }

        [Fact]
        public async Task CreateAttachment_Returns201Created()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var attachmentDto = new AttachmentDto
            {
                Lines = new List<AttachmentLineDto>
                {
                    new()
                    {
                        SourcePath = @"C:\Temp",
                        FileName = "Contrato.pdf",
                        U_TipoDoc = "CONTRATO"
                    }
                }
            };

            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/api/v1/Attachments")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(attachmentDto),
                        Encoding.UTF8,
                        "application/json")
                };

            request.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var response =
                await _client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);
        }

        [Fact]
        public async Task GetBusinessPartners_WithoutSession_Returns401Unauthorized()
        {
            var response =
                await _client.GetAsync(
                    "/api/v1/BusinessPartners");

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        [Fact]
        public async Task GetBusinessPartnerByCardCode_ReturnsExpectedCustomer()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/api/v1/BusinessPartners('C00001')");

            request.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var response =
                await _client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var content =
                await response.Content
                    .ReadFromJsonAsync<
                        ApiResponse<BusinessPartnerDto>>();

            Assert.NotNull(content);
            Assert.True(content.Success);
            Assert.Equal(
                "C00001",
                content.Data?.CardCode);

            Assert.NotEmpty(
                content.Data?.BPAddresses!);
        }

        [Fact]
        public async Task Concurrency_MultipleSimultaneousRequests_AreHandledSuccessfully()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var tasks =
                Enumerable.Range(1, 15)
                    .Select(async i =>
                    {
                        var req =
                            new HttpRequestMessage(
                                HttpMethod.Get,
                                "/api/v1/BusinessPartners('C00001')");

                        req.Headers.Add(
                            "B1SESSION",
                            loginResult.SessionId);

                        req.Headers.Add(
                            "X-Audit-User",
                            $"operador_{i}@empresa.com");

                        req.Headers.Add(
                            "X-Audit-App",
                            "StressTestHarness");

                        var resp =
                            await _client.SendAsync(req);

                        Assert.Equal(
                            HttpStatusCode.OK,
                            resp.StatusCode);
                    });

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task GraphQL_Query_ReturnsProjectedFields()
        {
            var graphQlQuery = new
            {
                query = @"
                query {
                    businessPartners(
                        where: {
                            cardType: {
                                eq: ""cCustomer""
                            }
                        }) {
                        cardCode
                        cardName
                    }
                }"
            };

            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/graphql")
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(
                            graphQlQuery),
                        Encoding.UTF8,
                        "application/json")
                };

            var response =
                await _client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var rawJson =
                await response.Content.ReadAsStringAsync();

            Assert.Contains(
                "businessPartners",
                rawJson);
        }

        [Fact]
        public async Task Logout_TerminatesSession_AndBlocksSubsequentCalls()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var logoutRequest =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/api/v1/Logout");

            logoutRequest.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var logoutResponse =
                await _client.SendAsync(logoutRequest);

            Assert.Equal(
                HttpStatusCode.NoContent,
                logoutResponse.StatusCode);

            var pingRequest =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/api/test/secure-ping");

            pingRequest.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var pingResponse =
                await _client.SendAsync(pingRequest);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                pingResponse.StatusCode);
        }

        [Fact]
        public async Task ApprovalRequests_GetByCode_And_Update_ReturnsSuccess()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var getReq =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/b1s/v1/ApprovalRequests(6106)");

            getReq.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var getResp =
                await _client.SendAsync(getReq);

            Assert.Equal(
                HttpStatusCode.OK,
                getResp.StatusCode);

            var content =
                await getResp.Content
                    .ReadFromJsonAsync<
                        ApiResponse<ApprovalRequestDto>>();

            Assert.NotNull(content?.Data);
            Assert.Equal(6106, content.Data.Code);
            Assert.Equal(
                "arsApproved",
                content.Data.Status);

            Assert.Equal(
                5137,
                content.Data.DraftEntry);

            var patchDto =
                new UpdateApprovalRequestDto
                {
                    CurrentStage = 9,
                    Status = "Y",
                    ApprovalRequestDecisions =
                        new List<
                            ApprovalRequestDecisionDto>
                        {
                            new()
                            {
                                Status =
                                    "ardApproved",

                                ApproverUserName =
                                    "manager",

                                Remarks =
                                    "Aprobado por Dirección Financiera"
                            }
                        }
                };

            var patchReq =
                new HttpRequestMessage(
                    HttpMethod.Patch,
                    "/b1s/v1/ApprovalRequests(6106)")
                {
                    Content =
                        JsonContent.Create(patchDto)
                };

            patchReq.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var patchResp =
                await _client.SendAsync(patchReq);

            Assert.Equal(
                HttpStatusCode.OK,
                patchResp.StatusCode);
        }

        [Fact]
        public async Task Drafts_GetByDocEntry_And_SaveDraftToDocument_ReturnsSuccess()
        {
            var loginDto = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/v1/Login",
                    loginDto);

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDto>();

            Assert.NotNull(loginResult);

            var getReq =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    "/b1s/v1/Drafts(5137)");

            getReq.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var getResp =
                await _client.SendAsync(getReq);

            Assert.Equal(
                HttpStatusCode.OK,
                getResp.StatusCode);

            var content =
                await getResp.Content
                    .ReadFromJsonAsync<
                        ApiResponse<DraftDto>>();

            Assert.NotNull(content?.Data);
            Assert.Equal(
                5137,
                content.Data.DocEntry);

            Assert.Equal(
                "P000009",
                content.Data.CardCode);

            Assert.Equal(
                "tYES",
                content.Data.Confirmed);

            Assert.NotEmpty(
                content.Data.DocumentLines);

            var saveReq =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "/b1s/v1/Drafts(5137)/SaveDraftToDocument");

            saveReq.Headers.Add(
                "B1SESSION",
                loginResult.SessionId);

            var saveResp =
                await _client.SendAsync(saveReq);

            Assert.Equal(
                HttpStatusCode.OK,
                saveResp.StatusCode);
        }

        // =====================================================
        // TEST SAP CONNECTOR
        // Fake aislado. No conecta con SAP real.
        // =====================================================

        private class TestSapDiApiConnector :
            ISapDiApiConnector
        {
            public (
                bool Connected,
                string ErrorMessage) Connect(
                    UserSession session,
                    string password)
            {
                return (
                    true,
                    string.Empty);
            }

            // =================================================
            // BUSINESS PARTNERS
            // =================================================

            public Task<BusinessPartnerDto?>
                GetBusinessPartnerAsync(
                    UserSession session,
                    string cardCode)
            {
                return Task.FromResult<
                    BusinessPartnerDto?>(
                    new BusinessPartnerDto
                    {
                        CardCode = cardCode,
                        CardName =
                            "Cliente Test SAP",
                        CardType =
                            "cCustomer",

                        BPAddresses =
                            new List<BPAddressDto>
                            {
                                new()
                                {
                                    AddressName =
                                        "Fiscal",

                                    Street =
                                        "Av. Principal 100",

                                    AddressType =
                                        "bo_BillTo"
                                }
                            }
                    });
            }

            public Task<(
                bool Success,
                string CardCode,
                string? ErrorMessage)>
                CreateBusinessPartnerAsync(
                    UserSession session,
                    BusinessPartnerDto bpDto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        CardCode:
                            bpDto.CardCode,

                        ErrorMessage:
                            (string?)null
                    ));
            }

            public Task<(
                bool Success,
                string CardCode,
                string? ErrorMessage)>
                UpdateBusinessPartnerAsync(
                    UserSession session,
                    string cardCode,
                    BusinessPartnerDto bpDto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        CardCode: cardCode,
                        ErrorMessage:
                            (string?)null
                    ));
            }

            // =================================================
            // ATTACHMENTS
            // =================================================

            public Task<(
                bool Success,
                int AttachmentEntry,
                string? ErrorMessage)>
                CreateOrUpdateAttachmentAsync(
                    UserSession session,
                    AttachmentDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        AttachmentEntry: 100,
                        ErrorMessage:
                            (string?)null
                    ));
            }

            // =================================================
            // APPROVAL REQUESTS
            // =================================================

            public Task<ApprovalRequestDto?> GetApprovalRequestAsync(
     UserSession session,
     int code)
            {
                return Task.FromResult<ApprovalRequestDto?>(
                    new ApprovalRequestDto
                    {
                        Code = code,
                        ApprovalTemplatesID = 74,
                        ObjectType = "22",
                        IsDraft = "Y",
                        Status = "arsApproved",
                        Remarks = "TEST",
                        CurrentStage = 9,
                        OriginatorID = 208,
                        CreationDate = "2026-07-16",
                        CreationTime = "11:49:00",
                        DraftEntry = 5137,
                        DraftType = "112",
                        ApprovalRequestLines = new List<ApprovalRequestLineDto>
                        {
                new()
                {
                    StageCode = 9,
                    UserID = 1,
                    Status = "ardApproved"
                }
                        }
                    });
            }

            public Task<IEnumerable<ApprovalRequestDto>> GetApprovalRequestsFilteredAsync(
                UserSession session,
                ApprovalRequestFilterDto filter)
            {
                return Task.FromResult<IEnumerable<ApprovalRequestDto>>(
                    new List<ApprovalRequestDto>
                    {
            new()
            {
                Code = 6106,
                Status = "arsApproved",
                DraftEntry = 5137
            }
                    });
            }

            public Task<(bool Success, int Code, string? ErrorMessage)> UpdateApprovalRequestAsync(
                UserSession session,
                int code,
                UpdateApprovalRequestDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        Code: code,
                        ErrorMessage: (string?)null
                    ));
            }

            // =========================================================
            // DRAFTS
            // =========================================================

            public Task<DraftDto?> GetDraftAsync(
                UserSession session,
                int docEntry)
            {
                return Task.FromResult<DraftDto?>(
                    new DraftDto
                    {
                        DocEntry = docEntry,
                        DocNum = 2004241,
                        DocType = "dDocument_Items",
                        CardCode = "P000009",
                        CardName = "CONSTRUCTORA QUIMAC, S.A.",
                        DocTotal = 14820.860m,
                        Comments = "TEST SRV",
                        Confirmed = "tYES",
                        DocumentLines = new List<DraftDocumentLineDto>
                        {
                new()
                {
                    LineNum = 0,
                    ItemCode = "SRV0188",
                    ItemDescription = "BODEGA",
                    Quantity = 1,
                    Price = 13232.91m
                }
                        }
                    });
            }

            public Task<IEnumerable<DraftDto>> GetDraftsFilteredAsync(
                UserSession session,
                DraftFilterDto filter)
            {
                return Task.FromResult<IEnumerable<DraftDto>>(
                    new List<DraftDto>
                    {
            new()
            {
                DocEntry = 5137,
                CardCode = "P000009",
                CardName = "CONSTRUCTORA QUIMAC, S.A."
            }
                    });
            }

            public Task<(
                bool Success,
                int DocEntry,
                int? GeneratedDocEntry,
                string? ErrorMessage)> SaveDraftToDocumentAsync(
                    UserSession session,
                    int docEntry)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        DocEntry: docEntry,
                        GeneratedDocEntry: (int?)9999,
                        ErrorMessage: (string?)null
                    ));
            }

            // =========================================================
            // USERS
            // =========================================================

            public Task<UserDto?> GetUserByIdAsync(
                UserSession session,
                int internalKey,
                bool includePermissions = false)
            {
                return Task.FromResult<UserDto?>(null);
            }

            public Task<UserDto?> GetUserByCodeAsync(
                UserSession session,
                string userCode,
                bool includePermissions = false)
            {
                return Task.FromResult<UserDto?>(null);
            }

            public Task<IEnumerable<UserDto>> GetUsersFilteredAsync(
                UserSession session,
                UserFilterDto filter)
            {
                return Task.FromResult<IEnumerable<UserDto>>(
                    new List<UserDto>());
            }

            public Task<(bool Success, int InternalKey, string? ErrorMessage)> CreateUserAsync(
                UserSession session,
                CreateUserDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        InternalKey: 1,
                        ErrorMessage: (string?)null
                    ));
            }

            public Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateUserAsync(
                UserSession session,
                int internalKey,
                UpdateUserDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        InternalKey: internalKey,
                        ErrorMessage: (string?)null
                    ));
            }

            public Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateUserByCodeAsync(
                UserSession session,
                string userCode,
                UpdateUserDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        InternalKey: 1,
                        ErrorMessage: (string?)null
                    ));
            }

            public Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangeUserPasswordAsync(
                UserSession session,
                int internalKey,
                ChangeUserPasswordDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        InternalKey: internalKey,
                        ErrorMessage: (string?)null
                    ));
            }

            public Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangeUserPasswordByCodeAsync(
                UserSession session,
                string userCode,
                ChangeUserPasswordDto dto)
            {
                return Task.FromResult(
                    (
                        Success: true,
                        InternalKey: 1,
                        ErrorMessage: (string?)null
                    ));
            }

            // =========================================================
            // COMPANIES
            // =========================================================

            public Task<List<CompanyDto>> GetSapCompaniesFromSrgcAsync(
                UserSession session)
            {
                return Task.FromResult(
                    new List<CompanyDto>());
            }
        }

        private class TestBusinessPartnerService : IBusinessPartnerService
        {
            private readonly ISapDiApiConnector _sapConnector;

            public TestBusinessPartnerService(
                ISapDiApiConnector sapConnector)
            {
                _sapConnector = sapConnector;
            }

            public IQueryable<BusinessPartnerDto> GetBusinessPartnersQueryable()
            {
                return new List<BusinessPartnerDto>
                        {
                            new()
                            {
                                CardCode = "C00001",
                                CardName = "Cliente Test SAP",
                                CardType = "cCustomer"
                            }
                        }.AsQueryable();
            }

            public Task<BusinessPartnerDto?> GetByCardCodeAsync(
                string cardCode,
                UserSession? session = null)
            {
                return _sapConnector.GetBusinessPartnerAsync(
                    session ?? new UserSession(),
                    cardCode);
            }

            public Task<IEnumerable<BusinessPartnerDto>> GetFilteredAsync(
                BusinessPartnerFilterDto filter,
                UserSession? session = null)
            {
                return Task.FromResult<IEnumerable<BusinessPartnerDto>>(
                    new List<BusinessPartnerDto>
                    {
                                new()
                                {
                                    CardCode = "C00001",
                                    CardName = "Cliente Test SAP",
                                    CardType = "cCustomer"
                                }
                    });
            }

            public Task<(bool Success, string CardCode, string? ErrorMessage)> CreateAsync(
                BusinessPartnerDto dto,
                UserSession? session = null)
            {
                return _sapConnector.CreateBusinessPartnerAsync(
                    session ?? new UserSession(),
                    dto);
            }

            public Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateAsync(
                string cardCode,
                BusinessPartnerDto dto,
                UserSession? session = null)
            {
                return _sapConnector.UpdateBusinessPartnerAsync(
                    session ?? new UserSession(),
                    cardCode,
                    dto);
            }
        }
    }
}
