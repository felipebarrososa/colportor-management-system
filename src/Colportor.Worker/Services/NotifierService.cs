using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Colportor.Api.Data;
using Colportor.Api.Services;

namespace Colportor.Worker.Services
{
    public class NotifierService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        public NotifierService(IServiceProvider sp) { _sp = sp; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var email = scope.ServiceProvider.GetRequiredService<EmailService>();
                    var today = DateTime.UtcNow.Date;

                    var list = await db.Colportors.AsNoTracking().ToListAsync(stoppingToken);
                    foreach (var c in list)
                    {
                        var (status, due) = StatusService.ComputeStatus(c.LastVisitDate);
                        if (due is null) continue;

                        if (today == due.Value.AddDays(-15))
                        {
                            var exists = db.NotificationLogs.Any(n => n.ColportorId == c.Id && n.Type == "PRE15" && n.SentAt.Date == today);
                            var user = db.Users.FirstOrDefault(u => u.ColportorId == c.Id);
                            if (!exists && user != null)
                            {
                                await email.SendAsync(user.Email, "Aviso: visita ao PAC em 15 dias",
                                    $"<p>Olá {c.FullName},</p><p>Sua visita ao PAC vence em 15 dias ({due:dd/MM/yyyy}).</p>");
                                db.NotificationLogs.Add(new Colportor.Api.Models.NotificationLog
                                {
                                    ColportorId = c.Id,
                                    Type = "PRE15",
                                    SentAt = DateTime.UtcNow,
                                    Email = user.Email
                                });
                            }
                        }

                        if (today == due.Value)
                        {
                            var exists = db.NotificationLogs.Any(n => n.ColportorId == c.Id && n.Type == "DUE" && n.SentAt.Date == today);
                            var user = db.Users.FirstOrDefault(u => u.ColportorId == c.Id);
                            if (!exists && user != null)
                            {
                                await email.SendAsync(user.Email, "Vencimento hoje: visita ao PAC",
                                    $"<p>Olá {c.FullName},</p><p>Sua visita ao PAC vence hoje ({due:dd/MM/yyyy}).</p>");
                                db.NotificationLogs.Add(new Colportor.Api.Models.NotificationLog
                                {
                                    ColportorId = c.Id,
                                    Type = "DUE",
                                    SentAt = DateTime.UtcNow,
                                    Email = user.Email
                                });
                            }
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch { /* log em produção */ }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
