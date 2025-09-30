using System;

namespace Colportor.Api.Models
{
    public class NotificationLog
    {
        public int Id { get; set; }

        public int ColportorId { get; set; }

        /// <summary>AVISO_15 ou VENCIMENTO</summary>
        public string Type { get; set; } = default!;

        /// <summary>UTC</summary>
        public DateTime SentAt { get; set; }

        public string Email { get; set; } = default!;
    }
}
