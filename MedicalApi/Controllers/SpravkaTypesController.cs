using Microsoft.AspNetCore.Mvc;
using MedicalApi.Models;

[ApiController]
[Route("api/[controller]")]
public class SpravkaTypesController : ControllerBase
{
    // Каталог заполнен сразу, поэтому не пропадает после перезапуска сервера
    public static List<SpravkaType> Types = new List<SpravkaType>
    {
        new SpravkaType { Id = 1, Name = "Справка 083", Price = 100, ValidDays = 365, NeedsDoctorExam = true, ForChildrenOnly = false },
        new SpravkaType { Id = 2, Name = "Санитарная книжка", Price = 750, ValidDays = 365, NeedsDoctorExam = true, ForChildrenOnly = false },
        new SpravkaType { Id = 3, Name = "Справка в бассейн", Price = 300, ValidDays = 90, NeedsDoctorExam = true, ForChildrenOnly = false },
        new SpravkaType { Id = 4, Name = "Справка 086 для поступления", Price = 1500, ValidDays = 180, NeedsDoctorExam = true, ForChildrenOnly = false },
        new SpravkaType { Id = 5, Name = "Справка в детский лагерь", Price = 500, ValidDays = 3, NeedsDoctorExam = true, ForChildrenOnly = true },
        new SpravkaType { Id = 6, Name = "Справка об отсутствии контакта с инфекционными больными", Price = 200, ValidDays = 3, NeedsDoctorExam = false, ForChildrenOnly = false },
    };

    // Следующий свободный Id. Только растёт, поэтому Id удалённых записей не переиспользуются.
    private static int _nextId = Types.Max(t => t.Id) + 1;

    [HttpGet]
    [ProducesResponseType<List<SpravkaType>>(StatusCodes.Status200OK)]
    public List<SpravkaType> GetAll()
    {
        return Types;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SpravkaType> GetById(int id)
    {
        SpravkaType? type = Types.FirstOrDefault(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }
        return type;
    }

    [HttpPost]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status201Created)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    public ActionResult<SpravkaType> Create(SpravkaType type)
    {
        string? error = Validate(type, 0);
        if (error != null)
        {
            return BadRequest(error);
        }

        type.Id = _nextId++;
        Types.Add(type);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<SpravkaType>(StatusCodes.Status200OK)]
    [ProducesResponseType<string>(StatusCodes.Status400BadRequest, "text/plain")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SpravkaType> Update(int id, SpravkaType type)
    {
        SpravkaType? existing = Types.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return NotFound();
        }

        string? error = Validate(type, id);
        if (error != null)
        {
            return BadRequest(error);
        }

        existing.Name = type.Name;
        existing.Price = type.Price;
        existing.ValidDays = type.ValidDays;
        existing.NeedsDoctorExam = type.NeedsDoctorExam;
        existing.ForChildrenOnly = type.ForChildrenOnly;
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Delete(int id)
    {
        SpravkaType? type = Types.FirstOrDefault(t => t.Id == id);
        if (type == null)
        {
            return NotFound();
        }

        // Уже выписанные справки хранят свои название и цену, поэтому их удаление вида не затрагивает
        Types.Remove(type);
        return NoContent();
    }

    // Проверка данных для POST и PUT. Возвращает текст ошибки или null, если всё верно.
    // currentId: Id редактируемой записи (0 при создании), чтобы не считать её дубликатом самой себя.
    private static string? Validate(SpravkaType type, int currentId)
    {
        if (string.IsNullOrWhiteSpace(type.Name))
        {
            return "Укажите название вида справки";
        }
        type.Name = type.Name.Trim();
        if (type.Name.Length > 100)
        {
            return "Название вида справки должно быть не длиннее 100 символов";
        }
        if (Types.Any(t => t.Id != currentId && string.Equals(t.Name, type.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Вид справки «{type.Name}» уже есть в каталоге";
        }
        if (type.Price <= 0)
        {
            return "Цена должна быть больше 0";
        }
        if (type.Price > 1_000_000)
        {
            return "Цена должна быть не больше 1 000 000 тенге";
        }
        if (type.ValidDays < 1 || type.ValidDays > 3650)
        {
            return "Срок действия справки должен быть от 1 до 3650 дней";
        }
        return null;
    }
}
