using System.Collections.Generic;

namespace Colportor.Api.Models;

/// <summary>
/// Estatísticas de colportores
/// </summary>
public class ColportorStats
{
    public int TotalColportors { get; set; }
    public int ActiveColportors { get; set; }
    public int MaleColportors { get; set; }
    public int FemaleColportors { get; set; }
    public Dictionary<string, int> ColportorsByRegion { get; set; } = new();
    public Dictionary<string, int> ColportorsByGender { get; set; } = new();
}
