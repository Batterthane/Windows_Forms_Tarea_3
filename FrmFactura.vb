Imports System.Globalization
Imports System.IO

Public Class FrmFactura

    Private ReadOnly Cult As CultureInfo = CultureInfo.InvariantCulture
    Private ReadOnly dtDetalle As New DataTable()
    Private dtProductos As DataTable
    Private facturaId As Integer = 0          ' 0 = factura nueva (sin guardar)

    ' ---------- Carga ----------
    Private Sub FrmFactura_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        dtDetalle.Columns.Add("Codigo", GetType(Integer))
        dtDetalle.Columns.Add("Nombre", GetType(String))
        dtDetalle.Columns.Add("Cantidad", GetType(Integer))
        dtDetalle.Columns.Add("Precio", GetType(Decimal))
        dtDetalle.Columns.Add("Importe", GetType(Decimal))
        dgvDetalle.DataSource = dtDetalle

        For Each nombre As String In New String() {"Precio", "Importe"}
            With dgvDetalle.Columns(nombre).DefaultCellStyle
                .Format = "N2"
                .FormatProvider = Cult
                .Alignment = DataGridViewContentAlignment.MiddleRight
            End With
        Next
        dgvDetalle.Columns("Cantidad").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight

        txtUsuario.Text = Variable.Usuario
        lblItbisT.Text = "ITBIS (" & (MetodosFacturas.TasaItbis * 100D).ToString("0", Cult) & "%):"
        CargarProductos()
        NuevaFactura()
    End Sub

    ' Seguridad extra: solo el administrador factura (el botón ya está bloqueado para el invitado)
    Private Sub FrmFactura_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If Not Variable.EsAdmin Then
            MessageBox.Show("Solo el administrador puede crear facturas.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.Close()
        End If
    End Sub

    Private Sub FrmFactura_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If facturaId = 0 AndAlso dtDetalle.Rows.Count > 0 Then
            If MessageBox.Show("Hay una factura sin guardar. ¿Desea salir y perderla?", "Factura sin guardar",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                e.Cancel = True
            End If
        End If
    End Sub

    ' ---------- Atajo F5: imprimir ----------
    Private Sub FrmFactura_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.F5 Then
            e.Handled = True
            e.SuppressKeyPress = True
            Exportar()
        End If
    End Sub

    ' ---------- Productos ----------
    Private Sub CargarProductos()
        Try
            Dim t As DataTable = MetodosProductos.Listar()
            t.Columns.Add("Texto", GetType(String))
            For Each r As DataRow In t.Rows
                r("Texto") = r("Codigo").ToString() & " - " & r("Nombre").ToString() & " (" & r("Marca").ToString() & ")"
            Next
            dtProductos = t
            t.DefaultView.RowFilter = "Stock > 0"          ' solo productos con existencias
            cboProducto.DataSource = Nothing
            cboProducto.DisplayMember = "Texto"
            cboProducto.ValueMember = "Codigo"
            cboProducto.DataSource = t.DefaultView
            cboProducto.SelectedIndex = -1
        Catch ex As Exception
            MessageBox.Show("Error al cargar productos: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function ProductoSeleccionado() As DataRow
        If cboProducto.SelectedIndex < 0 AndAlso cboProducto.Text.Trim() <> "" Then
            Dim idx As Integer = cboProducto.FindStringExact(cboProducto.Text)
            If idx >= 0 Then cboProducto.SelectedIndex = idx
        End If
        If cboProducto.SelectedIndex < 0 Then Return Nothing
        Dim drv As DataRowView = TryCast(cboProducto.SelectedItem, DataRowView)
        If drv Is Nothing Then Return Nothing
        Return drv.Row
    End Function

    Private Sub cboProducto_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboProducto.SelectedIndexChanged
        Dim r As DataRow = Nothing
        Dim drv As DataRowView = TryCast(cboProducto.SelectedItem, DataRowView)
        If drv IsNot Nothing Then r = drv.Row
        If r Is Nothing Then
            lblInfo.Text = "Precio: -   Stock disponible: -"
        Else
            lblInfo.Text = "Precio: " & Convert.ToDecimal(r("Precio")).ToString("N2", Cult) & "   Stock disponible: " & r("Stock").ToString()
        End If
    End Sub

    ' ---------- Detalle ----------
    Private Sub btnAgregar_Click(sender As Object, e As EventArgs) Handles btnAgregar.Click
        If facturaId <> 0 Then Return

        Dim p As DataRow = ProductoSeleccionado()
        If p Is Nothing Then
            MessageBox.Show("Seleccione un producto de la lista.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            cboProducto.Focus()
            Return
        End If

        Dim cant As Integer = CInt(nudCantidad.Value)
        Dim codigo As Integer = Convert.ToInt32(p("Codigo"))
        Dim stock As Integer = Convert.ToInt32(p("Stock"))
        Dim precio As Decimal = Convert.ToDecimal(p("Precio"))

        Dim existentes As DataRow() = dtDetalle.Select("Codigo = " & codigo.ToString())
        Dim yaEnFactura As Integer = 0
        If existentes.Length > 0 Then yaEnFactura = Convert.ToInt32(existentes(0)("Cantidad"))

        If cant + yaEnFactura > stock Then
            MessageBox.Show("Stock insuficiente de """ & p("Nombre").ToString() & """." & vbCrLf &
                            "Disponible: " & stock & "   Ya en la factura: " & yaEnFactura,
                            "Stock insuficiente", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If existentes.Length > 0 Then
            existentes(0)("Cantidad") = cant + yaEnFactura
            existentes(0)("Importe") = (cant + yaEnFactura) * precio
        Else
            dtDetalle.Rows.Add(codigo, p("Nombre").ToString(), cant, precio, cant * precio)
        End If

        Recalcular()
        nudCantidad.Value = 1
        cboProducto.SelectedIndex = -1
        cboProducto.Focus()
    End Sub

    Private Sub btnQuitar_Click(sender As Object, e As EventArgs) Handles btnQuitar.Click
        If facturaId <> 0 OrElse dgvDetalle.CurrentRow Is Nothing Then Return
        Dim drv As DataRowView = TryCast(dgvDetalle.CurrentRow.DataBoundItem, DataRowView)
        If drv Is Nothing Then Return
        dtDetalle.Rows.Remove(drv.Row)
        Recalcular()
    End Sub

    Private Sub Recalcular()
        Dim subtotal As Decimal = 0D
        For Each r As DataRow In dtDetalle.Rows
            subtotal += Convert.ToDecimal(r("Importe"))
        Next
        Dim itbis As Decimal = Math.Round(subtotal * MetodosFacturas.TasaItbis, 2, MidpointRounding.AwayFromZero)
        lblSubtotal.Text = subtotal.ToString("N2", Cult)
        lblItbis.Text = itbis.ToString("N2", Cult)
        lblTotal.Text = (subtotal + itbis).ToString("N2", Cult)
    End Sub

    ' ---------- Guardar ----------
    Private Function Guardar() As Boolean
        If facturaId <> 0 Then Return True
        If dtDetalle.Rows.Count = 0 Then
            MessageBox.Show("Agregue al menos un artículo a la factura.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If

        ' La base de datos solo recibe código y cantidad; el precio lo toma ella misma
        Dim tvp As New DataTable()
        tvp.Columns.Add("Codigo", GetType(Integer))
        tvp.Columns.Add("Cantidad", GetType(Integer))
        For Each r As DataRow In dtDetalle.Rows
            tvp.Rows.Add(Convert.ToInt32(r("Codigo")), Convert.ToInt32(r("Cantidad")))
        Next

        Try
            Dim id As Integer = MetodosFacturas.Crear(txtCliente.Text.Trim(), Variable.Usuario, tvp)
            facturaId = id
            CargarFacturaGuardada(id)
            Bloquear(True)
            CargarProductos()          ' el stock cambió
            Return True
        Catch ex As Exception
            MessageBox.Show("No se pudo guardar la factura: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            CargarProductos()
            Return False
        End Try
    End Function

    ' Muestra los datos tal como quedaron en la base de datos
    Private Sub CargarFacturaGuardada(id As Integer)
        Dim ds As DataSet = MetodosFacturas.Obtener(id)
        Dim cab As DataRow = ds.Tables(0).Rows(0)
        txtNumero.Text = cab("Numero").ToString()
        txtFecha.Text = Convert.ToDateTime(cab("Fecha")).ToString("dd/MM/yyyy HH:mm", Cult)
        txtCliente.Text = cab("Cliente").ToString()

        dtDetalle.Rows.Clear()
        For Each r As DataRow In ds.Tables(1).Rows
            dtDetalle.Rows.Add(Convert.ToInt32(r("Codigo")), r("Nombre").ToString(), Convert.ToInt32(r("Cantidad")),
                               Convert.ToDecimal(r("Precio")), Convert.ToDecimal(r("Importe")))
        Next
        lblSubtotal.Text = Convert.ToDecimal(cab("Subtotal")).ToString("N2", Cult)
        lblItbis.Text = Convert.ToDecimal(cab("Itbis")).ToString("N2", Cult)
        lblTotal.Text = Convert.ToDecimal(cab("Total")).ToString("N2", Cult)
    End Sub

    Private Sub Bloquear(valor As Boolean)
        cboProducto.Enabled = Not valor
        nudCantidad.Enabled = Not valor
        btnAgregar.Enabled = Not valor
        btnQuitar.Enabled = Not valor
        btnGuardar.Enabled = Not valor
        txtCliente.ReadOnly = valor
    End Sub

    Private Sub btnGuardar_Click(sender As Object, e As EventArgs) Handles btnGuardar.Click
        If dtDetalle.Rows.Count = 0 Then
            MessageBox.Show("Agregue al menos un artículo a la factura.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If MessageBox.Show("¿Guardar la factura? Se descontará el stock de los productos vendidos.", "Confirmar venta",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        If Guardar() Then
            MessageBox.Show("Factura " & txtNumero.Text & " guardada." & vbCrLf & "Total: " & lblTotal.Text &
                            vbCrLf & vbCrLf & "Pulse F5 para imprimirla.", "Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ' ---------- Nueva / Cerrar ----------
    Private Sub NuevaFactura()
        facturaId = 0
        dtDetalle.Rows.Clear()
        txtNumero.Text = "(nueva)"
        txtFecha.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cult)
        txtCliente.Text = "Consumidor final"
        nudCantidad.Value = 1
        Bloquear(False)
        Recalcular()
        cboProducto.SelectedIndex = -1
    End Sub

    Private Sub btnNueva_Click(sender As Object, e As EventArgs) Handles btnNueva.Click
        If facturaId = 0 AndAlso dtDetalle.Rows.Count > 0 Then
            If MessageBox.Show("La factura actual no está guardada. ¿Descartarla?", "Nueva factura",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        End If
        NuevaFactura()
    End Sub

    Private Sub btnCerrar_Click(sender As Object, e As EventArgs) Handles btnCerrar.Click
        Me.Close()
    End Sub

    ' ---------- Imprimir (botón o F5) a .pdf o .txt ----------
    Private Sub btnImprimir_Click(sender As Object, e As EventArgs) Handles btnImprimir.Click
        Exportar()
    End Sub

    Private Sub Exportar()
        If facturaId = 0 Then
            If dtDetalle.Rows.Count = 0 Then
                MessageBox.Show("Agregue artículos a la factura antes de imprimir.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If MessageBox.Show("La factura aún no está guardada." & vbCrLf & "¿Guardarla ahora (se descuenta el stock) y exportarla?",
                               "Imprimir factura", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            If Not Guardar() Then Return
        End If

        Try
            Dim ds As DataSet = MetodosFacturas.Obtener(facturaId)
            Dim cab As DataRow = ds.Tables(0).Rows(0)

            Using sfd As New SaveFileDialog()
                sfd.Title = "Guardar factura"
                sfd.Filter = "Documento PDF (*.pdf)|*.pdf|Archivo de texto (*.txt)|*.txt"
                sfd.FileName = cab("Numero").ToString()
                If sfd.ShowDialog(Me) <> DialogResult.OK Then Return

                Dim ruta As String = sfd.FileName
                Dim ext As String = Path.GetExtension(ruta).ToLowerInvariant()
                If ext <> ".pdf" AndAlso ext <> ".txt" Then
                    ruta &= If(sfd.FilterIndex = 1, ".pdf", ".txt")
                End If

                ExportadorFactura.Guardar(ruta, cab, ds.Tables(1))

                If MessageBox.Show("Factura guardada en:" & vbCrLf & ruta & vbCrLf & vbCrLf & "¿Desea abrirla?", "Listo",
                                   MessageBoxButtons.YesNo, MessageBoxIcon.Information) = DialogResult.Yes Then
                    System.Diagnostics.Process.Start(ruta)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("No se pudo exportar la factura: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
