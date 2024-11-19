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
    public async Task<IActionResult> AgregarHorario([FromBody] Horarios nuevoHorario, [FromHeader] string sessionToken)
    {   
        // Verifica si el token es valido
        var usuario = _context.Usuario.FirstOrDefault(u => u.sessionToken == sessionToken);
        // Simular la verificación del tipo de usuario
        if ( usuario == null || usuario.tipo_usuario != "secretaria")
        {
            return Forbid("Acceso no permitido. Solo la secretaria puede registrar horarios.");
        }

        nuevoHorario.hora_inicio = TimeSpan.ParseExact(nuevoHorario.hora_inicio.ToString(), @"hh\:mm\:ss", null);

        // Agregar el nuevo horario a la base de datos
        _context.Horarios.Add(nuevoHorario);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Horario agregado con éxito", nuevoHorario });
    }

    [HttpPut("editar/{id}")]
    public async Task<IActionResult> EditarHorario(int id, [FromBody] Horarios horarioEditado, [FromHeader] string sessionToken)
    {
        // Verifica si el token es valido
        var usuario = _context.Usuario.FirstOrDefault(u => u.sessionToken == sessionToken);

        // Verificar si el tipo de usuario es "secretaria"
        if (usuario == null || usuario.tipo_usuario != "secretaria")
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
        horario.hora_inicio = TimeSpan.ParseExact(horarioEditado.hora_inicio.ToString(), @"hh\:mm\:ss", null);

        // Guardar cambios en la base de datos
        await _context.SaveChangesAsync();

        return Ok(new { message = "Horario editado con éxito", horario });
    }

    [HttpDelete("eliminar/{id}")]
    public async Task<IActionResult> EliminarHorario(int id, [FromHeader] string sessionToken)
    {   
        // Verifica si el token es valido
        var usuario = _context.Usuario.FirstOrDefault(u => u.sessionToken == sessionToken);

        // Verificar si el tipo de usuario es "secretaria"
        if (usuario == null || usuario.tipo_usuario != "secretaria")
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

    // Metodo para obtener los horarios
    [HttpGet("listar")]
    public async Task<ActionResult<IEnumerable<Horarios>>> ListarHorarios()
    {
        // obtene todos los horarios de la base de datos
        var horarios = await _context.Horarios.ToListAsync();

        // verifica si hay horaios registrados
        if(horarios == null || !horarios.Any())
        {
            return NotFound("No se encontraron horarios");
        }

        // Formatear el tiempo antes de enviarlo
        var resultado = horarios.Select(h => new 
        {
            h.id_horario,
            hora_inicio = h.hora_inicio.ToString(@"hh\:mm") // Formato HH:mm
        });

        // retorna la lista de horarios
        return Ok(resultado);
    }

    //Metodo para obtener el horario por ID
    [HttpGet("detalle/{id}")]
    public async Task<ActionResult<Horarios>> ObtenerHorario(int id)
    {
        // Busca el horario por ID
        var horario = await _context.Horarios.FindAsync(id);

        // verifica si el horario existe
        if(horario == null)
        {
            return NotFound("Horario no encontrado");
        }

        return Ok(horario);
    }
}