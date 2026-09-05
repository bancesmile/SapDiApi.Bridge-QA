using System.Runtime.InteropServices;
using SAPbobsCOM;
using SapDiApi.Bridge.Models.Sap;

namespace SapDiApi.Bridge.Services.Sap
{
    public class SapCompanyPool : ISapCompanyPool, IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConcurrent;
        private readonly int _timeoutSeconds;
        private readonly ILogger<SapCompanyPool> _logger;
        private int _activeCount = 0;

        public int CurrentActiveConnections => _activeCount;
        public int MaxConcurrentConnections => _maxConcurrent;

        public SapCompanyPool(IConfiguration configuration, ILogger<SapCompanyPool> logger)
        {
            _logger = logger;
            _maxConcurrent = configuration.GetValue<int>("SapSettings:MaxConcurrentConnections", 5);
            _timeoutSeconds = configuration.GetValue<int>("SapSettings:ConnectionTimeoutSeconds", 30);
            _semaphore = new SemaphoreSlim(_maxConcurrent, _maxConcurrent);

            _logger.LogInformation("SapCompanyPool inicializado con límite de concurrencia: {Max} conexiones simultáneas.", _maxConcurrent);
        }

        public async Task<T> ExecuteAsync<T>(SapConnectionInfo connInfo, Func<Company, Task<T>> action)
        {
            var timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            bool acquired = await _semaphore.WaitAsync(timeout);

            if (!acquired)
            {
                _logger.LogWarning("Timeout esperando slot de concurrencia en SAP ({Timeout}s). Límite actual: {Max}",
                    _timeoutSeconds, _maxConcurrent);
                throw new TimeoutException($"El servidor de SAP está ocupado procesando {_maxConcurrent} solicitudes simultáneas. Por favor reintente en unos momentos.");
            }

            Interlocked.Increment(ref _activeCount);
            Company? company = null;

            try
            {
                int maxRetries = 2;
                int attempt = 0;
                int connectResult = -1;

                while (attempt <= maxRetries && connectResult != 0)
                {
                    attempt++;
                    company = CreateCompanyInstance(connInfo);
                    connectResult = company.Connect();

                    if (connectResult != 0)
                    {
                        string errDesc = company.GetLastErrorDescription();
                        connInfo.RegistrarIntentoConexion(connectResult, errDesc);

                        if (connInfo.EsErrorRecuperable(connectResult) && attempt <= maxRetries)
                        {
                            int waitMs = connInfo.CalcularTiempoEsperaMs();
                            _logger.LogWarning("Error transitorio de concurrencia en SAP ({Code}: {Desc}). Reintentando slot en {Ms}ms...",
                                connectResult, errDesc, waitMs);

                            if (company != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                Marshal.ReleaseComObject(company);
                                company = null;
                            }

                            await Task.Delay(waitMs);
                            continue;
                        }

                        throw new Exception($"Fallo de conexión en SAP DI API ({connectResult}): {errDesc}");
                    }
                }

                connInfo.ReiniciarContadores();
                return await action(company!);
            }
            finally
            {
                if (company != null)
                {
                    try
                    {
                        if (company.Connected)
                        {
                            company.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error al desconectar Company: {Message}", ex.Message);
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Marshal.ReleaseComObject(company);
                    }
                }

                Interlocked.Decrement(ref _activeCount);
                _semaphore.Release();
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

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
