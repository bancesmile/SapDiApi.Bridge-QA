using Microsoft.Data.Sqlite;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Companies;
using SapDiApi.Bridge.Services.Sap;
using System.Collections.Concurrent;

namespace SapDiApi.Bridge.Services.Companies
{
    /// <summary>
    /// Implementación de alta velocidad con caché en memoria RAM y persistencia SQLite local.
    /// Resuelve identificadores de empresa (`CompanyId` / `CompanyCode`) en 0 ms.
    /// </summary>
    public class CompanyResolverService : ICompanyResolverService
    {
        private readonly string _connectionString;
        private readonly string _defaultCompanyDb;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CompanyResolverService> _logger;

        private readonly ConcurrentDictionary<int, CompanyDto> _byId = new();
        private readonly ConcurrentDictionary<string, CompanyDto> _byCode = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, CompanyDto> _byDbName = new(StringComparer.OrdinalIgnoreCase);

        public CompanyResolverService(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<CompanyResolverService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _defaultCompanyDb = configuration["SapSettings:DefaultCompanyDB"] ?? string.Empty;

            var dbPath = configuration["Database:MetadataDbPath"] ?? "Data/bridge_metadata.db";
            var fullPath = System.IO.Path.IsPathRooted(dbPath) ? dbPath : System.IO.Path.Combine(AppContext.BaseDirectory, dbPath);

            var dir = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            _connectionString = $"Data Source={fullPath}";

            InitializeDatabase();
            LoadCacheFromDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS CatEmpresasSap (
                        CompanyId INTEGER PRIMARY KEY AUTOINCREMENT,
                        CompanyCode TEXT NOT NULL UNIQUE,
                        SapDatabase TEXT NOT NULL UNIQUE,
                        CompanyName TEXT NOT NULL,
                        Localization TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_CatEmpresasSap_Code ON CatEmpresasSap(CompanyCode);
                    CREATE INDEX IF NOT EXISTS IX_CatEmpresasSap_Db ON CatEmpresasSap(SapDatabase);
                ";

                using var cmd = new SqliteCommand(createTableSql, conn);
                cmd.ExecuteNonQuery();

                _logger.LogInformation("Base de datos SQLite de metadatos inicializada exitosamente ({Conn}).", _connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al inicializar la base de datos local SQLite de empresas.");
            }
        }

        private void LoadCacheFromDatabase()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string selectSql = @"
                    SELECT CompanyId, CompanyCode, SapDatabase, CompanyName, Localization, IsActive, CreatedAt, UpdatedAt
                    FROM CatEmpresasSap
                ";

                using var cmd = new SqliteCommand(selectSql, conn);
                using var reader = cmd.ExecuteReader();

                _byId.Clear();
                _byCode.Clear();
                _byDbName.Clear();

                int count = 0;
                while (reader.Read())
                {
                    var dto = new CompanyDto
                    {
                        CompanyId = reader.GetInt32(0),
                        CompanyCode = reader.GetString(1),
                        SapDatabase = reader.GetString(2),
                        CompanyName = reader.GetString(3),
                        Localization = reader.IsDBNull(4) ? null : reader.GetString(4),
                        IsActive = reader.GetInt32(5) == 1,
                        CreatedAt = DateTime.TryParse(reader.GetString(6), out var cAt) ? cAt : null,
                        UpdatedAt = reader.IsDBNull(7) ? null : (DateTime.TryParse(reader.GetString(7), out var uAt) ? uAt : null)
                    };

                    _byId[dto.CompanyId] = dto;
                    _byCode[dto.CompanyCode] = dto;
                    _byDbName[dto.SapDatabase] = dto;
                    count++;
                }

                _logger.LogInformation("Caché en memoria de Sociedades SAP cargada exitosamente: {Count} empresas activas/registradas.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la caché de empresas desde SQLite.");
            }
        }

