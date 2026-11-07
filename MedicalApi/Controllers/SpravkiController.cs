using Microsoft.AspNetCore.Mvc;
using MedicalApi.Models;

[ApiController]
[Route("api/[controller]")]
public class SpravkiController : ControllerBase
{
    // Публичный список, чтобы контроллер пациентов мог показать справки пациента. Временно, пока нет базы данных.
    // Тестовые данные: после запуска сразу есть несколько справок.
    public static List<Spravka> Spravki = new List<Spravka>
    {
        new Spravka { Id = 1, PatientId = 1, SpravkaTypeId = 1, Name = "Справка 083", Price = 100 },
        new Spravka { Id = 2, PatientId = 2, SpravkaTypeId = 2, Name = "Санитарная книжка", Price = 750 },
        new Spravka { Id = 3, PatientId = 3, SpravkaTypeId = 5, Name = "Справка в детский лагерь", Price = 500 },
    };

    // Баг из задания 8: раньше было spravka.Id = _spravki.Count + 1.
    // После удаления записи Count уменьшался, и новая справка получала Id уже существующей.
    // Теперь счётчик только растёт, поэтому Id никогда не повторяются.
    private static int _nextId = Spravki.Max(s => s.Id) + 1;

    // GET /api/spravki — все справки, GET /api/spravki?patientId=3 — только справки пациента 3
    [HttpGet]
    [ProducesResponseType<List<Spravka>>(StatusCodes.Status200OK)]
    public List<Spravka> GetAll(int? patientId)
    {
        if (patientId == null)
        {
            return Spravki;
        }
        return Spravki.Where(s => s.PatientId == patientId).ToList();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<Spravka>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Spravka> GetById(int id)
    {
        Spravka? spravka = Spravki.FirstOrDefault(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }
        return spravka;
    }

    // Статистика для главврача: сколько справок выписано и на какую сумму
    [HttpGet("stats")]
    [ProducesResponseType<SpravkiStats>(StatusCodes.Status200OK)]
    public SpravkiStats GetStats()
    {
        return new SpravkiStats
        {
            TotalCount = Spravki.Count,
            TotalSum = Spravki.Sum(s => s.Price),
            ByType = Spravki
                .GroupBy(s => s.Name)
                .Select(g => new SpravkiStatsByType { Name = g.Key, Count = g.Count(), Sum = g.Sum(s => s.Price) })
                .OrderByDescending(x => x.Sum)
                .ToList(),
        };
    }

    [HttpPost]
    [ProducesResponseType<Spravka>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    public ActionResult<Spravka> Create(Spravka spravka)
    {
        string? error = ValidateAndFill(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }

        spravka.Id = _nextId++;
        Spravki.Add(spravka);
        return CreatedAtAction(nameof(GetById), new { id = spravka.Id }, spravka);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<Spravka>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Spravka> Update(int id, Spravka spravka)
    {
        Spravka? existing = Spravki.FirstOrDefault(s => s.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        string? error = ValidateAndFill(spravka);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.PatientId = spravka.PatientId;
        existing.SpravkaTypeId = spravka.SpravkaTypeId;
        existing.Name = spravka.Name;
        existing.Price = spravka.Price;
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        Spravka? spravka = Spravki.FirstOrDefault(s => s.Id == id);
        if (spravka == null)
        {
            return NotFound();
        }

        Spravki.Remove(spravka);
        return NoContent();
    }

    // Проверка для POST и PUT. Название и цену берём из каталога, а не из тела запроса.
    // Возвращает текст ошибки или null, если всё верно.
    private static string? ValidateAndFill(Spravka spravka)
    {
        Patient? patient = PatientsController.Patients.FirstOrDefault(p => p.Id == spravka.PatientId);
        if (patient == null)
        {
            return $"Пациент с номером {spravka.PatientId} не найден в картотеке. Укажите patientId существующего пациента";
        }

        SpravkaType? type = SpravkaTypesController.Types.FirstOrDefault(t => t.Id == spravka.SpravkaTypeId);
        if (type == null)
        {
            return $"Вид справки с номером {spravka.SpravkaTypeId} не найден в каталоге. Список видов: GET /api/SpravkaTypes";
        }

        if (type.ForChildrenOnly && patient.GetAge() >= 18)
        {
            return $"«{type.Name}» выдаётся только детям до 18 лет, а пациенту уже исполнилось 18. Выберите другой вид справки";
        }

        spravka.Name = type.Name;
        spravka.Price = type.Price;
        return null;
    }
}
