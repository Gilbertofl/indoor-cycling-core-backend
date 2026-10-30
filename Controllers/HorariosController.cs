using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

/// <summary>
/// Programación de horarios y turnos para las clases de spinning.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HorariosController : ControllerBase
{
    private readonly GimnasioContext _context;

    public HorariosController(GimnasioContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Agrega un nuevo horario de clase (ejemplo: '08:00' o '18:30').
    /// </summary>
    /// <param name="dto">Hora de inicio de la clase.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="201">Horario creado con éxito.</response>
    /// <response code="400">Formato de hora no válido. Se recomienda 'HH:mm'.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para agregar horarios.</response>
    /// <response code="409">Ya existe un horario programado a la misma hora.</response>
    [HttpPost("agregar")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AgregarHorario([FromBody] HorarioCreateDto dto, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        if (!TryParseHora(dto.hora_inicio, out var horaParsed))
        {
            return BadRequest(new { message = "Formato de hora inválido. Utilice el formato HH:mm o HH:mm:ss (ejemplo: '08:00' o '17:30:00')." });
        }

        // Verificar si ya existe este horario
        var existe = await _context.Horarios.AnyAsync(h => h.hora_inicio == horaParsed);
        if (existe)
        {
            return Conflict(new { message = "Ya existe un horario registrado con esa misma hora de inicio." });
        }

        var nuevoHorario = new Horarios
        {
            hora_inicio = horaParsed
        };

        _context.Horarios.Add(nuevoHorario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerHorario), new { id = nuevoHorario.id_horario }, new
        {
            message = "Horario agregado con éxito.",
            horario = new
            {
                nuevoHorario.id_horario,
                hora_inicio = nuevoHorario.hora_inicio.ToString(@"hh\:mm")
            }
        });
    }

    /// <summary>
    /// Modifica la hora de inicio de un turno existente.
    /// </summary>
    /// <param name="id">Número identificador del horario a modificar.</param>
    /// <param name="dto">Nueva hora de inicio.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Horario modificado con éxito.</response>
    /// <response code="400">Formato de hora no válido.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para modificar horarios.</response>
    /// <response code="404">Horario no encontrado.</response>
    /// <response code="409">Ya existe otro turno programado a esa misma hora.</response>
    [HttpPut("editar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarHorario(int id, [FromBody] HorarioEditDto dto, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var horario = await _context.Horarios.FindAsync(id);
        if (horario == null)
        {
            return NotFound(new { message = "Horario no encontrado." });
        }

        if (!string.IsNullOrWhiteSpace(dto.hora_inicio))
        {
            if (!TryParseHora(dto.hora_inicio, out var horaParsed))
            {
                return BadRequest(new { message = "Formato de hora inválido. Utilice el formato HH:mm o HH:mm:ss." });
            }

            var existeOtro = await _context.Horarios.AnyAsync(h => h.hora_inicio == horaParsed && h.id_horario != id);
            if (existeOtro)
            {
                return Conflict(new { message = "Ya existe otro horario registrado con esa hora." });
            }

            horario.hora_inicio = horaParsed;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Horario editado con éxito.",
            horario = new
            {
                horario.id_horario,
                hora_inicio = horario.hora_inicio.ToString(@"hh\:mm")
            }
        });
    }

    /// <summary>
    /// Elimina un horario de clase (solo si no tiene reservaciones activas).
    /// </summary>
    /// <param name="id">Número identificador del horario a eliminar.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Horario eliminado con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para eliminar horarios.</response>
    /// <response code="404">Horario no encontrado.</response>
    /// <response code="409">El horario tiene reservaciones activas programadas.</response>
    [HttpDelete("eliminar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminarHorario(int id, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var horario = await _context.Horarios.FindAsync(id);
        if (horario == null)
        {
            return NotFound(new { message = "Horario no encontrado." });
        }

        // Validar si tiene reservaciones activas asociadas
        var tieneReservas = await _context.Reservaciones.AnyAsync(r => r.id_horario == id);
        if (tieneReservas)
        {
            return Conflict(new { message = "No se puede eliminar el horario porque tiene reservaciones activas programadas." });
        }

        _context.Horarios.Remove(horario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Horario eliminado con éxito." });
    }

    /// <summary>
    /// Consulta todos los horarios disponibles para las clases ordenados cronológicamente.
    /// </summary>
    /// <response code="200">Lista de horarios de clases.</response>
    [HttpGet("listar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarHorarios()
    {
        var horarios = await _context.Horarios
            .OrderBy(h => h.hora_inicio)
            .ToListAsync();

        var resultado = horarios.Select(h => new
        {
            h.id_horario,
            hora_inicio = h.hora_inicio.ToString(@"hh\:mm")
        });

        return Ok(resultado);
    }

    /// <summary>
    /// Consulta un horario de clase específico por su número identificador.
    /// </summary>
    /// <param name="id">Número identificador del horario.</param>
    /// <response code="200">Detalle del horario.</response>
    /// <response code="404">Horario no encontrado.</response>
    [HttpGet("detalle/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerHorario(int id)
    {
        var horario = await _context.Horarios.FindAsync(id);
        if (horario == null)
        {
            return NotFound(new { message = "Horario no encontrado." });
        }

        return Ok(new
        {
            horario.id_horario,
            hora_inicio = horario.hora_inicio.ToString(@"hh\:mm")
        });
    }

    private static bool TryParseHora(string? input, out TimeSpan result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string[] formats = { @"hh\:mm\:ss", @"h\:mm\:ss", @"hh\:mm", @"h\:mm", "HH:mm:ss", "H:mm:ss", "HH:mm", "H:mm" };
        return TimeSpan.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, out result)
            || TimeSpan.TryParse(input.Trim(), out result);
    }
}
