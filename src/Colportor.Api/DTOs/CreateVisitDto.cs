using System.ComponentModel.DataAnnotations;

namespace Colportor.Api.DTOs
{
    /// <summary>
    /// DTO para criação de visita (check-in)
    /// </summary>
    public class CreateVisitDto
    {
        /// <summary>
        /// ID do colportor
        /// </summary>
        [Required(ErrorMessage = "ID do colportor é obrigatório")]
        public int ColportorId { get; set; }

        /// <summary>
        /// Data da visita
        /// </summary>
        [Required(ErrorMessage = "Data da visita é obrigatória")]
        public DateTime Date { get; set; }
    }
}
