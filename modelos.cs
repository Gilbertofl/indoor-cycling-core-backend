//Usuarios
public class Usuario{
    public int id_usuario { get; set; }
    public required string nombre { get; set; } 
    public required string apellidos { get; set; }
    public int edad { get; set; } 
    public required string email { get; set; } 
    public required string password { get; set; }
    public string telefono { get; set; }
    public required string tipo_usuario { get; set; }
    public string sessionToken { get; set; }
    
}
public class usuarioLogin{
    public required string email { get; set; } 
    public required string password { get; set; }
}


//Reservacion
public class Reservacion{
    public int id_reservacion { get; set; }
    public int id_cliente { get; set; }
    public required string nombre { get; set; }
    public required string apellidos { get; set; }
    public required string talla { get; set; }
    public required string email { get; set; }
    public int id_horario { get; set; }
    public int id_bicicleta { get; set; }
}


//Bicicletas
public class Bicicletas{
    public int id_bicicleta { get; set; }
    public required string nombre_bicicleta { get; set; }
    public required string estado { get; set; } 
}
public class bicicletaEdit{
    public required string nombre_bicicleta { get; set; }
    public required string estado { get; set; } 
}


//Horarios
public class Horarios{
    public int id_horario { get; set; }
    public TimeSpan hora_inicio { get; set; }
}


//membresias
public class Membresias{
    public int id_membresia { get; set; }
    public int id_cliente { get; set; }
    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_expiracion { get; set; }
    public string estado { get; set; }
}



