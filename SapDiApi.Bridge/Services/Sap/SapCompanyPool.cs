using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using SAPbobsCOM;
using SapDiApi.Bridge.Models.Sap;

namespace SapDiApi.Bridge.Services.Sap
{
    /// <summary>
    /// Pool de conexiones inteligentes persistentes (Keep-Alive) para SAP Business One DI API.
    /// Mantiene instancias activas por sociedad para responder en ~20ms y evitar crashes de memoria COM por ciclos continuos de Connect/Disconnect.
    /// </summary>
    public class SapCompanyPool : ISapCompanyPool, IDisposable
    {
        private readonly ConcurrentDictionary<string, PooledCompanyEntry> _companyCache = new();
        private readonly SemaphoreSlim _globalThrottle;
        private readonly int _maxConcurrent;
        private readonly int _timeoutSeconds;
        private readonly int _idleTimeoutMinutes;
        private readonly ILogger<SapCompanyPool> _logger;
        private readonly Timer _cleanupTimer;
        private bool _isDisposed = false;

        public int CurrentActiveConnections => _companyCache.Count(c => c.Value.IsConnected);
        public int MaxConcurrentConnections => _maxConcurrent;

        public SapCompanyPool(IConfiguration configuration, ILogger<SapCompanyPool> logger)
        {
            _logger = logger;
            _maxConcurrent = configuration.GetValue<int>("SapSettings:MaxConcurrentConnections", 5);
            _timeoutSeconds = configuration.GetValue<int>("SapSettings:ConnectionTimeoutSeconds", 30);
            _idleTimeoutMinutes = configuration.GetValue<int>("SapSettings:IdleConnectionTimeoutMinutes", 15);
            _globalThrottle = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);

            // Timer de limpieza en segundo plano para desconectar sociedades inactivas cada 2 minutos
            _cleanupTimer = new Timer(CleanupIdleConnections, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

            _logger.LogInformation("SapCompanyPool inicializado con caché persistente multi-empresa (Keep-Alive: {Idle}min, MaxConcurrente: {Max}).",
                _idleTimeoutMinutes, _maxConcurrent);
        }

