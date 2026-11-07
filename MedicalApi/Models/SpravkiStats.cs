namespace MedicalApi.Models;

// Статистика для главврача
public class SpravkiStats
{
    public int TotalCount { get; set; }
    public decimal TotalSum { get; set; }
    public List<SpravkiStatsByType> ByType { get; set; } = new List<SpravkiStatsByType>();
}

public class SpravkiStatsByType
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public decimal Sum { get; set; }
}
