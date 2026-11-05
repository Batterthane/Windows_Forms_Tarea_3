Imports System.Data
Imports System.Data.SqlClient

Public Class MetodosFacturas

    ' Debe ser igual a la tasa usada en dbo.SP_CREA_FACTURA
    Public Const TasaItbis As Decimal = 0.18D

    ' detalle: tabla con SOLO dos columnas, en este orden: Codigo (Int32) y Cantidad (Int32)
    Public Shared Function Crear(cliente As String, usuario As String, detalle As DataTable) As Integer
        Using cn = Conexion.Obtener(), cmd As New SqlCommand("dbo.SP_CREA_FACTURA", cn)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@cliente", cliente)
            cmd.Parameters.AddWithValue("@usuario", usuario)
            Dim p As SqlParameter = cmd.Parameters.AddWithValue("@detalle", detalle)
            p.SqlDbType = SqlDbType.Structured
            p.TypeName = "dbo.TipoDetalleFactura"
            cn.Open()
            Return Convert.ToInt32(cmd.ExecuteScalar())
        End Using
    End Function

    ' Devuelve un DataSet: Tables(0) = cabecera (1 fila), Tables(1) = detalle
    Public Shared Function Obtener(id As Integer) As DataSet
        Using cn = Conexion.Obtener(), cmd As New SqlCommand("dbo.SP_OBTENER_FACTURA", cn)
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@id", id)
            Dim ds As New DataSet()
            Using da As New SqlDataAdapter(cmd)
                da.Fill(ds)
            End Using
            Return ds
        End Using
    End Function
End Class
