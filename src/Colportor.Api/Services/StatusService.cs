namespace Colportor.Api.Services;
public static class StatusService
{
    public static (string status, DateTime? dueDate) ComputeStatus(DateTime? lastVisit)
    {
        if(lastVisit is null) return ("VENCIDO", null);
        var due = lastVisit.Value.AddMonths(6).Date;
        var today = DateTime.UtcNow.Date;
        if (today < due.AddDays(-15)) return ("EM DIA", due);
        if (today >= due.AddDays(-15) && today < due) return ("AVISO", due);
        return ("VENCIDO", due);
    }
}
