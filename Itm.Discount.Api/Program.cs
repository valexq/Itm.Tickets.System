using Itm.Discount.Api.Dtos;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;



var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

//2. Registramos la autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidIssuer = jwtSettings["Issuer"],
          ValidateAudience = true,
          ValidAudience = jwtSettings["Audience"],
          ValidateLifetime = true, // Valida que el token no haya expirado
          ValidateIssuerSigningKey = true, // Valida la firma del token
          IssuerSigningKey = new SymmetricSecurityKey(secretKey) // Clave secreta para validar la firma
      };
  });


//3. Agregamos autorización para proteger los endpoints
builder.Services.AddAuthorization();


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

