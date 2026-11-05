' Estado global del usuario que inició sesión
Public Class Variable
    Public Shared Usuario As String = ""
    Public Shared Estado As String = ""   ' "Administrador" o "Invitado"

    Public Shared ReadOnly Property EsAdmin() As Boolean
        Get
            Return Estado = "Administrador"
        End Get
    End Property
End Class
