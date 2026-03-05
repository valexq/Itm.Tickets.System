using System.Text;
using Itm.Event.Api.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); // Permite que Swagger analice los endpoints
builder.Services.AddSwaggerGen();           // Genera la documentación visual

//var jwtSettings = builder.Configuration.GetSection("JwtSettings");
//var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

//2. Registramos la autenticación JWT
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
  //  {
    //    options.TokenValidationParameters = new TokenValidationParameters
      //  {
        //    ValidateIssuer = true,
          //  ValidIssuer = jwtSettings["Issuer"],
            //ValidateAudience = true,
            //ValidAudience = jwtSettings["Audience"],
            //ValidateLifetime = true, // Valida que el token no haya expirado
            //ValidateIssuerSigningKey = true, // Valida la firma del token
            //IssuerSigningKey = new SymmetricSecurityKey(secretKey) // Clave secreta para validar la firma
       // };
    //});


//3. Agregamos autorización (Opcional, pero recomendado para proteger los endpoints)
//builder.Services.AddAuthorization();

var app = builder.Build();



// --- 2. ZONA DE MIDDLEWARE 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Activa el JSON de Swagger
    app.UseSwaggerUI(); // Activa la página web azul bonita
}

// Middleware de seguridad (JWT)

//app.UseAuthentication(); // Verifica el token JWT en cada petición
//app.UseAuthorization();    // Verifica los permisos del usuario

var EventDb = new List<EventDto>
{
    new(1, "Concierto ITM", 50000, 100),
    new(2, "Demostracion musical", 25000, 100) 
};

// --- 3. ZONA DE ENDPOINTS
app.MapGet("/api/events/{id}", (int id) =>
{
    var item = EventDb.FirstOrDefault(p => p.EventId == id);

    //  PATRÓN DE RESPUESTA HTTP:
    // Si existe (is not null) -> 200 OK con el dato.
    // Si no existe -> 404 NotFound.
    return item is not null ? Results.Ok(item) : Results.NotFound();
});
//.RequireAuthorization(); // Protegemos este endpoint, solo usuarios autenticados pueden acceder


app.MapPost("/api/events/reserve", (ReserveRequestDto request) =>
{
    // 1. Buscamos las sillas
    var silla = EventDb.FirstOrDefault(p => p.EventId == request.EventId);

    // 2. Validamos que existen sillas en el evento 

    if (silla is null)
    {
        return Results.NotFound(new { Error = "Evento no existe" });
    }
    if (silla.SillasDisponibles < request.Quantity)
    {
        // 400 Bad Request: No hay sillas disponibles para reservar la cantidad solicitada
        return Results.BadRequest(new{ Error = "No hay sillas disponibles"});
    }
    var index = EventDb.IndexOf(silla);
    EventDb[index] = silla with { SillasDisponibles = silla.SillasDisponibles - request.Quantity };

    return Results.Ok(new { Message = $"Reserva exitosa para {request.Quantity} sillas en el evento {silla.nombre}" });
});

app.MapPost("/api/events/release", (ReserveRequestDto request) =>
{
    // 1. Buscamos las sillas
    var silla = EventDb.FirstOrDefault(p => p.EventId == request.EventId);

    // 2. Validamos que existen sillas en el evento 

    if (silla is null)
    {
        return Results.NotFound(new { Error = "Evento no existe" });
    }
 
    var index = EventDb.IndexOf(silla);
    EventDb[index] = silla with { SillasDisponibles = silla.SillasDisponibles + request.Quantity };

    return Results.Ok(new { Message = $"Release exitoso para {request.Quantity} sillas en el evento {silla.nombre}" });
});
app.Run();
