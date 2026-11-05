Imports System.Configuration
Imports System.Data.SqlClient

Public Class Conexion

    ' Arma la cadena de conexión a partir de <appSettings> en App.config
    Public Shared Function Cadena() As String
        Dim b As New SqlConnectionStringBuilder()
        b.DataSource = ConfigurationManager.AppSettings("Servidor")
        b.InitialCatalog = ConfigurationManager.AppSettings("BaseDeDatos")

        Dim usuario As String = ConfigurationManager.AppSettings("Usuario")
        If String.IsNullOrWhiteSpace(usuario) Then
            b.IntegratedSecurity = True          ' autenticación de Windows
        Else
            b.UserID = usuario
            b.Password = ConfigurationManager.AppSettings("Contrasena")
        End If
        Return b.ConnectionString
    End Function

    Public Shared Function Obtener() As SqlConnection
        Return New SqlConnection(Cadena())
    End Function
End Class
