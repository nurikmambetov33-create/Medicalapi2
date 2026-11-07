using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MedicalApi.Models;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    // Публичный список, чтобы контроллер справок мог проверить пациента. Временно, пока нет базы данных.
    // Тестовые данные: после запуска в картотеке сразу есть пациенты.
    public static List<Patient> Patients = new List<Patient>
    {
        new Patient { Id = 1, Name = "Иванов Иван Иванович", BirthDate = new DateTime(1985, 3, 12), Iin = "850312300123", Phone = "+77011234567", Address = "ул. Абая, 10, кв. 5", UchastokNumber = 3, HasInsurance = true },
        new Patient { Id = 2, Name = "Петрова Анна Сергеевна", BirthDate = new DateTime(1992, 7, 25), Iin = "920725400456", Phone = "+77029876543", Address = "пр. Достык, 45, кв. 12", UchastokNumber = 7, HasInsurance = true },
        new Patient { Id = 3, Name = "Сидоров Тимур Ерланович", BirthDate = new DateTime(2015, 11, 2), Iin = "151102500789", Phone = "+77075554433", Address = "мкр. Самал-2, 8, кв. 33", UchastokNumber = 3, HasInsurance = false },
    };

    // Следующий свободный Id. Только растёт, поэтому Id удалённых записей не переиспользуются.
    private static int _nextId = Patients.Max(p => p.Id) + 1;

    [HttpGet]
    [ProducesResponseType<List<Patient>>(StatusCodes.Status200OK)]
    public List<Patient> GetAll()
    {
        return Patients;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<Patient>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Patient> GetById(int id)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }
        return patient;
    }

    // Справки пациента для его карточки
    [HttpGet("{id:int}/spravki")]
    [ProducesResponseType<List<Spravka>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<List<Spravka>> GetSpravki(int id)
    {
        if (!Patients.Any(p => p.Id == id))
        {
            return NotFound();
        }
        return SpravkiController.Spravki.Where(s => s.PatientId == id).ToList();
    }

    // Записи пациента на приём для его карточки
    [HttpGet("{id:int}/appointments")]
    [ProducesResponseType<List<Appointment>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<List<Appointment>> GetAppointments(int id)
    {
        if (!Patients.Any(p => p.Id == id))
        {
            return NotFound();
        }
        return AppointmentsController.Appointments
            .Where(a => a.PatientId == id)
            .OrderBy(a => a.StartTime)
            .ToList();
    }

    [HttpPost]
    [ProducesResponseType<Patient>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    public ActionResult<Patient> Create(Patient patient)
    {
        string? error = Validate(patient, 0);
        if (error != null)
        {
            return BadRequest(error);
        }

        patient.Id = _nextId++;
        Patients.Add(patient);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<Patient>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Patient> Update(int id, Patient patient)
    {
        Patient? existing = Patients.FirstOrDefault(p => p.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        string? error = Validate(patient, id);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.Name = patient.Name;
        existing.BirthDate = patient.BirthDate;
        existing.Iin = patient.Iin;
        existing.Phone = patient.Phone;
        existing.Address = patient.Address;
        existing.UchastokNumber = patient.UchastokNumber;
        existing.HasInsurance = patient.HasInsurance;
        return Ok(existing);
    }

    // Пациента удаляем вместе с его справками и записями на приём:
    // без пациента они теряют смысл, а ссылки на несуществующего пациента ломали бы фронтенд.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        Patient? patient = Patients.FirstOrDefault(p => p.Id == id);
        if (patient == null)
        {
            return NotFound();
        }

        SpravkiController.Spravki.RemoveAll(s => s.PatientId == id);
        AppointmentsController.Appointments.RemoveAll(a => a.PatientId == id);
        Patients.Remove(patient);
        return NoContent();
    }

    // Проверка данных для POST и PUT. Возвращает текст ошибки или null, если всё верно.
    // currentId: Id редактируемого пациента (0 при создании), чтобы не считать его дубликатом самого себя.
    private static string? Validate(Patient patient, int currentId)
    {
        if (string.IsNullOrWhiteSpace(patient.Name))
        {
            return "Укажите ФИО пациента";
        }
        patient.Name = patient.Name.Trim();
        if (patient.Name.Length < 2 || patient.Name.Length > 100)
        {
            return "ФИО пациента должно быть от 2 до 100 символов";
        }

        if (patient.BirthDate == default)
        {
            return "Укажите дату рождения в формате ГГГГ-ММ-ДД, например 2000-05-15";
        }
        if (patient.BirthDate.Date > DateTime.Today)
        {
            return "Дата рождения не может быть в будущем";
        }
        if (patient.BirthDate.Year < 1900)
        {
            return "Дата рождения не может быть раньше 1900 года";
        }

        patient.Iin = (patient.Iin ?? "").Trim();
        if (!Regex.IsMatch(patient.Iin, "^[0-9]{12}$"))
        {
            return "ИИН должен состоять ровно из 12 цифр";
        }
        if (Patients.Any(p => p.Id != currentId && p.Iin == patient.Iin))
        {
            return "Пациент с таким ИИН уже есть в картотеке";
        }

        patient.Phone = (patient.Phone ?? "").Trim();
        if (!Regex.IsMatch(patient.Phone, @"^\+7[0-9]{10}$"))
        {
            return "Телефон должен быть в формате +7XXXXXXXXXX, например +77011234567";
        }

        if (string.IsNullOrWhiteSpace(patient.Address))
        {
            return "Укажите адрес пациента";
        }
        patient.Address = patient.Address.Trim();
        if (patient.Address.Length > 200)
        {
            return "Адрес должен быть не длиннее 200 символов";
        }

        if (patient.UchastokNumber < 1 || patient.UchastokNumber > 50)
        {
            return "Номер участка должен быть от 1 до 50";
        }

        return null;
    }
}
