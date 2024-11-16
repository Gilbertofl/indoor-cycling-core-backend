using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class HorariosController : ControllerBase
{
    private readonly GimnasioContext _context;

    public HorariosController(GimnasioContext context)
    {
        _context = context;
    }

    [HttpPost("agregar")]
    public async Task<IActionResult> AgregarHorario([FromBody] Horarios nuevoHorario, [FromQuery] string tipoUsuario)
    {
        // Simular la verificación del tipo de usuario
        if (tipoUsuario != "secretaria")
        {
            return Forbid("Acceso no permitido. Solo la secretaria puede registrar horarios.");
        }

        // Agregar el nuevo horario a la base de datos
        _context.Horarios.Add(nuevoHorario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Horario agregado con éxito", nuevoHorario });
    }

    [HttpPut("editar/{id}")]
    public async Task<IActionResult> EditarHorario(int id, [FromBody] Horarios horarioEditado, [FromQuery] string tipoUsuario)
    {
        // Verificar si el tipo de usuario es "secretaria"
        if (tipoUsuario != "secretaria")
        {
            return Forbid("Acceso no permitido. Solo la secretaria puede editar horarios.");
        }

        // Buscar el horario por ID
        var horario = await _context.Horarios.FindAsync(id);
        if (horario == null)
        {
            return NotFound("Horario no encontrado.");
        }

        // Actualizar los detalles del horario
        horario.hora_inicio = horarioEditado.hora_inicio;

        // Guardar cambios en la base de datos
        await _context.SaveChangesAsync();

        return Ok(new { message = "Horario editado con éxito", horario });
    }

    [HttpDelete("eliminar/{id}")]
    public async Task<IActionResult> EliminarHorario(int id, [FromQuery] string tipoUsuario)
    {
        // Verificar si el tipo de usuario es "secretaria"
        if (tipoUsuario != "secretaria")
        {
            return Forbid("Acceso no permitido. Solo la secretaria puede eliminar horarios.");
        }

        // Buscar el horario por ID
        var horario = await _context.Horarios.FindAsync(id);
        if (horario == null)
        {
            return NotFound("Horario no encontrado.");
        }

        // Eliminar el horario
        _context.Horarios.Remove(horario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Horario eliminado con éxito" });
    }


}