using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class MembresiasController : ControllerBase
{
    
    private readonly GimnasioContext _context;

    public MembresiasController(GimnasioContext context)
    {
        _context = context;
    }

    [HttpPost("membresia/registrar")]
    public async Task<IActionResult> RegistrarMembresia([FromBody] Membresias nuevaMembresia, [FromQuery] string tipoUsuario)
    {

        if (tipoUsuario != "secretaria")
        {
            return Forbid("Acceso no permitido.");
        }
        // Validar que el cliente existe
        var cliente = await _context.Usuario.FindAsync(nuevaMembresia.id_cliente);
        if (cliente == null)
        {
            return NotFound("Cliente no encontrado.");
        }

        nuevaMembresia.fecha_inicio = DateTime.UtcNow;
        nuevaMembresia.fecha_expiracion = DateTime.UtcNow.AddMonths(1); // Ejemplo de 1 mes de membresía
        nuevaMembresia.estado = "Activa";

        // Agregar la nueva membresía
        _context.Membresias.Add(nuevaMembresia);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Membresía registrada con éxito", nuevaMembresia });
    }

    [HttpPut("membresia/editar/{id}")]
    public async Task<IActionResult> EditarMembresia(int id, [FromBody] Membresias membresiaActualizada, [FromQuery] string tipoUsuario)
    {
        // Verificar si la secretaria está haciendo la solicitud
        var usuarioAutenticado = _context.Usuario
            .FirstOrDefault(u => u.email == User.Identity.Name);

        if (tipoUsuario != "secretaria")
        {
            return Forbid("Acceso no permitido.");
        }

        // Buscar la membresía existente
        var membresiaExistente = await _context.Membresias.FindAsync(id);
        if (membresiaExistente == null)
        {
            return NotFound("Membresía no encontrada.");
        }

        // Actualizar los datos de la membresía
        membresiaExistente.fecha_inicio = membresiaActualizada.fecha_inicio;
        membresiaExistente.fecha_expiracion = membresiaActualizada.fecha_expiracion;
        membresiaExistente.estado = membresiaActualizada.estado;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Membresía actualizada con éxito", membresiaExistente });
    }

    [HttpDelete("membresia/eliminar/{id}")]
    public async Task<IActionResult> EliminarMembresia(int id, [FromQuery] string tipoUsuario)
    {
        // Verificar si la secretaria está haciendo la solicitud
        var usuarioAutenticado = _context.Usuario
            .FirstOrDefault(u => u.email == User.Identity.Name);

        if (tipoUsuario != "secretaria")
        {
            return Forbid("Acceso no permitido.");
        }

        // Buscar la membresía existente
        var membresiaExistente = await _context.Membresias.FindAsync(id);
        if (membresiaExistente == null)
        {
            return NotFound("Membresía no encontrada.");
        }

        // Eliminar la membresía
        _context.Membresias.Remove(membresiaExistente);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Membresía eliminada con éxito" });
    }



}