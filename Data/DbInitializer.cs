using Microsoft.EntityFrameworkCore;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GimnasioContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GimnasioContext>>();

        try
        {
            // Intentar aplicar migraciones pendientes si la base de datos está disponible
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning("No se pudo conectar o migrar la base de datos automáticamente: {Message}", ex.Message);
            return; // Si no hay conexión con MySQL, salimos pacíficamente
        }

        try
        {
            // 1. Cargar Usuarios iniciales si no existen
            if (!await context.Usuario.AnyAsync())
            {
                var usuariosSemilla = new List<Usuario>
                {
                    new Usuario
                    {
                        nombre = "Ana",
                        apellidos = "Martínez",
                        edad = 30,
                        email = "secretaria@gimnasio.com",
                        password = PasswordHelper.HashPassword("Secretaria123!"),
                        telefono = "555-100-2000",
                        tipo_usuario = "secretaria"
                    },
                    new Usuario
                    {
                        nombre = "Carlos",
                        apellidos = "Gómez",
                        edad = 28,
                        email = "cliente@gimnasio.com",
                        password = PasswordHelper.HashPassword("Cliente123!"),
                        telefono = "555-300-4000",
                        tipo_usuario = "cliente"
                    }
                };

                await context.Usuario.AddRangeAsync(usuariosSemilla);
                await context.SaveChangesAsync();
                logger.LogInformation("Usuarios de prueba creados (secretaria@gimnasio.com y cliente@gimnasio.com).");
            }

            // 2. Cargar Catálogo de 15 Bicicletas si no existen
            if (!await context.Bicicletas.AnyAsync())
            {
                var bicicletasSemilla = new List<Bicicletas>();
                for (int i = 1; i <= 15; i++)
                {
                    bicicletasSemilla.Add(new Bicicletas
                    {
                        nombre_bicicleta = $"Bicicleta #{i:D2}",
                        estado = "libre"
                    });
                }

                await context.Bicicletas.AddRangeAsync(bicicletasSemilla);
                await context.SaveChangesAsync();
                logger.LogInformation("Catálogo de 15 bicicletas cargado exitosamente.");
            }

            // 3. Cargar Horarios de clases si no existen
            if (!await context.Horarios.AnyAsync())
            {
                var horariosSemilla = new List<Horarios>
                {
                    new Horarios { hora_inicio = new TimeSpan(7, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(8, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(9, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(10, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(17, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(18, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(19, 0, 0) },
                    new Horarios { hora_inicio = new TimeSpan(20, 0, 0) }
                };

                await context.Horarios.AddRangeAsync(horariosSemilla);
                await context.SaveChangesAsync();
                logger.LogInformation("Horarios de clases cargados exitosamente.");
            }

            // 4. Cargar Membresía activa para el cliente de prueba si no existe
            var cliente = await context.Usuario.FirstOrDefaultAsync(u => u.email == "cliente@gimnasio.com");
            if (cliente != null && !await context.Membresias.AnyAsync(m => m.id_cliente == cliente.id_usuario))
            {
                var membresiaSemilla = new Membresias
                {
                    id_cliente = cliente.id_usuario,
                    fecha_inicio = DateTime.UtcNow,
                    fecha_expiracion = DateTime.UtcNow.AddMonths(1),
                    estado = "Activa"
                };

                await context.Membresias.AddAsync(membresiaSemilla);
                await context.SaveChangesAsync();
                logger.LogInformation("Membresía activa de 1 mes asignada a cliente@gimnasio.com.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocurrió un error al sembrar los datos iniciales: {Message}", ex.Message);
        }
    }
}
