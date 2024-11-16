using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class BicicletasController : ControllerBase
{
   private readonly GimnasioContext _context;

   // Constructor que inicializa el contexto de la base de datos
   public BicicletasController(GimnasioContext context)
   {
       _context = context;
   }

   // Método para agregar una nueva bicicleta
   [HttpPost("agregar")]
   public async Task<ActionResult<Bicicletas>> AgregarBicicleta([FromBody] Bicicletas bicicleta)
   {
       // Verificar si los datos de la bicicleta son válidos
       if (bicicleta == null)
       {
           return BadRequest("Datos de la bicicleta inválidos.");
       }

       // Agregar la bicicleta al contexto
       _context.Bicicletas.Add(bicicleta);
       await _context.SaveChangesAsync(); // Guardar cambios en la base de datos

       // Retornar una respuesta de creación con la ubicación de la nueva bicicleta
       return CreatedAtAction(nameof(AgregarBicicleta), new { id = bicicleta.id_bicicleta }, bicicleta);
   }

   // Método para editar una bicicleta existente
   [HttpPut("editar/{id}")]
   public async Task<IActionResult> EditarBicicleta(int id, [FromBody] bicicletaEdit bicicletaActualizada)
   {
       // Buscar la bicicleta por su ID
       var bicicleta = await _context.Bicicletas.FindAsync(id);

       // Verificar si la bicicleta existe
       if (bicicleta == null)
       {
           return NotFound("Bicicleta no encontrada.");
       }

       // Actualizar los datos de la bicicleta
       bicicleta.nombre_bicicleta = bicicletaActualizada.nombre_bicicleta;
       bicicleta.estado = bicicletaActualizada.estado;

       // Guardar los cambios en la base de datos
       await _context.SaveChangesAsync();

       // Retornar la bicicleta actualizada
       return Ok(bicicleta);
   }

   // Método para borrar una bicicleta existente
   [HttpDelete("borrar/{id}")]
   public async Task<IActionResult> BorrarBicicleta(int id)
   {
       // Buscar la bicicleta por su ID
       var bicicleta = await _context.Bicicletas.FindAsync(id);

       // Verificar si la bicicleta existe
       if (bicicleta == null)
       {
           return NotFound("Bicicleta no encontrada.");
       }

       // Eliminar la bicicleta del contexto
       _context.Bicicletas.Remove(bicicleta);
       await _context.SaveChangesAsync(); // Guardar cambios en la base de datos

       // Retornar un mensaje de éxito
       return Ok(new { message = "Bicicleta eliminada" });
   }
}
