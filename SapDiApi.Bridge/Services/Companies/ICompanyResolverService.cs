using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Companies;

namespace SapDiApi.Bridge.Services.Companies
{
    /// <summary>
    /// Servicio de resolución de empresas con caché en memoria RAM ultrarrápida y catálogo SQLite local.
    /// </summary>
    public interface ICompanyResolverService
    {
        /// <summary>
        /// Resuelve el nombre real de la base de datos SAP (`dbName`) a partir de un CompanyId (numérico),
        /// CompanyCode (alias) o dbName heredado.
        /// </summary>
        (bool Found, CompanyDto? Company, string SapDatabase) ResolveCompany(string? identifier);

        /// <summary>
        /// Obtiene todas las empresas registradas en el catálogo.
        /// </summary>
        IEnumerable<CompanyDto> GetAll(bool onlyActive = true);

        /// <summary>
        /// Obtiene el listado público para clientes y frontend (sin revelar nombres internos de esquemas de BD).
        /// </summary>
        IEnumerable<PublicCompanyDto> GetPublicCompanies(bool onlyActive = true);

        /// <summary>
        /// Obtiene una empresa por su CompanyId numérico.
        /// </summary>
        CompanyDto? GetById(int companyId);

        /// <summary>
        /// Obtiene una empresa por su CompanyCode alfanumérico.
        /// </summary>
        CompanyDto? GetByCode(string companyCode);

        /// <summary>
        /// Recarga la caché en memoria RAM desde el archivo local SQLite.
        /// </summary>
        Task ReloadCacheAsync();

        /// <summary>
        /// Sincroniza el catálogo local de empresas consultando la tabla maestra `SBOCOMMON.SRGC` en SAP.
        /// </summary>
        Task<(int TotalSynced, int NewAdded, int Updated)> SyncFromSapAsync(UserSession? session = null);

        /// <summary>
        /// Registra o actualiza una empresa manualmente en el catálogo SQLite y recarga la memoria.
        /// </summary>
        Task<bool> UpsertCompanyAsync(CompanyDto company);
    }
}
