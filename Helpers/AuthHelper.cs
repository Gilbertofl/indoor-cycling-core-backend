using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public static class AuthHelper
{
    public static async Task<(Usuario? usuario, IActionResult? errorResult)> ValidarSesionAsync(
        GimnasioContext context,
        string? sessionToken,
        string[]? rolesPermitidos = null)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return (null, new UnauthorizedObjectResult(new { message = "Se requiere el header 'sessionToken' para esta operación." }));
        }

        var usuario = await context.Usuario.FirstOrDefaultAsync(u => u.sessionToken == sessionToken);
        if (usuario == null)
        {
            return (null, new UnauthorizedObjectResult(new { message = "Token de sesión inválido o expirado." }));
        }

        if (rolesPermitidos != null && rolesPermitidos.Length > 0)
        {
            if (!rolesPermitidos.Contains(usuario.tipo_usuario, StringComparer.OrdinalIgnoreCase))
            {
                return (usuario, new ObjectResult(new { message = $"Acceso denegado. Se requiere uno de los siguientes roles: {string.Join(", ", rolesPermitidos)}." })
                {
                    StatusCode = 403
                });
            }
        }

        return (usuario, null);
    }
}
