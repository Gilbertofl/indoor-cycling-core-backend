using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly GimnasioContext _context;

    // Constructor que inicializa el contexto de la base de datos
    public UsuariosController(GimnasioContext context)
    {
        _context = context;
    }

    // Método para registrar un nuevo usuario
    [HttpPost("registrar")]
    public async Task<ActionResult<Usuario>> RegistrarUsuario([FromBody] Usuario usuario)
    {
        // Verificar si los datos del usuario son válidos
        if (usuario == null)
        {
            return BadRequest("Datos del usuario son inválidos.");
        }

        // Verificar si la contraseña es proporcionada
        if (string.IsNullOrEmpty(usuario.password))
        {
            return BadRequest("La contraseña es requerida.");
        }

        // Agregar el nuevo usuario al contexto
        _context.Usuario.Add(usuario);
        await _context.SaveChangesAsync(); // Guardar cambios en la base de datos

        // Retornar una respuesta de creación con la ubicación del nuevo usuario
        return CreatedAtAction(nameof(RegistrarUsuario), new { id = usuario.id_usuario }, usuario);
    }

    // Método para iniciar sesión de un usuario
    [HttpPost("login")]
    public IActionResult Login([FromBody] usuarioLogin loginUsuario)
    {
        // Buscar el usuario por su email
        var usuario = _context.Usuario
            .FirstOrDefault(u => u.email == loginUsuario.email);

        // Verificar si el usuario existe y si la contraseña es correcta
        if (usuario == null || usuario.password != loginUsuario.password)
        {
            return Unauthorized("Credenciales incorrectas");
        }

        // Genera un token nuevo en cada sesion
        var sessionToken = Guid.NewGuid().ToString();

        // Guardar el nuevo sessionToken en la base de datos
        usuario.sessionToken = sessionToken;
        _context.SaveChanges();

        // Retornar un mensaje de éxito al iniciar sesión
        return Ok(new { message = "Sesión iniciada", 
            usuario = new {
                id = usuario.id_usuario,
                email = usuario.email,
                tipo_usuario = usuario.tipo_usuario
            }, sessionToken
        });
    }

    // Método para editar un usuario existente
    [HttpPut("editar/{id}")]
    public async Task<IActionResult> EditarUsuario(int id, [FromBody] Usuario usuarioEdit)
    {
        // Buscar el usuario por su ID
        var usuario = await _context.Usuario.FindAsync(id);

        // Verificar si el usuario existe
        if (usuario == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        // Actualizar los datos del usuario
        usuario.nombre = usuarioEdit.nombre;
        usuario.apellidos = usuarioEdit.apellidos;
        usuario.edad = usuarioEdit.edad;
        usuario.email = usuarioEdit.email;
        usuario.password = usuarioEdit.password;
        usuario.telefono = usuarioEdit.telefono;
        usuario.tipo_usuario = usuarioEdit.tipo_usuario;

        // Guardar los cambios en la base de datos
        await _context.SaveChangesAsync();

        // Retornar el usuario actualizado
        return Ok(usuario);
    }

    // Método para borrar un usuario existente
    [HttpDelete("borrar/{id}")]
    public async Task<IActionResult> BorrarUsuario(int id)
    {
        // Buscar el usuario por su ID
        var usuario = await _context.Usuario.FindAsync(id);

        // Verificar si el usuario existe
        if (usuario == null)
        {
            return NotFound("Usuario no encontrado");
        }

        // Eliminar el usuario del contexto
        _context.Usuario.Remove(usuario);
        await _context.SaveChangesAsync(); // Guardar cambios en la base de datos

        // Retornar un mensaje de éxito
        return Ok(new { message = "Usuario eliminado" });
    }

    // Metodo para obtener los usuarios
    [HttpGet("listar")]
    public async Task<ActionResult<IEnumerable<Usuario>>> ListarUsuarios()
    {
        //obtene todos los horarios de la base de datos
        var usuarios = await _context.Usuario.ToListAsync();

        // verifica si hay perfiles registrados
        if(usuarios == null || !usuarios.Any()){
            return NotFound("No se encontraron usuarios");
        }

        // retorna la lista de horarios
        return Ok(usuarios);
    }

    // Metodo para obtener el usuario por ID
    [HttpGet("detalle/{id}")]
    public async Task<ActionResult<Usuario>> ObtenerUsuario(int id)
    {
        // Busca el usuario por ID
        var usuario = await _context.Usuario.FindAsync(id);

        // verifica si el usuario existe
        if(usuario == null){
            return NotFound("Usuario no encontrado");
        }

        return Ok(usuario);
    }

    [HttpPost("logout")]
    public IActionResult Logout([FromHeader] string sessionToken)
    {
        var usuario = _context.Usuario.FirstOrDefault(u => u.sessionToken == sessionToken);
        if (usuario == null)
        {
            return Unauthorized("Token inválido.");
        }

        // Invalidar el token
        usuario.sessionToken = null;
        _context.SaveChanges();

        return Ok(new { message = "Sesión cerrada correctamente." });
    }
    
}
