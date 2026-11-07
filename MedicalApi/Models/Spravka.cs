namespace MedicalApi.Models;

public class Spravka
{
    public int Id { get; set; }

    // Номер пациента из картотеки, которому выписана справка
    public int PatientId { get; set; }

    // Вид справки из каталога
    public int SpravkaTypeId { get; set; }

    // Название и цена заполняются сервером из каталога видов справок
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
