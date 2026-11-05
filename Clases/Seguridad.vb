Imports System.Data
Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text

Public Class Seguridad
    ' SHA256 en hexadecimal mayúscula (64 caracteres)
    Public Shared Function Hash(texto As String) As String
        Using sha As SHA256 = SHA256.Create()
            Dim bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto))
            Dim sb As New StringBuilder()
            For Each b In bytes
                sb.Append(b.ToString("X2"))
            Next
            Return sb.ToString()
        End Using
    End Function

    ' Devuelve el Estado ("Administrador"/"Invitado") o "" si las credenciales son incorrectas
    Public Shared Function Validar(usuario As String, clave As String) As String
        Using cn = Conexion.Obtener(), cmd As New SqlCommand("dbo._iniciosesion", cn)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@usuario", usuario)
            cmd.Parameters.AddWithValue("@clave", Hash(clave))
            cn.Open()
            Dim r = cmd.ExecuteScalar()
            Return If(r Is Nothing, "", r.ToString())
        End Using
    End Function
End Class
