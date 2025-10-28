using Colportor.Api.Data;
using Colportor.Api.Models;
using Colportor.Api.Repositories.Interfaces;

namespace Colportor.Api.Repositories
{
    public class WhatsAppConnectionRepository : BaseRepository<WhatsAppConnection>, IWhatsAppConnectionRepository
    {
        public WhatsAppConnectionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
