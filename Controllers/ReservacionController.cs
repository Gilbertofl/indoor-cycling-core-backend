using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class ReservacionController : ControllerBase
{
    private readonly GimnasioContext _context;

    public ReservacionController(GimnasioContext context)
    {
        _context = context;
    }

    [HttpPost("reservar")]
    public async Task<IActionResult> Reservar([FromBody] Reservacion nuevaReservacion, [FromHeader] string sessionToken)
    {
        // Verificar si el token es válido
        var usuarioToken = _context.Usuario.FirstOrDefault(u => u.sessionToken == sessionToken);

        if (usuarioToken == null || usuarioToken.tipo_usuario != "secretaria")
        {
            return Forbid("Acceso no permitido. Solo la secretaria puede crear reservaciones.");
        }

        // Verificar si ya existe una reservación con la misma bicicleta y horario
        var reservacionExistente = await _context.Reservaciones
            .FirstOrDefaultAsync(r => r.id_horario == nuevaReservacion.id_horario && r.id_bicicleta == nuevaReservacion.id_bicicleta);

        if (reservacionExistente != null)
        {
            return Conflict(new { message = "La bicicleta ya está reservada para este horario." });
        }

        // Verificar el cliente
        var cliente = await _context.Usuario.FindAsync(nuevaReservacion.id_cliente);

        if (cliente == null)
        {
            return NotFound("Cliente no encontrado.");
        }

        // Actualizar el estado de la bicicleta
        var bicicleta = await _context.Bicicletas.FindAsync(nuevaReservacion.id_bicicleta);

        if (bicicleta == null)
        {
            return NotFound("Bicicleta no encontrada.");
        }

        if (bicicleta.estado == "ocupada")
        {
            return Conflict(new { message = "La bicicleta ya está ocupada." });
        }

        bicicleta.estado = "ocupada"; // Marcar la bicicleta como ocupada

        // Completar los datos de la reservación
        nuevaReservacion.nombre = cliente.nombre;
        nuevaReservacion.apellidos = cliente.apellidos;
        nuevaReservacion.email = cliente.email;

        // Guardar la reservación y actualizar el estado de la bicicleta
        _context.Reservaciones.Add(nuevaReservacion);
        _context.Bicicletas.Update(bicicleta);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservación realizada con éxito", nuevaReservacion });
    }


    [HttpPut("editar-reservacion/{id}")]
    public async Task<IActionResult> EditarReservacion(int id, [FromBody] Reservacion reservacionEditada, [FromHeader] string sessionToken)
    {   
        // Validar el token se la sesion y el tipo de usuario
        var usuarioToken = await _context.Usuario.FirstOrDefaultAsync(u => u.sessionToken == sessionToken);

        if (usuarioToken == null || usuarioToken.tipo_usuario != "secretaria")
        {
            return Forbid("Acceso no permitido.");
        }

        // Buscar la reservación por su ID
        var reservacion = await _context.Reservaciones.FindAsync(id);

        if (reservacion == null)
        {
            return NotFound("Reservación no encontrada.");
        }
        
        // validacion de nuevos valores
        if(reservacionEditada.id_horario != reservacion.id_horario)
        {
            var horarioExiste = await _context.Horarios.AnyAsync(h => h.id_horario == reservacionEditada.id_horario);
            if (!horarioExiste)
            {
                return BadRequest("El horario especifico no existe.");
            }
        }

        if (reservacionEditada.id_bicicleta != reservacion.id_bicicleta)
        {
            // Marcar como "libre" la bicicleta anterior
            var bicicletaAnterior = await _context.Bicicletas.FindAsync(reservacion.id_bicicleta);
            if (bicicletaAnterior != null)
            {
                bicicletaAnterior.estado = "libre";
            }

            // Validar y marcar como "ocupada" la nueva bicicleta
            var bicicletaNueva = await _context.Bicicletas.FindAsync(reservacionEditada.id_bicicleta);
            if (bicicletaNueva == null)
            {
                return BadRequest("La bicicleta especificada no existe.");
            }

            if (bicicletaNueva.estado == "ocupada")
            {
                return Conflict(new { message = "La nueva bicicleta ya está ocupada." });
            }

            bicicletaNueva.estado = "ocupada";
            reservacion.id_bicicleta = reservacionEditada.id_bicicleta;
        }
            // Actualizar los datos de la reservación
            reservacion.nombre = reservacionEditada.nombre ?? reservacion.nombre;
            reservacion.apellidos = reservacionEditada.apellidos ?? reservacion.apellidos;
            reservacion.email = reservacionEditada.email ?? reservacion.email;
            reservacion.talla = reservacionEditada.talla ?? reservacion.talla;
            reservacion.id_horario = reservacionEditada.id_horario;
        

        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservación editada con éxito", reservacion });
    }


    [HttpDelete("eliminar-reservacion/{id}")]
    public async Task<IActionResult> EliminarReservacion(int id)
    {
        // Buscar la reservación por su ID
        var reservacion = await _context.Reservaciones.FindAsync(id);

        if (reservacion == null)
        {
            return NotFound("Reservación no encontrada.");
        }

        // Actualizar el estado de la bicicleta a "disponible"
        var bicicleta = await _context.Bicicletas.FindAsync(reservacion.id_bicicleta);

        if (bicicleta != null)
        {
            bicicleta.estado = "libre";
            _context.Bicicletas.Update(bicicleta);
        }

        // Eliminar la reservación
        _context.Reservaciones.Remove(reservacion);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservación eliminada con éxito" });
    }


    [HttpGet("historial")]
    public async Task<IActionResult> ObtenerHistorialReservaciones([FromHeader] string sessionToken)
    {
        // Validar el token de sesión y obtener el usuario correspondiente
        var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.sessionToken == sessionToken);

        if (usuario == null)
        {
            return Unauthorized("Token inválido. Acceso no permitido.");
        }

        // Variables para almacenar las reservaciones detalladas
        var reservacionesActuales = new List<object>();
        var historialReservaciones = new List<object>();

        if (usuario.tipo_usuario == "secretaria")
        {
            // Secretaria: obtiene todas las reservaciones actuales
            // Se realiza un JOIN entre las tablas Reservaciones, Bicicletas y Horarios:
            // - Primer JOIN (Reservaciones con Bicicletas): permite obtener el nombre de la bicicleta asociada a la reservación.
            // - Segundo JOIN (Reservaciones con Horarios): permite obtener la hora de inicio del horario asociado.
            reservacionesActuales = _context.Reservaciones
                .Join(_context.Bicicletas, r => r.id_bicicleta, b => b.id_bicicleta, (r, b) => new { r, b })
                .Join(_context.Horarios, rb => rb.r.id_horario, h => h.id_horario, (rb, h) => new
                {
                    rb.r.id_reservacion,
                    rb.r.nombre,
                    rb.r.apellidos,
                    Bicicleta = rb.b.nombre_bicicleta,
                    Horario = $"{h.hora_inicio}",
                })
                .ToList<object>();

            // Obtiene todo el historial de reservaciones
            // Similar al caso de reservaciones actuales, pero opera sobre la tabla HistorialReservaciones
            // - Incluye el campo "estado" que indica el estado final de la reservación.
            historialReservaciones = _context.HistorialReservaciones
                .Join(_context.Bicicletas, h => h.id_bicicleta, b => b.id_bicicleta, (h, b) => new { h, b })
                .Join(_context.Horarios, hb => hb.h.id_horario, h => h.id_horario, (hb, h) => new
                {
                    hb.h.id_historial,
                    hb.h.nombre,
                    hb.h.apellidos,
                    Bicicleta = hb.b.nombre_bicicleta,
                    Horario = $"{h.hora_inicio}",
                    hb.h.estado
                })
                .ToList<object>();
        }
        else if (usuario.tipo_usuario == "cliente")
        {
            // Cliente: obtiene únicamente sus reservaciones actuales
            // - Filtra las reservaciones por el ID del cliente (id_cliente == usuario.id_usuario).
            // - Realiza JOINs similares al caso de secretaria para asociar Bicicletas y Horarios.
            reservacionesActuales = _context.Reservaciones
                .Where(r => r.id_cliente == usuario.id_usuario)
                .Join(_context.Bicicletas, r => r.id_bicicleta, b => b.id_bicicleta, (r, b) => new { r, b })
                .Join(_context.Horarios, rb => rb.r.id_horario, h => h.id_horario, (rb, h) => new
                {
                    rb.r.id_reservacion,
                    rb.r.nombre,
                    rb.r.apellidos,
                    Bicicleta = rb.b.nombre_bicicleta,
                    Horario = $"{h.hora_inicio}",
                })
                .ToList<object>();

            // Obtiene únicamente el historial de reservaciones del cliente actual
            historialReservaciones = _context.HistorialReservaciones
                .Where(h => h.id_cliente == usuario.id_usuario)
                .Join(_context.Bicicletas, h => h.id_bicicleta, b => b.id_bicicleta, (h, b) => new { h, b })
                .Join(_context.Horarios, hb => hb.h.id_horario, h => h.id_horario, (hb, h) => new
                {
                    hb.h.id_historial,
                    hb.h.nombre,
                    hb.h.apellidos,
                    Bicicleta = hb.b.nombre_bicicleta,
                    Horario = $"{h.hora_inicio}",
                    hb.h.estado
                })
                .ToList<object>();
        }
        else
        {
            // Si el usuario no tiene permisos, retorna un error 403 (Forbid)
            return Forbid("Acceso no permitido.");
        }

        // Combinar ambas listas en una respuesta unificada
        var resultado = new
        {
            ReservacionesActuales = reservacionesActuales,
            HistorialReservaciones = historialReservaciones
        };

        return Ok(resultado);
    }


    [HttpPost("mover-a-historial/{id}")]
    public async Task<IActionResult> MoverReservacionAHistorial(int id)
    {
        // Buscar la reservación por su ID
        var reservacion = await _context.Reservaciones.FindAsync(id);

        if (reservacion == null)
        {
            return NotFound("Reservación no encontrada.");
        }

        // Crear una nueva entrada para el historial
        var historial = new HistorialReservaciones
        {
            id_reservacion = reservacion.id_reservacion,
            id_cliente = reservacion.id_cliente,
            id_horario = reservacion.id_horario,
            id_bicicleta = reservacion.id_bicicleta,
            estado = "finalizado", // Marcar el estado en el historial como "finalizado"
            nombre = reservacion.nombre,
            apellidos = reservacion.apellidos,
            email = reservacion.email,
            talla = reservacion.talla
        };

        // Actualizar el estado de la bicicleta asociada a "libre"
        var bicicleta = await _context.Bicicletas.FindAsync(reservacion.id_bicicleta);
        if (bicicleta != null)
        {
            bicicleta.estado = "libre";
            _context.Bicicletas.Update(bicicleta);
        }

        // Agregar al historial y eliminar de la tabla principal
        _context.HistorialReservaciones.Add(historial);
        _context.Reservaciones.Remove(reservacion);

        // Guardar los cambios
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reservación movida al historial con éxito.", historial });
    }

}