        public (bool Found, CompanyDto? Company, string SapDatabase) ResolveCompany(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                if (!string.IsNullOrWhiteSpace(_defaultCompanyDb))
                {
                    if (_byDbName.TryGetValue(_defaultCompanyDb, out var defaultComp))
                    {
                        return (true, defaultComp, defaultComp.SapDatabase);
                    }
                    return (true, null, _defaultCompanyDb);
                }
                return (false, null, string.Empty);
            }

            var cleanId = identifier.Trim();

            // 1. Intentar resolver por CompanyId numérico (ej: "1", "42")
            if (int.TryParse(cleanId, out int numericId))
            {
                if (_byId.TryGetValue(numericId, out var compById))
                {
                    return (true, compById, compById.SapDatabase);
                }
            }

            // 2. Intentar resolver por CompanyCode (ej: "DIST_NORTE", "SUC01")
            if (_byCode.TryGetValue(cleanId, out var compByCode))
            {
                return (true, compByCode, compByCode.SapDatabase);
            }

            // 3. Intentar resolver por SapDatabase directo (ej: "SBO_DIST_NORTE_PROD")
            if (_byDbName.TryGetValue(cleanId, out var compByDb))
            {
                return (true, compByDb, compByDb.SapDatabase);
            }

            // 4. Si no está en el catálogo pero parece un nombre directo de base de datos SAP, permitir fallback
            return (false, null, cleanId);
        }

        public IEnumerable<CompanyDto> GetAll(bool onlyActive = true)
        {
            var list = _byId.Values.AsEnumerable();
            return onlyActive ? list.Where(c => c.IsActive) : list;
        }

        public IEnumerable<PublicCompanyDto> GetPublicCompanies(bool onlyActive = true)
        {
            return GetAll(onlyActive).Select(c => new PublicCompanyDto
            {
                CompanyId = c.CompanyId,
                CompanyCode = c.CompanyCode,
                CompanyName = c.CompanyName,
                Localization = c.Localization,
                IsActive = c.IsActive
            });
        }

        public CompanyDto? GetById(int companyId)
        {
            return _byId.TryGetValue(companyId, out var comp) ? comp : null;
        }

        public CompanyDto? GetByCode(string companyCode)
        {
            if (string.IsNullOrWhiteSpace(companyCode)) return null;
            return _byCode.TryGetValue(companyCode.Trim(), out var comp) ? comp : null;
        }

        public Task ReloadCacheAsync()
        {
            LoadCacheFromDatabase();
            return Task.CompletedTask;
        }

        public async Task<(int TotalSynced, int NewAdded, int Updated)> SyncFromSapAsync(UserSession? session = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var sapConnector = scope.ServiceProvider.GetRequiredService<ISapDiApiConnector>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var defaultDb = config["SapSettings:DefaultCompanyDB"] ?? _defaultCompanyDb;
            var defaultUser = config["SapSettings:DefaultUserName"] ?? "manager";
            var defaultPass = config["SapSettings:ServicePassword"] ?? string.Empty;

            var effectiveSession = session ?? new UserSession
            {
                CompanyDB = defaultDb,
                UserName = defaultUser,
                Password = defaultPass,
                AuditUser = "SystemCompanySync"
            };

            var sapCompanies = await sapConnector.GetSapCompaniesFromSrgcAsync(effectiveSession);
            if (sapCompanies == null || sapCompanies.Count == 0)
            {
                _logger.LogWarning("No se encontraron sociedades en SBOCOMMON.SRGC.");
                return (0, 0, 0);
            }

            int newAdded = 0;
            int updated = 0;
            var nowIso = DateTime.UtcNow.ToString("o");

            using (var conn = new SqliteConnection(_connectionString))
            {
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();

                foreach (var sapComp in sapCompanies)
                {
                    if (string.IsNullOrWhiteSpace(sapComp.SapDatabase)) continue;

                    string checkSql = "SELECT CompanyId, CompanyCode FROM CatEmpresasSap WHERE SapDatabase = @db";
                    using var checkCmd = new SqliteCommand(checkSql, conn, tx);
                    checkCmd.Parameters.AddWithValue("@db", sapComp.SapDatabase);

                    using var checkReader = await checkCmd.ExecuteReaderAsync();
                    if (checkReader.Read())
                    {
                        int existingId = checkReader.GetInt32(0);
                        checkReader.Close();

                        string updateSql = @"
                            UPDATE CatEmpresasSap
                            SET CompanyName = @name, Localization = @loc, IsActive = @active, UpdatedAt = @now
                            WHERE CompanyId = @id
                        ";
                        using var updateCmd = new SqliteCommand(updateSql, conn, tx);
                        updateCmd.Parameters.AddWithValue("@name", sapComp.CompanyName);
                        updateCmd.Parameters.AddWithValue("@loc", (object?)sapComp.Localization ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@active", sapComp.IsActive ? 1 : 0);
                        updateCmd.Parameters.AddWithValue("@now", nowIso);
                        updateCmd.Parameters.AddWithValue("@id", existingId);

                        await updateCmd.ExecuteNonQueryAsync();
                        updated++;
                    }
                    else
                    {
                        checkReader.Close();

                        string generatedCode = GenerateFriendlyCode(sapComp.SapDatabase);
                        string insertSql = @"
                            INSERT INTO CatEmpresasSap (CompanyCode, SapDatabase, CompanyName, Localization, IsActive, CreatedAt, UpdatedAt)
                            VALUES (@code, @db, @name, @loc, @active, @now, @now)
                        ";
                        using var insertCmd = new SqliteCommand(insertSql, conn, tx);
                        insertCmd.Parameters.AddWithValue("@code", generatedCode);
                        insertCmd.Parameters.AddWithValue("@db", sapComp.SapDatabase);
                        insertCmd.Parameters.AddWithValue("@name", sapComp.CompanyName);
                        insertCmd.Parameters.AddWithValue("@loc", (object?)sapComp.Localization ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@active", sapComp.IsActive ? 1 : 0);
                        insertCmd.Parameters.AddWithValue("@now", nowIso);

                        await insertCmd.ExecuteNonQueryAsync();
                        newAdded++;
                    }
                }

                await tx.CommitAsync();
            }

            LoadCacheFromDatabase();

            _logger.LogInformation("Sincronización de empresas desde SAP finalizada: {Total} procesadas, {New} agregadas, {Updated} actualizadas.",
                sapCompanies.Count, newAdded, updated);

            return (sapCompanies.Count, newAdded, updated);
        }

        public async Task<bool> UpsertCompanyAsync(CompanyDto company)
        {
            if (string.IsNullOrWhiteSpace(company.CompanyCode) || string.IsNullOrWhiteSpace(company.SapDatabase))
            {
                return false;
            }

            var nowIso = DateTime.UtcNow.ToString("o");
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string upsertSql = @"
                INSERT INTO CatEmpresasSap (CompanyCode, SapDatabase, CompanyName, Localization, IsActive, CreatedAt, UpdatedAt)
                VALUES (@code, @db, @name, @loc, @active, @now, @now)
                ON CONFLICT(SapDatabase) DO UPDATE SET
                    CompanyCode = excluded.CompanyCode,
                    CompanyName = excluded.CompanyName,
                    Localization = excluded.Localization,
                    IsActive = excluded.IsActive,
                    UpdatedAt = excluded.UpdatedAt;
            ";

            using var cmd = new SqliteCommand(upsertSql, conn);
            cmd.Parameters.AddWithValue("@code", company.CompanyCode.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@db", company.SapDatabase.Trim());
            cmd.Parameters.AddWithValue("@name", company.CompanyName.Trim());
            cmd.Parameters.AddWithValue("@loc", (object?)company.Localization ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@active", company.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@now", nowIso);

            await cmd.ExecuteNonQueryAsync();
            LoadCacheFromDatabase();
            return true;
        }

        private static string GenerateFriendlyCode(string dbName)
        {
            var clean = dbName.Trim();
            if (clean.StartsWith("SBO_", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(4);
            }
            return clean.ToUpperInvariant();
        }
    }
}
