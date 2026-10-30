using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Gestión de cuentas de usuario, inicio de sesión y perfiles.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsuariosController : ControllerBase
{
    private readonly GimnasioContext _context;

    public UsuariosController(GimnasioContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registra una nueva cuenta de usuario (cliente o secretaria).
    /// </summary>
    /// <param name="dto">Datos personales requeridos para crear la cuenta.</param>
    /// <response code="201">Cuenta creada con éxito. Regresa los datos del usuario registrado.</response>
    /// <response code="400">Los datos enviados están incompletos o no son válidos.</response>
    /// <response code="409">El correo electrónico ya se encuentra registrado con otra cuenta.</response>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioRegistroDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar si el correo ya está registrado
        var emailExiste = await _context.Usuario.AnyAsync(u => u.email.ToLower() == dto.email.ToLower());
        if (emailExiste)
        {
            return Conflict(new { message = "El correo electrónico ya está registrado." });
        }

        var tipo = string.IsNullOrWhiteSpace(dto.tipo_usuario) ? "cliente" : dto.tipo_usuario.ToLower();
        if (tipo != "cliente" && tipo != "secretaria" && tipo != "admin")
        {
            return BadRequest(new { message = "El tipo de usuario debe ser 'cliente', 'secretaria' o 'admin'." });
        }

        var nuevoUsuario = new Usuario
        {
            nombre = dto.nombre.Trim(),
            apellidos = dto.apellidos.Trim(),
            edad = dto.edad,
            email = dto.email.Trim().ToLower(),
            password = PasswordHelper.HashPassword(dto.password),
            telefono = dto.telefono?.Trim(),
            tipo_usuario = tipo
        };

        _context.Usuario.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        var respuesta = MapearARespuesta(nuevoUsuario);
        return CreatedAtAction(nameof(ObtenerUsuario), new { id = nuevoUsuario.id_usuario }, respuesta);
    }

    /// <summary>
    /// Inicia sesión con correo y contraseña. Devuelve la clave de acceso (token) para usar en las demás opciones.
    /// </summary>
    /// <param name="loginDto">Credenciales de acceso (correo y contraseña).</param>
    /// <response code="200">Inicio de sesión exitoso. Entrega los datos del usuario y su clave de acceso (sessionToken).</response>
    /// <response code="400">Formato de datos no válido.</response>
    /// <response code="401">Correo o contraseña incorrectos.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] usuarioLogin loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _context.Usuario
            .FirstOrDefaultAsync(u => u.email.ToLower() == loginDto.email.ToLower());

        if (usuario == null || !PasswordHelper.VerifyPassword(loginDto.password, usuario.password))
        {
            return Unauthorized(new { message = "Credenciales incorrectas." });
        }

        // Si la contraseña estaba en texto plano, actualizarla de forma transparente con hash
        if (!usuario.password.Contains(':'))
        {
            usuario.password = PasswordHelper.HashPassword(loginDto.password);
        }

        // Generar y guardar nuevo sessionToken
        var sessionToken = Guid.NewGuid().ToString();
        usuario.sessionToken = sessionToken;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Sesión iniciada con éxito.",
            usuario = MapearARespuesta(usuario),
            sessionToken
        });
    }

    /// <summary>
    /// Cierra la sesión activa e invalida la clave de acceso actual.
    /// </summary>
    /// <param name="sessionToken">Clave de acceso enviada en el encabezado.</param>
    /// <response code="200">Sesión cerrada correctamente.</response>
    /// <response code="401">La clave de acceso no fue enviada o ya no es válida.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromHeader] string? sessionToken)
    {
        var (usuario, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        usuario!.sessionToken = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Sesión cerrada correctamente." });
    }

    /// <summary>
    /// Lista todos los usuarios registrados en el gimnasio (exclusivo para secretaria o administración).
    /// </summary>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Lista completa de usuarios.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para ver la lista de usuarios.</response>
    [HttpGet("listar")]
    [ProducesResponseType(typeof(IEnumerable<UsuarioResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuarios([FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var usuarios = await _context.Usuario
            .Select(u => MapearARespuesta(u))
            .ToListAsync();

        return Ok(usuarios);
    }

    /// <summary>
    /// Muestra los detalles y perfil de un usuario por su número de identificación.
    /// </summary>
    /// <param name="id">Número de identificación del usuario.</param>
    /// <param name="sessionToken">Clave de acceso activa.</param>
    /// <response code="200">Detalles del usuario encontrado.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para consultar el perfil de otro usuario.</response>
    /// <response code="404">Usuario no encontrado.</response>
    [HttpGet("detalle/{id}")]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerUsuario(int id, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        // Un cliente solo puede ver su propio perfil; la secretaria puede ver cualquiera
        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != id)
        {
            return StatusCode(403, new { message = "No tienes permisos para ver el perfil de otro usuario." });
        }

        var usuario = await _context.Usuario.FindAsync(id);
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        return Ok(MapearARespuesta(usuario));
    }

    /// <summary>
    /// Actualiza los datos de un usuario (nombre, teléfono, correo o contraseña).
    /// </summary>
    /// <param name="id">Número de identificación del usuario a modificar.</param>
    /// <param name="dto">Nuevos datos a actualizar.</param>
    /// <param name="sessionToken">Clave de acceso activa.</param>
    /// <response code="200">Usuario modificado con éxito.</response>
    /// <response code="400">Datos no válidos.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para modificar este usuario.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="409">El nuevo correo ya pertenece a otra persona.</response>
    [HttpPut("editar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarUsuario(int id, [FromBody] UsuarioEditDto dto, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        // Un cliente solo puede editar su propio perfil
        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != id)
        {
            return StatusCode(403, new { message = "No tienes permisos para modificar este usuario." });
        }

        var usuario = await _context.Usuario.FindAsync(id);
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        // Si cambia de email, validar que no esté en uso por otro
        if (!string.IsNullOrWhiteSpace(dto.email) && dto.email.ToLower() != usuario.email.ToLower())
        {
            var emailEnUso = await _context.Usuario.AnyAsync(u => u.email.ToLower() == dto.email.ToLower() && u.id_usuario != id);
            if (emailEnUso)
            {
                return Conflict(new { message = "El nuevo correo ya está en uso por otra cuenta." });
            }
            usuario.email = dto.email.Trim().ToLower();
        }

        if (!string.IsNullOrWhiteSpace(dto.nombre)) usuario.nombre = dto.nombre.Trim();
        if (!string.IsNullOrWhiteSpace(dto.apellidos)) usuario.apellidos = dto.apellidos.Trim();
        if (dto.edad.HasValue && dto.edad.Value > 0) usuario.edad = dto.edad.Value;
        if (dto.telefono != null) usuario.telefono = dto.telefono.Trim();

        // Solo secretaria/admin puede cambiar el tipo de usuario
        if (!string.IsNullOrWhiteSpace(dto.tipo_usuario) && (usuarioAuth.tipo_usuario == "secretaria" || usuarioAuth.tipo_usuario == "admin"))
        {
            usuario.tipo_usuario = dto.tipo_usuario.ToLower();
        }

        // Si se envía nueva contraseña, hashearla
        if (!string.IsNullOrWhiteSpace(dto.password))
        {
            if (dto.password.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });
            }
            usuario.password = PasswordHelper.HashPassword(dto.password);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Usuario actualizado con éxito.",
            usuario = MapearARespuesta(usuario)
        });
    }

    /// <summary>
    /// Elimina a un usuario del sistema (solo si no tiene reservaciones pendientes).
    /// </summary>
    /// <param name="id">Número de identificación del usuario a eliminar.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Usuario eliminado con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para eliminar usuarios.</response>
    /// <response code="404">Usuario no encontrado.</response>
    /// <response code="409">El usuario tiene reservaciones activas pendientes de concluir.</response>
    [HttpDelete("borrar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BorrarUsuario(int id, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var usuario = await _context.Usuario.FindAsync(id);
        if (usuario == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        // Verificar si tiene reservaciones activas
        var tieneReservas = await _context.Reservaciones.AnyAsync(r => r.id_cliente == id);
        if (tieneReservas)
        {
            return Conflict(new { message = "No se puede eliminar el usuario porque tiene reservaciones activas. Cancélelas o muévalas al historial primero." });
        }

        _context.Usuario.Remove(usuario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuario eliminado con éxito." });
    }

    private static UsuarioResponseDto MapearARespuesta(Usuario usuario)
    {
        return new UsuarioResponseDto
        {
            id_usuario = usuario.id_usuario,
            nombre = usuario.nombre,
            apellidos = usuario.apellidos,
            edad = usuario.edad,
            email = usuario.email,
            telefono = usuario.telefono,
            tipo_usuario = usuario.tipo_usuario
        };
    }
}
