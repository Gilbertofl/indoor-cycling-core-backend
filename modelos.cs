using System.ComponentModel.DataAnnotations;

// ==========================================
// ENTIDADES DE BASE DE DATOS
// ==========================================

// Usuarios
public class Usuario
{
    public int id_usuario { get; set; }
    public required string nombre { get; set; }
    public required string apellidos { get; set; }
    public int edad { get; set; }
    public required string email { get; set; }
    public required string password { get; set; }
    public string? telefono { get; set; }
    public required string tipo_usuario { get; set; } // "secretaria", "cliente", "admin"
    public string? sessionToken { get; set; }
}

// Bicicletas
public class Bicicletas
{
    public int id_bicicleta { get; set; }
    public required string nombre_bicicleta { get; set; }
    public string estado { get; set; } = "libre"; // "libre", "ocupada", "mantenimiento"
}

// Horarios
public class Horarios
{
    public int id_horario { get; set; }
    public TimeSpan hora_inicio { get; set; }
}

// Reservacion
public class Reservacion
{
    public int id_reservacion { get; set; }
    public int id_cliente { get; set; }
    public required string nombre { get; set; }
    public required string apellidos { get; set; }
    public required string talla { get; set; }
    public required string email { get; set; }
    public int id_horario { get; set; }
    public int id_bicicleta { get; set; }
}

// Historial de Reservaciones
public class HistorialReservaciones
{
    public int id_historial { get; set; }
    public int id_reservacion { get; set; }
    public int id_cliente { get; set; }
    public int id_horario { get; set; }
    public int id_bicicleta { get; set; }
    public required string nombre { get; set; }
    public required string apellidos { get; set; }
    public required string talla { get; set; }
    public required string email { get; set; }
    public string estado { get; set; } = "finalizado";
    public DateTime fecha_registro { get; set; } = DateTime.UtcNow;
}

// Membresias
public class Membresias
{
    public int id_membresia { get; set; }
    public int id_cliente { get; set; }
    public DateTime fecha_inicio { get; set; } = DateTime.UtcNow;
    public DateTime fecha_expiracion { get; set; }
    public string estado { get; set; } = "Activa"; // "Activa", "Vencida", "Cancelada"
}

// ==========================================
// DTOs (Data Transfer Objects)
// ==========================================

// DTOs de Usuario
public class UsuarioRegistroDto
{
    [Required]
    public required string nombre { get; set; }

    [Required]
    public required string apellidos { get; set; }

    [Range(12, 120, ErrorMessage = "La edad debe ser válida.")]
    public int edad { get; set; }

    [Required, EmailAddress]
    public required string email { get; set; }

    [Required, MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public required string password { get; set; }

    public string? telefono { get; set; }

    public string tipo_usuario { get; set; } = "cliente";
}

public class UsuarioEditDto
{
    public string? nombre { get; set; }
    public string? apellidos { get; set; }
    public int? edad { get; set; }
    public string? email { get; set; }
    public string? password { get; set; }
    public string? telefono { get; set; }
    public string? tipo_usuario { get; set; }
}

public class UsuarioResponseDto
{
    public int id_usuario { get; set; }
    public string nombre { get; set; } = string.Empty;
    public string apellidos { get; set; } = string.Empty;
    public int edad { get; set; }
    public string email { get; set; } = string.Empty;
    public string? telefono { get; set; }
    public string tipo_usuario { get; set; } = string.Empty;
}

public class usuarioLogin
{
    [Required, EmailAddress]
    public required string email { get; set; }

    [Required]
    public required string password { get; set; }
}

// DTOs de Bicicletas
public class BicicletaCreateDto
{
    [Required]
    public required string nombre_bicicleta { get; set; }
    public string estado { get; set; } = "libre";
}

public class bicicletaEdit
{
    public string? nombre_bicicleta { get; set; }
    public string? estado { get; set; }
}

// DTOs de Horarios
public class HorarioCreateDto
{
    [Required]
    public required string hora_inicio { get; set; } // Formato "HH:mm" o "HH:mm:ss"
}

public class HorarioEditDto
{
    public string? hora_inicio { get; set; }
}

// DTOs de Reservaciones
public class ReservacionCreateDto
{
    [Required]
    public int id_cliente { get; set; }

    [Required]
    public int id_horario { get; set; }

    [Required]
    public int id_bicicleta { get; set; }

    [Required]
    public required string talla { get; set; }
}

public class ReservacionEditDto
{
    public int? id_horario { get; set; }
    public int? id_bicicleta { get; set; }
    public string? talla { get; set; }
    public string? nombre { get; set; }
    public string? apellidos { get; set; }
    public string? email { get; set; }
}

// DTOs de Membresias
public class MembresiaCreateDto
{
    [Required]
    public int id_cliente { get; set; }
    public int meses_duracion { get; set; } = 1;
}

public class MembresiaEditDto
{
    public DateTime? fecha_inicio { get; set; }
    public DateTime? fecha_expiracion { get; set; }
    public string? estado { get; set; }
}

public class MembresiaResponseDto
{
    public int id_membresia { get; set; }
    public int id_cliente { get; set; }
    public string cliente_nombre { get; set; } = string.Empty;
    public string cliente_email { get; set; } = string.Empty;
    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_expiracion { get; set; }
    public string estado { get; set; } = string.Empty;
    public bool esta_vigente => estado == "Activa" && fecha_expiracion >= DateTime.UtcNow;
}