        public async Task<T> ExecuteAsync<T>(SapConnectionInfo connInfo, Func<Company, Task<T>> action)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SapCompanyPool));

            var timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            bool globalAcquired = await _globalThrottle.WaitAsync(timeout);
            if (!globalAcquired)
            {
                _logger.LogWarning("Timeout esperando slot global de concurrencia en SAP ({Timeout}s). Límite actual: {Max}",
                    _timeoutSeconds, _maxConcurrent);
                throw new TimeoutException($"El servidor de SAP está ocupado procesando {_maxConcurrent} solicitudes simultáneas. Por favor reintente en unos momentos.");
            }

            var poolKey = connInfo.GenerarClaveConexion();
            var entry = _companyCache.GetOrAdd(poolKey, key => new PooledCompanyEntry(key, connInfo, _logger));

            try
            {
                // Adquirir el semáforo exclusivo de esta empresa para garantizar thread-safety COM
                bool entryAcquired = await entry.Lock.WaitAsync(timeout);
                if (!entryAcquired)
                {
                    throw new TimeoutException($"La sociedad '{connInfo.CompanyDB}' está procesando otra operación. Intente nuevamente.");
                }

                try
                {
                    var company = entry.GetOrCreateConnectedCompany(CreateCompanyInstance);
                    entry.UpdateActivity();
                    return await action(company);
                }
                catch (Exception ex) when (entry.IsConnectionBroken(ex))
                {
                    _logger.LogWarning("Conexión con SAP interrumpida para {Key}. Reintentando con nueva instancia...", poolKey);
                    entry.InvalidateAndDisconnect();
                    var company = entry.GetOrCreateConnectedCompany(CreateCompanyInstance);
                    entry.UpdateActivity();
                    return await action(company);
                }
                finally
                {
                    entry.Lock.Release();
                }
            }
            finally
            {
                _globalThrottle.Release();
            }
        }

        private Company CreateCompanyInstance(SapConnectionInfo connInfo)
        {
            var dbServerType = connInfo.DbServerType.ToUpperInvariant() switch
            {
                "DST_HANADB" or "HANADB" or "HANA" => BoDataServerTypes.dst_HANADB,
                "DST_MSSQL2019" or "MSSQL2019" => BoDataServerTypes.dst_MSSQL2019,
                "DST_MSSQL2017" or "MSSQL2017" => BoDataServerTypes.dst_MSSQL2017,
                "DST_MSSQL2016" or "MSSQL2016" => BoDataServerTypes.dst_MSSQL2016,
                "DST_MSSQL2014" or "MSSQL2014" => BoDataServerTypes.dst_MSSQL2014,
                _ => BoDataServerTypes.dst_HANADB
            };

            var company = new Company
            {
                Server = connInfo.Server,
                LicenseServer = connInfo.LicenseServer,
                CompanyDB = connInfo.CompanyDB,
                UserName = connInfo.UserName,
                Password = connInfo.Password,
                DbServerType = dbServerType,
                UseTrusted = connInfo.UseTrusted,
                language = BoSuppLangs.ln_English
            };

            if (!string.IsNullOrWhiteSpace(connInfo.DbUserName))
                company.DbUserName = connInfo.DbUserName;

            if (!string.IsNullOrWhiteSpace(connInfo.DbPassword))
                company.DbPassword = connInfo.DbPassword;

            if (!string.IsNullOrWhiteSpace(connInfo.SLDServer))
                company.SLDServer = connInfo.SLDServer;

            return company;
        }

        private void CleanupIdleConnections(object? state)
        {
            if (_isDisposed) return;

            var threshold = DateTime.UtcNow.AddMinutes(-_idleTimeoutMinutes);
            var idleKeys = _companyCache
                .Where(pair => pair.Value.LastUsedUtc < threshold && pair.Value.IsConnected)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in idleKeys)
            {
                if (_companyCache.TryGetValue(key, out var entry))
                {
                    // Intentar adquirir el semáforo sin bloquear si está en uso
                    if (entry.Lock.Wait(0))
                    {
                        try
                        {
                            if (entry.LastUsedUtc < threshold && entry.IsConnected)
                            {
                                _logger.LogInformation("DI API: Cerrando conexión inactiva para '{Key}' por superar {Idle} minutos de inactividad.",
                                    key, _idleTimeoutMinutes);
                                entry.InvalidateAndDisconnect();
                            }
                        }
                        finally
                        {
                            entry.Lock.Release();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _cleanupTimer.Dispose();

            foreach (var pair in _companyCache)
            {
                try
                {
                    pair.Value.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error al liberar recurso en pool para {Key}: {Msg}", pair.Key, ex.Message);
                }
            }

            _companyCache.Clear();
            _globalThrottle.Dispose();
        }
    }

    /// <summary>
    /// Representa una entrada de conexión persistente con control de concurrencia y estado de salud.
    /// </summary>
    internal class PooledCompanyEntry : IDisposable
    {
        private readonly string _key;
        private readonly SapConnectionInfo _connInfo;
        private readonly ILogger _logger;
        public SemaphoreSlim Lock { get; } = new(1, 1);
        private Company? _company;
        public DateTime LastUsedUtc { get; private set; } = DateTime.UtcNow;

        public bool IsConnected
        {
            get
            {
                try
                {
                    return _company != null && _company.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        public PooledCompanyEntry(string key, SapConnectionInfo connInfo, ILogger logger)
        {
            _key = key;
            _connInfo = connInfo;
            _logger = logger;
        }

        public void UpdateActivity()
        {
            LastUsedUtc = DateTime.UtcNow;
        }

        public Company GetOrCreateConnectedCompany(Func<SapConnectionInfo, Company> companyFactory)
        {
            if (_company != null && _company.Connected)
            {
                return _company;
            }

            InvalidateAndDisconnect();

            _logger.LogInformation("DI API: Estableciendo conexión con SAP para '{DB}' ({User})...",
                _connInfo.CompanyDB, _connInfo.UserName);

            var company = companyFactory(_connInfo);
            int connectResult = company.Connect();

            if (connectResult != 0)
            {
                string errDesc = company.GetLastErrorDescription();
                _connInfo.RegistrarIntentoConexion(connectResult, errDesc);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try { Marshal.FinalReleaseComObject(company); } catch { }
                }

                throw new Exception($"Fallo de conexión en SAP DI API ({connectResult}): {errDesc}");
            }

            _connInfo.ReiniciarContadores();
            _company = company;
            _logger.LogInformation("DI API: Conexión persistente establecida con éxito para '{DB}'. Versión SAP: {Version}",
                _connInfo.CompanyDB, company.Version);

            return _company;
        }

        public bool IsConnectionBroken(Exception ex)
        {
            var msg = ex.Message.ToLowerInvariant();
            return msg.Contains("not connected") ||
                   msg.Contains("connection is closed") ||
                   msg.Contains("rpc server is unavailable") ||
                   msg.Contains("-107") ||
                   msg.Contains("-108") ||
                   msg.Contains("-111");
        }

        public void InvalidateAndDisconnect()
        {
            if (_company != null)
            {
                try
                {
                    if (_company.Connected)
                    {
                        _company.Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Aviso al desconectar Company {Key}: {Msg}", _key, ex.Message);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        Marshal.FinalReleaseComObject(_company);
                    }
                    catch { }
                }

                _company = null;
            }
        }

        public void Dispose()
        {
            InvalidateAndDisconnect();
            Lock.Dispose();
        }
    }
}
