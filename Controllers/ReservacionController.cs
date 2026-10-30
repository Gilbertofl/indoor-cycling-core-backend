using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Proceso de reservación de bicicletas por turno de clase e historial de asistencia.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReservacionController : ControllerBase
{
    private readonly GimnasioContext _context;

    public ReservacionController(GimnasioContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Aparta una bicicleta en un horario específico para un cliente con membresía activa.
    /// </summary>
    /// <param name="dto">Datos de la reservación (cliente, horario, bicicleta y talla).</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o del propio cliente.</param>
    /// <response code="201">Reservación creada exitosamente.</response>
    /// <response code="400">El cliente no cuenta con una membresía activa y vigente para reservar.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para reservar a nombre de otra persona.</response>
    /// <response code="404">Cliente, horario o bicicleta no encontrados.</response>
    /// <response code="409">Conflicto: la bicicleta ya está ocupada en ese horario, o el cliente ya tiene una bicicleta apartada en ese turno.</response>
    [HttpPost("reservar")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reservar([FromBody] ReservacionCreateDto dto, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        // Si es cliente, solo puede reservar para sí mismo
        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != dto.id_cliente)
        {
            return StatusCode(403, new { message = "Solo puedes realizar reservaciones para tu propia cuenta." });
        }

        // 1. Validar existencia del cliente
        var cliente = await _context.Usuario.FindAsync(dto.id_cliente);
        if (cliente == null)
        {
            return NotFound(new { message = "Cliente no encontrado." });
        }

        // 2. Validar que el cliente cuente con membresía activa y vigente
        var membresiaActiva = await _context.Membresias
            .FirstOrDefaultAsync(m => m.id_cliente == dto.id_cliente && m.estado == "Activa" && m.fecha_expiracion > DateTime.UtcNow);

        if (membresiaActiva == null)
        {
            return BadRequest(new { message = "El cliente no cuenta con una membresía activa y vigente para reservar." });
        }

        // 3. Validar existencia del horario
        var horario = await _context.Horarios.FindAsync(dto.id_horario);
        if (horario == null)
        {
            return NotFound(new { message = "El horario especificado no existe." });
        }

        // 4. Validar existencia y estado de la bicicleta
        var bicicleta = await _context.Bicicletas.FindAsync(dto.id_bicicleta);
        if (bicicleta == null)
        {
            return NotFound(new { message = "La bicicleta especificada no existe." });
        }

        if (bicicleta.estado == "mantenimiento" || bicicleta.estado == "inactiva")
        {
            return Conflict(new { message = "La bicicleta no está disponible para su uso (en mantenimiento o inactiva)." });
        }

        // 5. Validar que la bicicleta no esté reservada en ese horario específico
        var bicicletaOcupadaEnHorario = await _context.Reservaciones
            .AnyAsync(r => r.id_horario == dto.id_horario && r.id_bicicleta == dto.id_bicicleta);

        if (bicicletaOcupadaEnHorario)
        {
            return Conflict(new { message = "La bicicleta ya se encuentra reservada para este horario." });
        }

        // 6. Validar que el cliente no tenga ya otra bicicleta reservada en ese mismo horario
        var clienteYaTieneReservaEnHorario = await _context.Reservaciones
            .AnyAsync(r => r.id_horario == dto.id_horario && r.id_cliente == dto.id_cliente);

        if (clienteYaTieneReservaEnHorario)
        {
            return Conflict(new { message = "El cliente ya tiene una bicicleta reservada en este horario." });
        }

        var nuevaReservacion = new Reservacion
        {
            id_cliente = cliente.id_usuario,
            id_horario = horario.id_horario,
            id_bicicleta = bicicleta.id_bicicleta,
            talla = dto.talla.Trim(),
            nombre = cliente.nombre,
            apellidos = cliente.apellidos,
            email = cliente.email
        };

        _context.Reservaciones.Add(nuevaReservacion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerHistorialReservaciones), new { id = nuevaReservacion.id_reservacion }, new
        {
            message = "Reservación realizada con éxito.",
            reservacion = new
            {
                nuevaReservacion.id_reservacion,
                nuevaReservacion.id_cliente,
                cliente = $"{nuevaReservacion.nombre} {nuevaReservacion.apellidos}".Trim(),
                nuevaReservacion.email,
                nuevaReservacion.talla,
                bicicleta = bicicleta.nombre_bicicleta,
                horario = horario.hora_inicio.ToString(@"hh\:mm")
            }
        });
    }

    /// <summary>
    /// Modifica una reservación activa (ejemplo: cambiar bicicleta, horario o talla).
    /// </summary>
    /// <param name="id">Número identificador de la reservación.</param>
    /// <param name="dto">Nuevos datos para la reservación.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o del dueño de la reserva.</param>
    /// <response code="200">Reservación editada con éxito.</response>
    /// <response code="400">Horario no válido.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para modificar esta reservación.</response>
    /// <response code="404">Reservación no encontrada.</response>
    /// <response code="409">La nueva bicicleta ya está ocupada en ese horario o está en mantenimiento.</response>
    [HttpPut("editar-reservacion/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EditarReservacion(int id, [FromBody] ReservacionEditDto dto, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        var reservacion = await _context.Reservaciones.FindAsync(id);
        if (reservacion == null)
        {
            return NotFound(new { message = "Reservación no encontrada." });
        }

        // Solo secretaria o el cliente dueño de la reserva
        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != reservacion.id_cliente)
        {
            return StatusCode(403, new { message = "No tienes permiso para modificar esta reservación." });
        }

        int targetHorarioId = dto.id_horario ?? reservacion.id_horario;
        int targetBicicletaId = dto.id_bicicleta ?? reservacion.id_bicicleta;

        // Validar nuevo horario si cambió
        if (dto.id_horario.HasValue && dto.id_horario.Value != reservacion.id_horario)
        {
            var horarioExiste = await _context.Horarios.AnyAsync(h => h.id_horario == dto.id_horario.Value);
            if (!horarioExiste)
            {
                return BadRequest(new { message = "El horario especificado no existe." });
            }
        }

        // Validar nueva bicicleta si cambió
        if (dto.id_bicicleta.HasValue && dto.id_bicicleta.Value != reservacion.id_bicicleta)
        {
            var bici = await _context.Bicicletas.FindAsync(dto.id_bicicleta.Value);
            if (bici == null)
            {
                return BadRequest(new { message = "La bicicleta especificada no existe." });
            }
            if (bici.estado == "mantenimiento" || bici.estado == "inactiva")
            {
                return Conflict(new { message = "La bicicleta seleccionada está fuera de servicio." });
            }
        }

        // Validar si la combinación (targetHorarioId, targetBicicletaId) está ocupada por otra reservación
        var ocupadaPorOtra = await _context.Reservaciones
            .AnyAsync(r => r.id_horario == targetHorarioId && r.id_bicicleta == targetBicicletaId && r.id_reservacion != id);

        if (ocupadaPorOtra)
        {
            return Conflict(new { message = "La bicicleta seleccionada ya está ocupada en ese horario por otra reservación." });
        }

        reservacion.id_horario = targetHorarioId;
        reservacion.id_bicicleta = targetBicicletaId;
        if (!string.IsNullOrWhiteSpace(dto.talla)) reservacion.talla = dto.talla.Trim();
        if (!string.IsNullOrWhiteSpace(dto.nombre)) reservacion.nombre = dto.nombre.Trim();
        if (!string.IsNullOrWhiteSpace(dto.apellidos)) reservacion.apellidos = dto.apellidos.Trim();
        if (!string.IsNullOrWhiteSpace(dto.email)) reservacion.email = dto.email.Trim();

        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservación editada con éxito.", reservacion });
    }

    /// <summary>
    /// Cancela y retira una reservación activa, liberando la bicicleta y el cupo en la clase.
    /// </summary>
    /// <param name="id">Número identificador de la reservación a cancelar.</param>
    /// <param name="sessionToken">Clave de acceso activa.</param>
    /// <response code="200">Reservación cancelada con éxito.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para cancelar esta reservación.</response>
    /// <response code="404">Reservación no encontrada.</response>
    [HttpDelete("eliminar-reservacion/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarReservacion(int id, [FromHeader] string? sessionToken)
    {
        var (usuarioAuth, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        var reservacion = await _context.Reservaciones.FindAsync(id);
        if (reservacion == null)
        {
            return NotFound(new { message = "Reservación no encontrada." });
        }

        // Cliente solo puede cancelar la suya
        if (usuarioAuth!.tipo_usuario == "cliente" && usuarioAuth.id_usuario != reservacion.id_cliente)
        {
            return StatusCode(403, new { message = "No tienes permiso para cancelar esta reservación." });
        }

        _context.Reservaciones.Remove(reservacion);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservación cancelada y eliminada con éxito." });
    }

    /// <summary>
    /// Consulta las reservaciones activas y el historial de clases completadas.
    /// </summary>
    /// <param name="sessionToken">Clave de acceso activa.</param>
    /// <response code="200">Listado de reservaciones activas y clases concluidas.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    [HttpGet("historial")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObtenerHistorialReservaciones([FromHeader] string? sessionToken)
    {
        var (usuario, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken);
        if (error != null) return error;

        bool esAdminOSecretaria = usuario!.tipo_usuario == "secretaria" || usuario.tipo_usuario == "admin";

        var queryActivas = _context.Reservaciones.AsQueryable();
        var queryHistorial = _context.HistorialReservaciones.AsQueryable();

        if (!esAdminOSecretaria)
        {
            queryActivas = queryActivas.Where(r => r.id_cliente == usuario.id_usuario);
            queryHistorial = queryHistorial.Where(h => h.id_cliente == usuario.id_usuario);
        }

        var reservacionesActuales = await (from r in queryActivas
                                           join b in _context.Bicicletas on r.id_bicicleta equals b.id_bicicleta
                                           join h in _context.Horarios on r.id_horario equals h.id_horario
                                           select new
                                           {
                                               r.id_reservacion,
                                               r.id_cliente,
                                               r.nombre,
                                               r.apellidos,
                                               r.email,
                                               r.talla,
                                               id_bicicleta = b.id_bicicleta,
                                               bicicleta = b.nombre_bicicleta,
                                               id_horario = h.id_horario,
                                               horario = h.hora_inicio.ToString(@"hh\:mm")
                                           }).ToListAsync();

        var historialReservaciones = await (from h in queryHistorial
                                            join b in _context.Bicicletas on h.id_bicicleta equals b.id_bicicleta
                                            join hor in _context.Horarios on h.id_horario equals hor.id_horario
                                            orderby h.id_historial descending
                                            select new
                                            {
                                                h.id_historial,
                                                h.id_reservacion,
                                                h.id_cliente,
                                                h.nombre,
                                                h.apellidos,
                                                h.email,
                                                h.talla,
                                                id_bicicleta = b.id_bicicleta,
                                                bicicleta = b.nombre_bicicleta,
                                                id_horario = hor.id_horario,
                                                horario = hor.hora_inicio.ToString(@"hh\:mm"),
                                                h.estado,
                                                h.fecha_registro
                                            }).ToListAsync();

        return Ok(new
        {
            usuario = $"{usuario.nombre} {usuario.apellidos}".Trim(),
            tipo_usuario = usuario.tipo_usuario,
            reservacionesActuales,
            historialReservaciones
        });
    }

    /// <summary>
    /// Concluye una clase de spinning y mueve la reservación al historial de clases finalizadas.
    /// </summary>
    /// <param name="id">Número identificador de la reservación a concluir.</param>
    /// <param name="sessionToken">Clave de acceso de la secretaria o administración.</param>
    /// <response code="200">Reservación finalizada y guardada en el historial.</response>
    /// <response code="401">Se requiere iniciar sesión.</response>
    /// <response code="403">No tienes permiso para concluir reservaciones.</response>
    /// <response code="404">Reservación no encontrada.</response>
    [HttpPost("mover-a-historial/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoverReservacionAHistorial(int id, [FromHeader] string? sessionToken)
    {
        var (_, error) = await AuthHelper.ValidarSesionAsync(_context, sessionToken, new[] { "secretaria", "admin" });
        if (error != null) return error;

        var reservacion = await _context.Reservaciones.FindAsync(id);
        if (reservacion == null)
        {
            return NotFound(new { message = "Reservación no encontrada." });
        }

        var historial = new HistorialReservaciones
        {
            id_reservacion = reservacion.id_reservacion,
            id_cliente = reservacion.id_cliente,
            id_horario = reservacion.id_horario,
            id_bicicleta = reservacion.id_bicicleta,
            estado = "finalizado",
            nombre = reservacion.nombre,
            apellidos = reservacion.apellidos,
            email = reservacion.email,
            talla = reservacion.talla,
            fecha_registro = DateTime.UtcNow
        };

        _context.HistorialReservaciones.Add(historial);
        _context.Reservaciones.Remove(reservacion);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Reservación concluida y movida al historial con éxito.",
            historial
        });
    }
}
