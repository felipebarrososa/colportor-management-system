using System;

namespace Colportor.Api.Models
{
    public class MissionContact
    {
        public int Id { get; set; }

        // Vínculos
        public int RegionId { get; set; }
        public Region? Region { get; set; }

        public int? LeaderId { get; set; }
        public User? Leader { get; set; }

        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public int? CreatedByColportorId { get; set; }
        public Colportor? CreatedByColportor { get; set; }

        // Dados pessoais
        public string FullName { get; set; } = default!;
        public string? Gender { get; set; } // Masculino, Feminino, Outro
        public DateTime? BirthDate { get; set; } // UTC
        public string? MaritalStatus { get; set; }
        public string? Nationality { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Profession { get; set; }
        public bool SpeaksOtherLanguages { get; set; }
        public string? OtherLanguages { get; set; }
        public string? FluencyLevel { get; set; } // Básico, Avançado
        public string? Church { get; set; }
        public string? ConversionTime { get; set; }

        // Chamado/Disponibilidade
        public string? MissionsDedicationPlan { get; set; }
        public bool HasPassport { get; set; }
        public DateTime? AvailableDate { get; set; } // UTC

        // Pipeline/Contato
        public string Status { get; set; } = "Novo";
        public string? Notes { get; set; }
        public DateTime? LastContactedAt { get; set; } // UTC
        public DateTime? NextFollowUpAt { get; set; } // UTC

        // Auditoria
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}



