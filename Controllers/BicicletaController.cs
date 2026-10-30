using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Catálogo e inventario de bicicletas para las clases de spinning.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BicicletasController : ControllerBase
{
    private readonly GimnasioContext _context;

    public BicicletasController(GimnasioContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Da de alta una nueva bicicleta en el gimnasio (solo secretaria o administración).
    /// </summary>
    /// <param name="dto">Nombre identificador y estado inicial de la bicicleta.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria.</param>
    /// <response code="201">Bicicleta agregada exitosamente.</response>
    /// <response code="400">El nombre de la bicicleta es obligatorio.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para agregar bicicletas.</response>
    [HttpPost("agregar")]
    [ProducesResponseType(typeof(Bicicletas), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AgregarBicicleta([FromBody] BicicletaCreateDto dto, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        if (string.IsNullOrWhiteSpace(dto.nombre_bicicleta))
        {
            return BadRequest(new { message = "El nombre de la bicicleta es requerido." });
        }

        var nuevaBicicleta = new Bicicletas
        {
            nombre_bicicleta = dto.nombre_bicicleta.Trim(),
            estado = string.IsNullOrWhiteSpace(dto.estado) ? "libre" : dto.estado.Trim().ToLower()
        };

        _context.Bicicletas.Add(nuevaBicicleta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerBicicleta), new { id = nuevaBicicleta.id_bicicleta }, nuevaBicicleta);
    }

    /// <summary>
    /// Actualiza el nombre o el estado de una bicicleta (ej. cambiarla a 'mantenimiento').
    /// </summary>
    /// <param name="id">Número identificador de la bicicleta.</param>
    /// <param name="dto">Nuevos datos a guardar.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria.</param>
    /// <response code="200">Bicicleta actualizada con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para modificar bicicletas.</response>
    /// <response code="404">Bicicleta no encontrada.</response>
    [HttpPut("editar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditarBicicleta(int id, [FromBody] bicicletaEdit dto, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var bicicleta = await _context.Bicicletas.FindAsync(id);
        if (bicicleta == null)
        {
            return NotFound(new { message = "Bicicleta no encontrada." });
        }

        if (!string.IsNullOrWhiteSpace(dto.nombre_bicicleta))
        {
            bicicleta.nombre_bicicleta = dto.nombre_bicicleta.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.estado))
        {
            bicicleta.estado = dto.estado.Trim().ToLower();
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Bicicleta actualizada con éxito.", bicicleta });
    }

    /// <summary>
    /// Elimina una bicicleta del inventario (solo si no tiene reservaciones activas).
    /// </summary>
    /// <param name="id">Número identificador de la bicicleta a retirar.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria.</param>
    /// <response code="200">Bicicleta eliminada con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para retirar bicicletas.</response>
    /// <response code="404">Bicicleta no encontrada.</response>
    /// <response code="409">La bicicleta tiene reservaciones activas programadas.</response>
    [HttpDelete("borrar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BorrarBicicleta(int id, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var bicicleta = await _context.Bicicletas.FindAsync(id);
        if (bicicleta == null)
        {
            return NotFound(new { message = "Bicicleta no encontrada." });
        }

        // Verificar si la bicicleta está en alguna reservación activa
        var tieneReservacionesActivas = await _context.Reservaciones.AnyAsync(r => r.id_bicicleta == id);
        if (tieneReservacionesActivas)
        {
            return Conflict(new { message = "No se puede eliminar la bicicleta porque tiene reservaciones activas programadas." });
        }

        _context.Bicicletas.Remove(bicicleta);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Bicicleta eliminada con éxito." });
    }

    /// <summary>
    /// Muestra la lista de bicicletas. Si se envía el número de horario, avisa cuáles están disponibles en ese turno.
    /// </summary>
    /// <param name="idHorario">Opcional. Número de horario para consultar disponibilidad inmediata.</param>
    /// <response code="200">Lista de bicicletas con su estado general y su disponibilidad para la clase.</response>
    [HttpGet("listar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarBicicletas([FromQuery] int? idHorario)
    {
        var bicicletas = await _context.Bicicletas.ToListAsync();

        if (!idHorario.HasValue)
        {
            return Ok(bicicletas);
        }

        // Obtener los IDs de bicicletas reservadas para ese horario específico
        var bicisOcupadasEnHorario = await _context.Reservaciones
            .Where(r => r.id_horario == idHorario.Value)
            .Select(r => r.id_bicicleta)
            .ToListAsync();

        var resultado = bicicletas.Select(b => new
        {
            b.id_bicicleta,
            b.nombre_bicicleta,
            estado_general = b.estado,
            disponible_en_horario = b.estado != "mantenimiento" && !bicisOcupadasEnHorario.Contains(b.id_bicicleta)
        });

        return Ok(resultado);
    }

    /// <summary>
    /// Consulta la información de una bicicleta por su número de identificación.
    /// </summary>
    /// <param name="id">Número identificador de la bicicleta.</param>
    /// <response code="200">Detalle de la bicicleta.</response>
    /// <response code="404">Bicicleta no encontrada.</response>
    [HttpGet("detalle/{id}")]
    [ProducesResponseType(typeof(Bicicletas), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerBicicleta(int id)
    {
        var bicicleta = await _context.Bicicletas.FindAsync(id);
        if (bicicleta == null)
        {
            return NotFound(new { message = "Bicicleta no encontrada." });
        }

        return Ok(bicicleta);
    }
}
