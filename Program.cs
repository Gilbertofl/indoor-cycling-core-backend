using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Agrega servicios al contenedor.
builder.Services.AddEndpointsApiExplorer();

// Configura Swagger con soporte para el header sessionToken y comentarios XML
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gimnasio API - Control de Clases y Membresías",
        Version = "v1",
        Description = "API RESTful para la administración de gimnasio: control de clientes, membresías activas, catálogo de bicicletas de spinning, horarios y reservaciones de clases."
    });

    // Cargar comentarios XML para documentar parámetros y respuestas en Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("sessionToken", new OpenApiSecurityScheme
    {
        Description = "Clave de acceso (token) devuelta al iniciar sesión con éxito. Ingrésala aquí para autorizar tus pruebas.",
        Name = "sessionToken",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "sessionToken"
                },
                Scheme = "ApiKeyScheme",
                Name = "sessionToken",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Configura el contexto de la base de datos usando MySQL con fallback seguro
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=gimnasio;User=root;";

ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch
{
    serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
}

builder.Services.AddDbContext<GimnasioContext>(options =>
    options.UseMySql(connectionString, serverVersion));

// Agrega los controladores a la aplicación
builder.Services.AddControllers();

// Configurar CORS para permitir solicitudes desde el frontend o clientes de desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Agrega el servicio de autorización
builder.Services.AddAuthorization();

// Construye la aplicación
var app = builder.Build();

// Middleware global para captura amigable de errores inesperados
app.UseMiddleware<ExceptionMiddleware>();

// Habilita Swagger para la aplicación en cualquier entorno para facilitar pruebas
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gimnasio API v1");
    c.RoutePrefix = "swagger";
});

// Habilita el uso de CORS en la aplicacion
app.UseCors("AllowAll");

// Habilita la autorización para la aplicación
app.UseAuthorization();

// Mapea los controladores a las rutas de la aplicación
app.MapControllers();

// Redirige la raíz automáticamente a la documentación de Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Carga inicial automática de datos de prueba si la base de datos está vacía
await DbInitializer.SeedAsync(app.Services);

// Ejecuta la aplicación
app.Run();
