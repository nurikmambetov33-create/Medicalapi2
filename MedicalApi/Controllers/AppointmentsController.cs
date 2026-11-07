using Microsoft.AspNetCore.Mvc;
using MedicalApi.Models;

// Модуль «Запись на приём к врачу»
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private const int SlotMinutes = 15;  // длительность одного приёма
    private const int OpenHour = 8;      // поликлиника открывается в 8:00
    private const int CloseHour = 20;    // последний приём заканчивается до 20:00

    // Публичный список, чтобы контроллер пациентов мог показать записи пациента и удалить их вместе с ним.
    // Тестовые данные: записи на ближайшие рабочие дни.
    public static List<Appointment> Appointments = new List<Appointment>
    {
        new Appointment { Id = 1, PatientId = 1, DoctorName = "Ахметова Гульнара Маратовна", Specialty = "Терапевт", StartTime = NextWorkday(1).AddHours(9), Room = 105, Complaint = "Кашель, температура 37,5" },
        new Appointment { Id = 2, PatientId = 3, DoctorName = "Ким Елена Викторовна", Specialty = "Педиатр", StartTime = NextWorkday(1).AddHours(10).AddMinutes(30), Room = 210, Complaint = "Осмотр перед лагерем" },
        new Appointment { Id = 3, PatientId = 2, DoctorName = "Ахметова Гульнара Маратовна", Specialty = "Терапевт", StartTime = NextWorkday(2).AddHours(14).AddMinutes(15), Room = 105 },
    };

    // Следующий свободный Id. Только растёт, поэтому Id удалённых записей не переиспользуются.
    private static int _nextId = Appointments.Max(a => a.Id) + 1;

    // GET /api/appointments — все записи, можно отфильтровать по пациенту и по врачу:
    // GET /api/appointments?patientId=1, GET /api/appointments?doctorName=Ахметова
    // Записи отсортированы по времени приёма.
    [HttpGet]
    [ProducesResponseType<List<Appointment>>(StatusCodes.Status200OK)]
    public List<Appointment> GetAll(int? patientId, string? doctorName)
    {
        IEnumerable<Appointment> result = Appointments;
        if (patientId != null)
        {
            result = result.Where(a => a.PatientId == patientId);
        }
        if (!string.IsNullOrWhiteSpace(doctorName))
        {
            result = result.Where(a => a.DoctorName.Contains(doctorName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        return result.OrderBy(a => a.StartTime).ToList();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Appointment> GetById(int id)
    {
        Appointment? appointment = Appointments.FirstOrDefault(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }
        return appointment;
    }

    [HttpPost]
    [ProducesResponseType<Appointment>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    public ActionResult<Appointment> Create(Appointment appointment)
    {
        string? error = Validate(appointment, 0);
        if (error != null)
        {
            return BadRequest(error);
        }
        if (appointment.IsCompleted)
        {
            return BadRequest("Новая запись не может быть сразу отмечена как состоявшийся приём. Уберите isCompleted или поставьте false");
        }

        appointment.Id = _nextId++;
        Appointments.Add(appointment);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<Appointment>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Appointment> Update(int id, Appointment appointment)
    {
        Appointment? existing = Appointments.FirstOrDefault(a => a.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        // Время прошедшего приёма можно оставить как есть (например, чтобы отметить isCompleted),
        // а переносить запись можно только на будущее.
        bool timeChanged = existing.StartTime != appointment.StartTime;
        string? error = Validate(appointment, id, checkFuture: timeChanged);
        if (error != null)
        {
            return BadRequest(error);
        }
        if (appointment.IsCompleted && appointment.StartTime > DateTime.Now)
        {
            return BadRequest("Нельзя отметить приём состоявшимся, пока его время ещё не наступило");
        }

        existing.PatientId = appointment.PatientId;
        existing.DoctorName = appointment.DoctorName;
        existing.Specialty = appointment.Specialty;
        existing.StartTime = appointment.StartTime;
        existing.Room = appointment.Room;
        existing.Complaint = appointment.Complaint;
        existing.IsCompleted = appointment.IsCompleted;
        return Ok(existing);
    }

    // DELETE: отмена записи на приём
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        Appointment? appointment = Appointments.FirstOrDefault(a => a.Id == id);
        if (appointment == null)
        {
            return NotFound();
        }

        Appointments.Remove(appointment);
        return NoContent();
    }

    // Проверка данных для POST и PUT. Возвращает текст ошибки или null, если всё верно.
    // currentId: Id редактируемой записи (0 при создании), чтобы запись не конфликтовала сама с собой.
    private static string? Validate(Appointment appointment, int currentId, bool checkFuture = true)
    {
        if (!PatientsController.Patients.Any(p => p.Id == appointment.PatientId))
        {
            return $"Пациент с номером {appointment.PatientId} не найден в картотеке. Укажите patientId существующего пациента";
        }

        if (string.IsNullOrWhiteSpace(appointment.DoctorName))
        {
            return "Укажите ФИО врача";
        }
        appointment.DoctorName = appointment.DoctorName.Trim();
        if (appointment.DoctorName.Length > 100)
        {
            return "ФИО врача должно быть не длиннее 100 символов";
        }

        if (string.IsNullOrWhiteSpace(appointment.Specialty))
        {
            return "Укажите специальность врача, например «Терапевт»";
        }
        appointment.Specialty = appointment.Specialty.Trim();
        if (appointment.Specialty.Length > 50)
        {
            return "Специальность должна быть не длиннее 50 символов";
        }

        if (appointment.StartTime == default)
        {
            return "Укажите время приёма в формате ГГГГ-ММ-ДДTЧЧ:ММ, например 2026-12-01T10:30";
        }
        if (checkFuture && appointment.StartTime <= DateTime.Now)
        {
            return "Записаться можно только на будущее время";
        }
        if (appointment.StartTime.DayOfWeek == DayOfWeek.Sunday)
        {
            return "В воскресенье поликлиника не работает. Выберите другой день";
        }
        TimeSpan time = appointment.StartTime.TimeOfDay;
        if (time < TimeSpan.FromHours(OpenHour) || time > TimeSpan.FromHours(CloseHour) - TimeSpan.FromMinutes(SlotMinutes))
        {
            return $"Приём идёт с {OpenHour}:00 до {CloseHour}:00. Последняя запись в {CloseHour - 1}:{60 - SlotMinutes}";
        }
        if (appointment.StartTime.Minute % SlotMinutes != 0 || appointment.StartTime.Second != 0 || appointment.StartTime.Millisecond != 0)
        {
            return $"Приём начинается только в :00, :15, :30 или :45, например 10:30";
        }

        if (appointment.Room < 1 || appointment.Room > 999)
        {
            return "Номер кабинета должен быть от 1 до 999";
        }

        appointment.Complaint = (appointment.Complaint ?? "").Trim();
        if (appointment.Complaint.Length > 500)
        {
            return "Жалоба должна быть не длиннее 500 символов";
        }

        // Один врач и один пациент не могут быть в двух местах одновременно
        Appointment? doctorBusy = Appointments.FirstOrDefault(a =>
            a.Id != currentId &&
            a.StartTime == appointment.StartTime &&
            string.Equals(a.DoctorName, appointment.DoctorName, StringComparison.OrdinalIgnoreCase));
        if (doctorBusy != null)
        {
            return $"У врача {appointment.DoctorName} на {appointment.StartTime:dd.MM.yyyy HH:mm} уже есть запись. Выберите другое время";
        }
        Appointment? patientBusy = Appointments.FirstOrDefault(a =>
            a.Id != currentId &&
            a.PatientId == appointment.PatientId &&
            a.StartTime == appointment.StartTime);
        if (patientBusy != null)
        {
            return $"Пациент уже записан на {appointment.StartTime:dd.MM.yyyy HH:mm} к врачу {patientBusy.DoctorName}. Выберите другое время";
        }

        return null;
    }

    // Ближайший рабочий день (не воскресенье) через days дней, время 00:00. Нужен для тестовых данных.
    private static DateTime NextWorkday(int days)
    {
        DateTime day = DateTime.Today.AddDays(days);
        while (day.DayOfWeek == DayOfWeek.Sunday)
        {
            day = day.AddDays(1);
        }
        return day;
    }
}
