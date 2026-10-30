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
    public DbSet<HistorialReservaciones> HistorialReservaciones { get; set; }

    // Método para configurar el modelo de datos
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuración de la entidad Bicicletas
        modelBuilder.Entity<Bicicletas>(entity =>
        {
            entity.ToTable("bicicletas");
            entity.HasKey(b => b.id_bicicleta);
            entity.Property(b => b.nombre_bicicleta).HasMaxLength(100).IsRequired();
            entity.Property(b => b.estado).HasMaxLength(50).HasDefaultValue("libre");
        });

        // Configuración de la entidad Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(u => u.id_usuario);
            entity.HasIndex(u => u.email).IsUnique();
            entity.Property(u => u.nombre).HasMaxLength(100).IsRequired();
            entity.Property(u => u.apellidos).HasMaxLength(100).IsRequired();
            entity.Property(u => u.email).HasMaxLength(150).IsRequired();
            entity.Property(u => u.password).HasMaxLength(255).IsRequired();
            entity.Property(u => u.tipo_usuario).HasMaxLength(50).IsRequired();
            entity.Property(u => u.telefono).HasMaxLength(20);
            entity.Property(u => u.sessionToken).HasMaxLength(100);
        });

        // Configuración de la entidad Horarios
        modelBuilder.Entity<Horarios>(entity =>
        {
            entity.ToTable("horarios");
            entity.HasKey(h => h.id_horario);
            entity.Property(h => h.hora_inicio).IsRequired();
        });

        // Configuración de la entidad Reservacion
        modelBuilder.Entity<Reservacion>(entity =>
        {
            entity.ToTable("reservaciones");
            entity.HasKey(r => r.id_reservacion);
            entity.Property(r => r.nombre).HasMaxLength(100).IsRequired();
            entity.Property(r => r.apellidos).HasMaxLength(100).IsRequired();
            entity.Property(r => r.talla).HasMaxLength(20).IsRequired();
            entity.Property(r => r.email).HasMaxLength(150).IsRequired();

            // Claves foráneas conceptuales
            entity.HasIndex(r => new { r.id_horario, r.id_bicicleta });
        });

        // Configuración de la entidad Membresias
        modelBuilder.Entity<Membresias>(entity =>
        {
            entity.ToTable("membresias");
            entity.HasKey(m => m.id_membresia);
            entity.Property(m => m.estado).HasMaxLength(50).HasDefaultValue("Activa");
            entity.HasIndex(m => m.id_cliente);
        });

        // Configuracion de la entidad HistorialReservaciones
        modelBuilder.Entity<HistorialReservaciones>(entity =>
        {
            entity.ToTable("historialreservaciones");
            entity.HasKey(f => f.id_historial);
            entity.Property(f => f.nombre).HasMaxLength(100).IsRequired();
            entity.Property(f => f.apellidos).HasMaxLength(100).IsRequired();
            entity.Property(f => f.talla).HasMaxLength(20).IsRequired();
            entity.Property(f => f.email).HasMaxLength(150).IsRequired();
            entity.Property(f => f.estado).HasMaxLength(50).HasDefaultValue("finalizado");
        });
    }
}
