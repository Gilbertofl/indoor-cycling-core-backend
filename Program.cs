using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Agrega servicios al contenedor.
// Aprender más sobre cómo configurar Swagger/OpenAPI en https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer(); // Habilita la exploración de endpoints para la API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gimnasio API", Version = "v1" });
}
); // Agrega los servicios de Swagger para generar la documentación de la API

// Configura el contexto de la base de datos usando MySQL
builder.Services.AddDbContext<GimnasioContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), 
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Agrega los controladores a la aplicación
builder.Services.AddControllers();

// Configurar CORS para permitir solicitudes desde el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Agrega el servicio de autorización
builder.Services.AddAuthorization();

// Construye la aplicación
var app = builder.Build();

// Habilita Swagger para la aplicación
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gimnasio API v1"));

// Habilita el uso de CORS en la aplicacion
app.UseCors("AllowReactApp");

// Habilita la autorización para la aplicación
app.UseAuthorization();

// Mapea los controladores a las rutas de la aplicación
app.MapControllers();

// Ejecuta la aplicación
app.Run();
