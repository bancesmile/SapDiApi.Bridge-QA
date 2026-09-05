using System.ComponentModel.DataAnnotations;

namespace SapDiApi.Bridge.Models.Test
{
    public class EchoRequestDto
    {
        [Required(ErrorMessage = "El mensaje es obligatorio.")]
        public string Message { get; set; } = string.Empty;

        public Dictionary<string, object>? CustomAttributes { get; set; }
    }
}
