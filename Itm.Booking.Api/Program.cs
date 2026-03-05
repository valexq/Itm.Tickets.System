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



app.MapPost("/api/bookings", async (BookingRequestDto request, IHttpClientFactory factory) =>
{
    EventDto? evento = null;
    DiscountDto? descuento = null;
    decimal precioFinal = 0;

    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    try
    {
        // 1. LECTURA EN PARALELO (Clase 2)
        var eventTask = eventClient.GetFromJsonAsync<EventDto>($"/api/events/{request.EventId}");
        var discountTask = discountClient.GetFromJsonAsync<DiscountDto>($"/api/discounts/{request.DiscountCode}");
        await Task.WhenAll(eventTask, discountTask);
        evento = await eventTask;
        descuento = await discountTask;
        var total = evento!.PrecioBase * request.Tickets;
        precioFinal = total - (total * descuento!.Porcentaje);
    }
    catch
    {
        return Results.BadRequest("El evento o el codigo de descuento no existe");
    }
    

    // ¿Qué pasa si el código de descuento no existe y da 404? Deben manejarlo.
    // (Pista: Pueden usar try/catch aquí o validar la respuesta).
    // 2. ACCIÓN: RESERVAR SILLAS (Inicio de SAGA)
    var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve", 
        new { 
            EventId = request.EventId, Quantity = request.Tickets });

    if (!reserveResponse.IsSuccessStatusCode)
        return Results.BadRequest("No hay sillas suficientes o el evento no existe.");

    try
    {        // 3. SIMULACIÓN DE PAGO (Punto Crítico)
        bool paymentSuccess = new Random().Next(1, 10) > 5;
        if (!paymentSuccess) throw new Exception("Fondos insuficientes en la tarjeta de crédito.");

        return Results.Ok(new { Status = "Éxito",total = precioFinal, Message = "¡Disfruta el concierto ITM!" });
    }
    catch (Exception ex)
    {
    // 4. COMPENSACIÓN 
    Console.WriteLine($"[SAGA] Error en pago: {ex.Message}. Liberando sillas...");

    await eventClient.PostAsJsonAsync("/api/events/release",
        new { EventId = request.EventId, Quantity = request.Tickets });

    return Results.Problem("Tu pago fue rechazado. No te preocupes, no te cobramos y tus sillas fueron liberadas.");
}
});


app.Run();
