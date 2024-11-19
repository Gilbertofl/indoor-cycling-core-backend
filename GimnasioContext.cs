using Microsoft.EntityFrameworkCore;

public class GimnasioContext : DbContext
{
    // Constructor que recibe las opciones de configuración para el contexto
    public GimnasioContext(DbContextOptions<GimnasioContext> options)
        : base(options)
    {
    }

    // DbSets que representan las tablas en la base de datos
    public DbSet<Usuario> Usuario { get; set; }
    public DbSet<Bicicletas> Bicicletas { get; set; }
    public DbSet<Reservacion> Reservaciones { get; set; }
    public DbSet<Horarios> Horarios { get; set; }
    public DbSet<Membresias> Membresias { get; set; }

    // Método para configurar el modelo de datos
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuración de la entidad Bicicletas
        modelBuilder.Entity<Bicicletas>()
            .HasKey(b => b.id_bicicleta); // Define la clave primaria

        // Configuración de la entidad Usuario
        modelBuilder.Entity<Usuario>()
            .HasKey(u => u.id_usuario); // Define la clave primaria

        // Configuración de la entidad Reservacion
        modelBuilder.Entity<Reservacion>()
            .HasKey(r => r.id_reservacion); // Define la clave primaria

        // Configuración de la entidad Horarios
        modelBuilder.Entity<Horarios>()
            .HasKey(h => h.id_horario); // Define la clave primaria

        // Configuración de la entidad Membresias
        modelBuilder.Entity<Membresias>()
            .HasKey(m => m.id_membresia); // Define la clave primaria

        // Mapea cada entidad a su tabla correspondiente
        modelBuilder.Entity<Usuario>().ToTable("usuarios");
        modelBuilder.Entity<Bicicletas>().ToTable("bicicletas");
        modelBuilder.Entity<Reservacion>().ToTable("reservaciones");
        modelBuilder.Entity<Horarios>().ToTable("horarios");
        modelBuilder.Entity<Membresias>().ToTable("membresias");
    }
}
