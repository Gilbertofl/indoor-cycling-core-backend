using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Control de suscripciones, vigencia y registro de membresías de clientes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MembresiasController : ControllerBase
{
    private readonly GimnasioContext _context;

    public MembresiasController(GimnasioContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registra y activa una nueva membresía para un cliente por el número de meses solicitado.
    /// </summary>
    /// <param name="dto">Número de cliente y meses de duración de la membresía.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="201">Membresía activada con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para registrar membresías.</response>
    /// <response code="404">Cliente no encontrado.</response>
    /// <response code="409">El cliente ya tiene una membresía activa y vigente.</response>
    [HttpPost("registrar")]
    [HttpPost("membresia/registrar")]
    [ProducesResponseType(typeof(MembresiaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarMembresia([FromBody] MembresiaCreateDto dto, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var cliente = await _context.Usuario.FindAsync(dto.id_cliente);
        if (cliente == null)
        {
            return NotFound(new { message = "Cliente no encontrado." });
        }

        // Verificar si ya tiene una membresía activa y vigente
        var membresiaActiva = await _context.Membresias
            .FirstOrDefaultAsync(m => m.id_cliente == dto.id_cliente && m.estado == "Activa" && m.fecha_expiracion > DateTime.UtcNow);

        if (membresiaActiva != null)
        {
            return Conflict(new
            {
                message = "El cliente ya cuenta con una membresía activa vigente.",
                membresiaExistente = membresiaActiva
            });
        }

        int meses = dto.meses_duracion > 0 ? dto.meses_duracion : 1;
        var nuevaMembresia = new Membresias
        {
            id_cliente = dto.id_cliente,
            fecha_inicio = DateTime.UtcNow,
            fecha_expiracion = DateTime.UtcNow.AddMonths(meses),
            estado = "Activa"
        };

        _context.Membresias.Add(nuevaMembresia);
        await _context.SaveChangesAsync();

        var respuesta = new MembresiaResponseDto
        {
            id_membresia = nuevaMembresia.id_membresia,
            id_cliente = nuevaMembresia.id_cliente,
            cliente_nombre = $"{cliente.nombre} {cliente.apellidos}".Trim(),
            cliente_email = cliente.email,
            fecha_inicio = nuevaMembresia.fecha_inicio,
            fecha_expiracion = nuevaMembresia.fecha_expiracion,
            estado = nuevaMembresia.estado
        };

        return CreatedAtAction(nameof(ObtenerMembresia), new { id = nuevaMembresia.id_membresia }, respuesta);
    }

    /// <summary>
    /// Consulta todas las membresías del gimnasio y actualiza automáticamente el estado a 'Vencida' si expiraron.
    /// </summary>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Lista completa de membresías con nombre de cliente y vigencia.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para ver la lista de membresías.</response>
    [HttpGet("listar")]
    [ProducesResponseType(typeof(IEnumerable<MembresiaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarMembresias([FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        // Auto-actualizar estado de membresías vencidas
        var ahora = DateTime.UtcNow;
        var membresiasVencidas = await _context.Membresias
            .Where(m => m.estado == "Activa" && m.fecha_expiracion < ahora)
            .ToListAsync();

        if (membresiasVencidas.Any())
        {
            foreach (var m in membresiasVencidas)
            {
                m.estado = "Vencida";
            }
            await _context.SaveChangesAsync();
        }

        var lista = await (from m in _context.Membresias
                           join u in _context.Usuario on m.id_cliente equals u.id_usuario into usuarioJoin
                           from u in usuarioJoin.DefaultIfEmpty()
                           select new MembresiaResponseDto
                           {
                               id_membresia = m.id_membresia,
                               id_cliente = m.id_cliente,
                               cliente_nombre = u != null ? $"{u.nombre} {u.apellidos}".Trim() : "Desconocido",
                               cliente_email = u != null ? u.email : string.Empty,
                               fecha_inicio = m.fecha_inicio,
                               fecha_expiracion = m.fecha_expiracion,
                               estado = m.estado
                           }).ToListAsync();

        return Ok(lista);
    }

    /// <summary>
    /// Consulta el detalle de una membresía específica por su código identificador.
    /// </summary>
    /// <param name="id">Número identificador de la membresía.</param>
    /// <param name="sessionToken">Clave de acceso activa.</param>
    /// <response code="200">Detalles de la membresía.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para consultar esta membresía.</response>
    /// <response code="404">Membresía no encontrada.</response>
    [HttpGet("detalle/{id}")]
    [ProducesResponseType(typeof(MembresiaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerMembresia(int id, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        var membresia = await _context.Membresias.FindAsync(id);
        if (membresia == null)
        {
            return NotFound(new { message = "Membresía no encontrada." });
        }

        // Si es cliente, solo puede ver la suya
        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != membresia.id_cliente)
        {
            return StatusCode(403, new { message = "No tienes permiso para ver esta membresía." });
        }

        // Actualizar si está vencida
        if (membresia.estado == "Activa" && membresia.fecha_expiracion < DateTime.UtcNow)
        {
            membresia.estado = "Vencida";
            await _context.SaveChangesAsync();
        }

        var cliente = await _context.Usuario.FindAsync(membresia.id_cliente);

        var respuesta = new MembresiaResponseDto
        {
            id_membresia = membresia.id_membresia,
            id_cliente = membresia.id_cliente,
            cliente_nombre = cliente != null ? $"{cliente.nombre} {cliente.apellidos}".Trim() : "Desconocido",
            cliente_email = cliente != null ? cliente.email : string.Empty,
            fecha_inicio = membresia.fecha_inicio,
            fecha_expiracion = membresia.fecha_expiracion,
            estado = membresia.estado
        };

        return Ok(respuesta);
    }

    /// <summary>
    /// Consulta el estado actual e historial de membresías de un cliente en particular.
    /// </summary>
    /// <param name="idCliente">Número identificador del cliente.</param>
    /// <param name="sessionToken">Clave de acceso activa.</param>
    /// <response code="200">Membresías del cliente.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para ver las membresías de otro cliente.</response>
    /// <response code="404">Cliente no encontrado.</response>
    [HttpGet("cliente/{idCliente}")]
    [ProducesResponseType(typeof(IEnumerable<MembresiaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerMembresiasDeCliente(int idCliente, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != idCliente)
        {
            return StatusCode(403, new { message = "No tienes permiso para consultar las membresías de otro cliente." });
        }

        var cliente = await _context.Usuario.FindAsync(idCliente);
        if (cliente == null)
        {
            return NotFound(new { message = "Cliente no encontrado." });
        }

        var ahora = DateTime.UtcNow;
        var membresias = await _context.Membresias
            .Where(m => m.id_cliente == idCliente)
            .OrderByDescending(m => m.fecha_inicio)
            .ToListAsync();

        bool huboCambios = false;
        foreach (var m in membresias)
        {
            if (m.estado == "Activa" && m.fecha_expiracion < ahora)
            {
                m.estado = "Vencida";
                huboCambios = true;
            }
        }
        if (huboCambios)
        {
            await _context.SaveChangesAsync();
        }

        var resultado = membresias.Select(m => new MembresiaResponseDto
        {
            id_membresia = m.id_membresia,
            id_cliente = m.id_cliente,
            cliente_nombre = $"{cliente.nombre} {cliente.apellidos}".Trim(),
            cliente_email = cliente.email,
            fecha_inicio = m.fecha_inicio,
            fecha_expiracion = m.fecha_expiracion,
            estado = m.estado
        });

        return Ok(resultado);
    }

    /// <summary>
    /// Modifica las fechas o el estado (Activa, Vencida, Cancelada) de una membresía.
    /// </summary>
    /// <param name="id">Número identificador de la membresía.</param>
    /// <param name="dto">Nuevos datos de la membresía.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Membresía modificada con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para modificar membresías.</response>
    /// <response code="404">Membresía no encontrada.</response>
    [HttpPut("editar/{id}")]
    [HttpPut("membresia/editar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditarMembresia(int id, [FromBody] MembresiaEditDto dto, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var membresia = await _context.Membresias.FindAsync(id);
        if (membresia == null)
        {
            return NotFound(new { message = "Membresía no encontrada." });
        }

        if (dto.fecha_inicio.HasValue) membresia.fecha_inicio = dto.fecha_inicio.Value;
        if (dto.fecha_expiracion.HasValue) membresia.fecha_expiracion = dto.fecha_expiracion.Value;
        if (!string.IsNullOrWhiteSpace(dto.estado)) membresia.estado = dto.estado.Trim();

        await _context.SaveChangesAsync();

        return Ok(new { message = "Membresía actualizada con éxito.", membresia });
    }

    /// <summary>
    /// Elimina el registro de una membresía del sistema.
    /// </summary>
    /// <param name="id">Número identificador de la membresía a eliminar.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Membresía eliminada con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para eliminar membresías.</response>
    /// <response code="404">Membresía no encontrada.</response>
    [HttpDelete("eliminar/{id}")]
    [HttpDelete("membresia/eliminar/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarMembresia(int id, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var membresia = await _context.Membresias.FindAsync(id);
        if (membresia == null)
        {
            return NotFound(new { message = "Membresía no encontrada." });
        }

        _context.Membresias.Remove(membresia);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Membresía eliminada con éxito." });
    }
}
