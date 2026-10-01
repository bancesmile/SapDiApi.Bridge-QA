using System.Runtime.InteropServices;

namespace SapDiApi.Bridge.Infrastructure.Common
{
    /// <summary>
    /// Utilidades para gestión segura y liberación determinística de memoria COM (SAPbobsCOM).
    /// Evita fugas de punteros RCW y previene el error 0xc0000374 (Heap Corruption) y 0xc0000005 (Access Violation).
    /// </summary>
    public static class ComHelper
    {
        /// <summary>
        /// Libera de forma definitiva e inmediata un objeto COM nativo en Windows.
        /// </summary>
        public static void Release(object? comObject)
        {
            if (comObject != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Marshal.IsComObject(comObject))
            {
                try
                {
                    Marshal.FinalReleaseComObject(comObject);
                }
                catch
                {
                    // Ignorar excepciones al liberar COM en cierre de hilos
                }
            }
        }
    }
}
