using Itm.Discount.Api.Dtos;




var builder = WebApplication.CreateBuilder(args);

// --- 1. ZONA DE SERVICIOS 
// Aquí le decimos a .NET qué capacidades tendrá nuestra API.
builder.Services.AddEndpointsApiExplorer(); // Permite que Swagger analice los endpoints
builder.Services.AddSwaggerGen();           // Genera la documentación visual

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var DiscountsDb = new List<DiscountDto>
{
    new("ITM50", 0.5m),
    new("ITMCool", 0.01m) 
};


// --- 3. ZONA DE ENDPOINTS
app.MapGet("/api/discounts/{code}", (string code) =>
{
    var item = DiscountsDb.FirstOrDefault(p => p.Codigo == code);

    //  PATRÓN DE RESPUESTA HTTP:
    // Si existe (is not null) -> 200 OK con el dato.
    // Si no existe -> 404 NotFound.
    return item is not null ? Results.Ok(item) : Results.NotFound();
});
//.RequireAuthorization(); // Protegemos este endpoint, solo usuarios autenticados pueden acceder
app.Run();