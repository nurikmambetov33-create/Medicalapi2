namespace MedicalApi.Models;

public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime BirthDate { get; set; }

    // ИИН: 12 цифр
    public string Iin { get; set; } = "";

    // Телефон в формате +7XXXXXXXXXX
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";

    // Номер участка поликлиники
    public int UchastokNumber { get; set; }

    // Есть ли у пациента полис ОСМС (обязательное медицинское страхование)
    public bool HasInsurance { get; set; }

    // Возраст на сегодня. Это метод, а не свойство, поэтому в JSON он не попадает.
    public int GetAge()
    {
        DateTime today = DateTime.Today;
        int age = today.Year - BirthDate.Year;
        if (BirthDate.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
