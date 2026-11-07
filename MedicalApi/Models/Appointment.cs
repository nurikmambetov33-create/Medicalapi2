namespace MedicalApi.Models;

// Запись на приём к врачу
public class Appointment
{
    public int Id { get; set; }

    // Пациент из картотеки
    public int PatientId { get; set; }

    public string DoctorName { get; set; } = "";
    public string Specialty { get; set; } = "";

    // Начало приёма. Приём длится 15 минут.
    public DateTime StartTime { get; set; }

    public int Room { get; set; }

    // Жалоба пациента, необязательное поле
    public string Complaint { get; set; } = "";

    // Пациент пришёл и приём состоялся
    public bool IsCompleted { get; set; }
}
