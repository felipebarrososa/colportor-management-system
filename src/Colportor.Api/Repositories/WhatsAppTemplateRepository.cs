using Colportor.Api.Data;
using Colportor.Api.Models;
using Colportor.Api.Repositories.Interfaces;

namespace Colportor.Api.Repositories
{
    public class WhatsAppTemplateRepository : BaseRepository<WhatsAppTemplate>, IWhatsAppTemplateRepository
    {
        public WhatsAppTemplateRepository(AppDbContext context) : base(context)
        {
        }
    }
}
