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