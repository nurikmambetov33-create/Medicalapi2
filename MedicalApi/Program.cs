var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // По умолчанию ASP.NET сам отклоняет пустые строковые поля с английским текстом ошибки.
    // Отключаем это, чтобы проверки в контроллерах возвращали понятный текст на русском.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
