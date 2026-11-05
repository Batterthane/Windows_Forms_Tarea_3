Imports System.Data
Imports System.Data.SqlClient

Public Class MetodosProductos

    Private Shared Function Tabla(sp As String, ParamArray p() As SqlParameter) As DataTable
        Using cn = Conexion.Obtener(), cmd As New SqlCommand(sp, cn)
            cmd.CommandType = CommandType.StoredProcedure
            If p IsNot Nothing Then cmd.Parameters.AddRange(p)
            Dim dt As New DataTable()
            Using da As New SqlDataAdapter(cmd)
                da.Fill(dt)
            End Using
            Return dt
        End Using
    End Function

    Private Shared Function Escalar(sp As String, ParamArray p() As SqlParameter) As Integer
        Using cn = Conexion.Obtener(), cmd As New SqlCommand(sp, cn)
            cmd.CommandType = CommandType.StoredProcedure
            If p IsNot Nothing Then cmd.Parameters.AddRange(p)
            cn.Open()
            Return Convert.ToInt32(cmd.ExecuteScalar())
        End Using
    End Function

    Public Shared Function Listar() As DataTable
        Return Tabla("dbo.Sp_ListarProductos")
    End Function

    Public Shared Function Buscar(campo As String, valor As String) As DataTable
        Return Tabla("dbo.SP_BUSCA_PRODUCTO",
                     New SqlParameter("@campo", campo),
                     New SqlParameter("@valor", valor))
    End Function

    Public Shared Function PorCodigo(codigo As Integer) As DataTable
        Return Tabla("dbo.SP_BuscarProductoPorCodigo1", New SqlParameter("@codigo", codigo))
    End Function

    Public Shared Function SiguienteCodigo() As Integer
        Return Escalar("dbo.SP_SiguienteCodigo")
    End Function

    Public Shared Function Insertar(nombre As String, precio As Decimal, stock As Integer, marca As String) As Integer
        Return Escalar("dbo.SP_INSERTA_PRODUCTO",
                       New SqlParameter("@nombre", nombre),
                       New SqlParameter("@precio", precio),
                       New SqlParameter("@stock", stock),
                       New SqlParameter("@marca", marca))
    End Function

    Public Shared Function Actualizar(codigo As Integer, nombre As String, precio As Decimal, stock As Integer, marca As String) As Integer
        Return Escalar("dbo.SP_ACTUALIZA_PRODUCTO",
                       New SqlParameter("@codigo", codigo),
                       New SqlParameter("@nombre", nombre),
                       New SqlParameter("@precio", precio),
                       New SqlParameter("@stock", stock),
                       New SqlParameter("@marca", marca))
    End Function

    Public Shared Function Eliminar(codigo As Integer) As Integer
        Return Escalar("dbo.SP_EliminarProducto", New SqlParameter("@codigo", codigo))
    End Function
End Class
