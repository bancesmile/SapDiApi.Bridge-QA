using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Services.Sap;
using SapDiApi.Bridge.Services.Users;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Users
{
    public class UserServiceTests
    {
        private static UserSession CreateSession()
        {
            return new UserSession
            {
                SessionId = "QA-SESSION",
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123",
                AuditUser = "qa@milesimo.com.gt",
                AuditApp = "QA-Automation"
            };
        }

        // =====================================================
        // UT-060
        // Queryable actual
        // =====================================================
        [Fact]
        public void GetUsersQueryable_CurrentImplementation_ReturnsEmpty()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = service.GetUsersQueryable();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // =====================================================
        // UT-061
        // Buscar por ID sin sesion
        // =====================================================
        [Fact]
        public async Task GetById_WithoutSession_ReturnsNull()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.GetByIdAsync(
                1,
                false,
                null);

            Assert.Null(result);

            connector.Verify(
                x => x.GetUserByIdAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        // =====================================================
        // UT-062
        // Buscar por ID con sesion
        // =====================================================
        [Fact]
        public async Task GetById_WithSession_ReturnsUser()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var expected = new UserDto();

            connector
                .Setup(x => x.GetUserByIdAsync(
                    It.IsAny<UserSession>(),
                    1,
                    true))
                .ReturnsAsync(expected);

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.GetByIdAsync(
                1,
                true,
                CreateSession());

            Assert.Same(expected, result);

            connector.Verify(
                x => x.GetUserByIdAsync(
                    It.IsAny<UserSession>(),
                    1,
                    true),
                Times.Once);
        }

        // =====================================================
        // UT-063
        // Buscar por codigo sin sesion
        // =====================================================
        [Fact]
        public async Task GetByCode_WithoutSession_ReturnsNull()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.GetByCodeAsync(
                "manager",
                false,
                null);

            Assert.Null(result);

            connector.Verify(
                x => x.GetUserByCodeAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        // =====================================================
        // UT-064
        // Buscar por codigo con sesion
        // =====================================================
        [Fact]
        public async Task GetByCode_WithSession_ReturnsUser()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var expected = new UserDto();

            connector
                .Setup(x => x.GetUserByCodeAsync(
                    It.IsAny<UserSession>(),
                    "usuario_qa",
                    true))
                .ReturnsAsync(expected);

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.GetByCodeAsync(
                "usuario_qa",
                true,
                CreateSession());

            Assert.Same(expected, result);
        }

        // =====================================================
        // UT-065
        // Filtrar sin sesion
        // =====================================================
        [Fact]
        public async Task GetFiltered_WithoutSession_ReturnsEmpty()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.GetFilteredAsync(
                new UserFilterDto(),
                null);

            Assert.NotNull(result);
            Assert.Empty(result);

            connector.Verify(
                x => x.GetUsersFilteredAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<UserFilterDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-066
        // Filtrar con sesion
        // =====================================================
        [Fact]
        public async Task GetFiltered_WithSession_ReturnsUsers()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var expected = new List<UserDto>
            {
                new(),
                new()
            };

            connector
                .Setup(x => x.GetUsersFilteredAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<UserFilterDto>()))
                .ReturnsAsync(expected);

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.GetFilteredAsync(
                new UserFilterDto(),
                CreateSession());

            Assert.Equal(2, result.Count());
        }

        // =====================================================
        // UT-067
        // Crear sin sesion
        // =====================================================
        [Fact]
        public async Task Create_WithoutSession_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var dto = new CreateUserDto
            {
                UserCode = "usuario_qa",
                UserName = "Usuario QA"
            };

            var result = await service.CreateAsync(
                dto,
                null);

            Assert.False(result.Success);
            Assert.Equal(0, result.InternalKey);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.CreateUserAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<CreateUserDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-068
        // Crear con sesion
        // =====================================================
        [Fact]
        public async Task Create_WithSession_ReturnsSuccess()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.CreateUserAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<CreateUserDto>()))
                .ReturnsAsync((
                    true,
                    100,
                    (string?)null));

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var dto = new CreateUserDto
            {
                UserCode = "usuario_qa",
                UserName = "Usuario QA"
            };

            var result = await service.CreateAsync(
                dto,
                CreateSession());

            Assert.True(result.Success);
            Assert.Equal(100, result.InternalKey);
            Assert.Null(result.ErrorMessage);
        }

        // =====================================================
        // UT-069
        // Actualizar por ID sin sesion
        // =====================================================
        [Fact]
        public async Task Update_WithoutSession_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.UpdateAsync(
                100,
                new UpdateUserDto(),
                null);

            Assert.False(result.Success);
            Assert.Equal(100, result.InternalKey);

            connector.Verify(
                x => x.UpdateUserAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>(),
                    It.IsAny<UpdateUserDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-070
        // Actualizar por ID con sesion
        // =====================================================
        [Fact]
        public async Task Update_WithSession_ReturnsSuccess()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.UpdateUserAsync(
                    It.IsAny<UserSession>(),
                    100,
                    It.IsAny<UpdateUserDto>()))
                .ReturnsAsync((
                    true,
                    100,
                    (string?)null));

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.UpdateAsync(
                100,
                new UpdateUserDto(),
                CreateSession());

            Assert.True(result.Success);
            Assert.Equal(100, result.InternalKey);
        }

        // =====================================================
        // UT-071
        // Actualizar por codigo sin sesion
        // =====================================================
        [Fact]
        public async Task UpdateByCode_WithoutSession_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.UpdateByCodeAsync(
                "usuario_qa",
                new UpdateUserDto(),
                null);

            Assert.False(result.Success);
            Assert.Equal(0, result.InternalKey);

            connector.Verify(
                x => x.UpdateUserByCodeAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>(),
                    It.IsAny<UpdateUserDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-072
        // Actualizar por codigo con sesion
        // =====================================================
        [Fact]
        public async Task UpdateByCode_WithSession_ReturnsSuccess()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.UpdateUserByCodeAsync(
                    It.IsAny<UserSession>(),
                    "usuario_qa",
                    It.IsAny<UpdateUserDto>()))
                .ReturnsAsync((
                    true,
                    100,
                    (string?)null));

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.UpdateByCodeAsync(
                "usuario_qa",
                new UpdateUserDto(),
                CreateSession());

            Assert.True(result.Success);
            Assert.Equal(100, result.InternalKey);
        }

        // =====================================================
        // UT-073
        // Cambiar password por ID sin sesion
        // =====================================================
        [Fact]
        public async Task ChangePassword_WithoutSession_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.ChangePasswordAsync(
                100,
                new ChangeUserPasswordDto(),
                null);

            Assert.False(result.Success);
            Assert.Equal(100, result.InternalKey);

            connector.Verify(
                x => x.ChangeUserPasswordAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>(),
                    It.IsAny<ChangeUserPasswordDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-074
        // Cambiar password por ID con sesion
        // =====================================================
        [Fact]
        public async Task ChangePassword_WithSession_ReturnsSuccess()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.ChangeUserPasswordAsync(
                    It.IsAny<UserSession>(),
                    100,
                    It.IsAny<ChangeUserPasswordDto>()))
                .ReturnsAsync((
                    true,
                    100,
                    (string?)null));

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.ChangePasswordAsync(
                100,
                new ChangeUserPasswordDto(),
                CreateSession());

            Assert.True(result.Success);
            Assert.Equal(100, result.InternalKey);
        }

        // =====================================================
        // UT-075
        // Cambiar password por codigo sin sesion
        // =====================================================
        [Fact]
        public async Task ChangePasswordByCode_WithoutSession_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.ChangePasswordByCodeAsync(
                "usuario_qa",
                new ChangeUserPasswordDto(),
                null);

            Assert.False(result.Success);
            Assert.Equal(0, result.InternalKey);

            connector.Verify(
                x => x.ChangeUserPasswordByCodeAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>(),
                    It.IsAny<ChangeUserPasswordDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-076
        // Cambiar password por codigo con sesion
        // =====================================================
        [Fact]
        public async Task ChangePasswordByCode_WithSession_ReturnsSuccess()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.ChangeUserPasswordByCodeAsync(
                    It.IsAny<UserSession>(),
                    "usuario_qa",
                    It.IsAny<ChangeUserPasswordDto>()))
                .ReturnsAsync((
                    true,
                    100,
                    (string?)null));

            var service = new UserService(
                connector.Object,
                NullLogger<UserService>.Instance);

            var result = await service.ChangePasswordByCodeAsync(
                "usuario_qa",
                new ChangeUserPasswordDto(),
                CreateSession());

            Assert.True(result.Success);
            Assert.Equal(100, result.InternalKey);
            Assert.Null(result.ErrorMessage);
        }
    }
}