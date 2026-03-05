using Itm.Booking.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);




// --- 1. ZONA DE SERVICIOS 
// SERVICIOS: Aquí le decimos a .NET qué capacidades tendrá nuestra API.
builder.Services.AddEndpointsApiExplorer(); // Permite que Swagger analice los endpoints
builder.Services.AddSwaggerGen();           // Genera la documentación visual


// Registro de HttpClients con Polly para resiliencia
builder.Services.AddHttpClient("EventClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5100/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("DiscountClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5006/");
    client.Timeout = TimeSpan.FromSeconds(5);
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
    // 1. VALIDACIÓN DE ENTRADA
    try
    {
        // 2. LECTURA EN PARALELO: Obtener detalles del evento y descuento al mismo tiempo
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
    // SAGA ES UN PATRÓN DE ORQUESTACIÓN DE TRANSACCIONES DISTRIBUIDAS.
    // 3. ACCIÓN: RESERVAR SILLAS (Inicio de SAGA)
    var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve", 
        new { 
            EventId = request.EventId, Quantity = request.Tickets });

    if (!reserveResponse.IsSuccessStatusCode)
        return Results.BadRequest("No hay sillas suficientes o el evento no existe.");

    try
    {   // 4. SIMULACIÓN DE PAGO
        bool paymentSuccess = new Random().Next(1, 10) > 5;
        if (!paymentSuccess) throw new Exception("Fondos insuficientes en la tarjeta de crédito.");
        // 5. RESPUESTA AL CLIENTE
        return Results.Ok(new { Status = "Éxito",total = precioFinal, Message = "¡Disfruta el concierto ITM!" });
    }
    catch (Exception ex)
    {
        // 6. COMPENSACIÓN (En caso de error, revertimos la reserva) 
        Console.WriteLine($"[SAGA] Error en pago: {ex.Message}. Liberando sillas...");

    await eventClient.PostAsJsonAsync("/api/events/release",
        new { EventId = request.EventId, Quantity = request.Tickets });
        // 7. RESPUESTA AL CLIENTE
        return Results.Problem("Tu pago fue rechazado. No te preocupes, no te cobramos y tus sillas fueron liberadas.");
}
});

app.Run();
