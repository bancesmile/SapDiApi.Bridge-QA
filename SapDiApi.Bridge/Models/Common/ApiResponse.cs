namespace SapDiApi.Bridge.Models.Common
{
    /// <summary>
    /// Respuesta estandarizada para todos los endpoints de la API.
    /// </summary>
    /// <typeparam name="T">Tipo de dato contenido en el payload.</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string message = "Operación exitosa")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                TimestampUtc = DateTime.UtcNow
            };
        }

        public static ApiResponse<T> Fail(string message, T? data = default)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Respuesta estándar de error.
    /// </summary>
    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Error { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int StatusCode { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
