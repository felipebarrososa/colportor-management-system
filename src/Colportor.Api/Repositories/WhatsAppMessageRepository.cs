using Colportor.Api.Data;
using Colportor.Api.Models;
using Colportor.Api.Repositories.Interfaces;

namespace Colportor.Api.Repositories
{
    public class WhatsAppMessageRepository : BaseRepository<WhatsAppMessage>, IWhatsAppMessageRepository
    {
        public WhatsAppMessageRepository(AppDbContext context) : base(context)
        {
        }
    }
}
