using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class GimnasioContextFactory : IDesignTimeDbContextFactory<GimnasioContext>
{
    public GimnasioContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=gimnasio;User=root;";

        var optionsBuilder = new DbContextOptionsBuilder<GimnasioContext>();
        // Usa una versión fija de MySQL para herramientas de diseño (migraciones) sin requerir conexión activa
        optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));

        return new GimnasioContext(optionsBuilder.Options);
    }
}
