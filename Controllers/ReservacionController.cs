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
public async Task<IActionResult> Reservar([FromBody] Reservacion nuevaReservacion)
{
    // Verificar el tipo de usuario en el cuerpo de la solicitud
    var usuario = await _context.Usuario.FindAsync(nuevaReservacion.id_cliente);

    if (usuario == null)
    {
        return NotFound("Usuario no encontrado.");
    }

    // Verificar el tipo de usuario
    if (usuario.tipo_usuario == "cliente")
    {
        // Si es un cliente, se ignora el id_cliente que viene en el cuerpo y se utiliza su propio ID
        nuevaReservacion.id_cliente = usuario.id_usuario;
        nuevaReservacion.nombre = usuario.nombre;
        nuevaReservacion.apellidos = usuario.apellidos;
        nuevaReservacion.email = usuario.email;
    }
    else if (usuario.tipo_usuario == "secretaria")
    {
        // Si es secretaria, se permite agregar el id_cliente manualmente
        var cliente = _context.Usuario.Find(nuevaReservacion.id_cliente);

        if (cliente == null)
        {
            return NotFound("Cliente no encontrado.");
        }

        nuevaReservacion.nombre = cliente.nombre;
        nuevaReservacion.apellidos = cliente.apellidos;
        nuevaReservacion.email = cliente.email;
    }
    else
    {
        return Forbid("Acceso no permitido.");
    }

    // Guardar la reservación en la base de datos
    _context.Reservaciones.Add(nuevaReservacion);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Reservación realizada con éxito", nuevaReservacion });
}

[HttpPut("editar-reservacion/{id}")]
public async Task<IActionResult> EditarReservacion(int id, [FromBody] Reservacion reservacionEditada)
{
    // Buscar la reservación por su ID
    var reservacion = await _context.Reservaciones.FindAsync(id);

    if (reservacion == null)
    {
        return NotFound("Reservación no encontrada.");
    }
    
    // Dependiendo del tipo de usuario, se ajustan los datos permitidos
    var usuario = await _context.Usuario.FindAsync(reservacion.id_cliente);

    if (usuario != null && usuario.tipo_usuario == "secretaria")
    {
        // Solo las secretarias pueden cambiar los datos del cliente si es necesario
        reservacion.nombre = reservacionEditada.nombre;
        reservacion.apellidos = reservacionEditada.apellidos;
        reservacion.email = reservacionEditada.email;
        reservacion.talla = reservacionEditada.talla;
        // Actualizar los datos de la reservación
        reservacion.id_horario = reservacionEditada.id_horario;
        reservacion.id_bicicleta = reservacionEditada.id_bicicleta;
    }

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

    // Eliminar la reservación
    _context.Reservaciones.Remove(reservacion);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Reservación eliminada con éxito" });
}

[HttpGet("historial")]
public IActionResult ObtenerHistorialReservaciones(string tipo_usuario, int? id_cliente)
{
    List<Reservacion> reservaciones;

    if (tipo_usuario == "secretaria")
    {
        // Secretaria: obtiene todas las reservaciones
        reservaciones = _context.Reservaciones.ToList();
    }
    else if (tipo_usuario == "cliente" && id_cliente != null)
    {
        // Cliente: obtiene solo sus reservaciones
        reservaciones = _context.Reservaciones
            .Where(r => r.id_cliente == id_cliente)
            .ToList();
    }
    else
    {
        return BadRequest("Tipo de usuario o ID del cliente no válidos.");
    }

    return Ok(reservaciones);
}

}