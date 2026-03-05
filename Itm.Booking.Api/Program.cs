using Itm.Booking.Api.Dtos;




var builder = WebApplication.CreateBuilder(args);

// --- 1. ZONA DE SERVICIOS 
// Aquí le decimos a .NET qué capacidades tendrá nuestra API.
builder.Services.AddEndpointsApiExplorer(); // Permite que Swagger analice los endpoints
builder.Services.AddSwaggerGen();           // Genera la documentación visual

builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5100/");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("DiscountClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5006/");
})
.AddStandardResilienceHandler();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


