namespace MedicalApi.Models;

public class SpravkaType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    // Сколько дней действует справка
    public int ValidDays { get; set; }

    // Нужен ли осмотр врача перед выдачей
    public bool NeedsDoctorExam { get; set; }

    // Только для детей (до 18 лет)
    public bool ForChildrenOnly { get; set; }
}
