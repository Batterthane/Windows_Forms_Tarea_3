Imports System.Globalization

Public Class FrmProductos

    ' ---------- Carga y permisos ----------
    Private Sub FrmProductos_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        cboCampo.Items.AddRange(New Object() {"Codigo", "Nombre", "Marca"})
        cboCampo.SelectedIndex = 0

        Me.Text = "Registro de Productos - " & Variable.Usuario & " (" & Variable.Estado & ")"

        ' Invitado: solo lectura
        Dim admin As Boolean = Variable.EsAdmin
        btnNuevo.Enabled = admin
        btnActualizar.Enabled = admin
        btnInsertar.Enabled = admin
        btnEliminar.Enabled = admin
        btnFacturar.Enabled = admin        ' solo el administrador factura

        HabilitarCampos(False)
        CargarGrid()
    End Sub

    Private Sub FrmProductos_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        Application.Exit()
    End Sub

    ' ---------- Utilidades ----------
    Private Sub HabilitarCampos(valor As Boolean)
        txtNombre.Enabled = valor
        txtPrecio.Enabled = valor
        txtStock.Enabled = valor
        txtMarca.Enabled = valor
    End Sub

    Private Sub Limpiar()
        txtCodigo.Clear()
        txtNombre.Clear()
        txtPrecio.Clear()
        txtStock.Clear()
        txtMarca.Clear()
    End Sub

    Private Sub CargarGrid()
        Try
            dgvProductos.DataSource = MetodosProductos.Listar()
        Catch ex As Exception
            MessageBox.Show("Error al cargar productos: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function LeerCampos(ByRef precio As Decimal, ByRef stock As Integer) As Boolean
        If txtNombre.Text.Trim() = "" OrElse txtMarca.Text.Trim() = "" Then
            MessageBox.Show("Nombre y Marca son obligatorios.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        If Not Decimal.TryParse(txtPrecio.Text, NumberStyles.Number, CultureInfo.CurrentCulture, precio) OrElse precio < 0 Then
            MessageBox.Show("Precio no válido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtPrecio.Focus()
            Return False
        End If
        If Not Integer.TryParse(txtStock.Text, stock) OrElse stock < 0 Then
            MessageBox.Show("Stock no válido (número entero).", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtStock.Focus()
            Return False
        End If
        Return True
    End Function

    Private Sub MostrarFila(row As DataRow)
        txtCodigo.Text = row("Codigo").ToString()
        txtNombre.Text = row("Nombre").ToString()
        txtPrecio.Text = Convert.ToDecimal(row("Precio")).ToString("0.00")
        txtStock.Text = row("Stock").ToString()
        txtMarca.Text = row("Marca").ToString()
        HabilitarCampos(Variable.EsAdmin)
    End Sub

    ' ---------- Botones ----------
    Private Sub btnNuevo_Click(sender As Object, e As EventArgs) Handles btnNuevo.Click
        Try
            Limpiar()
            txtCodigo.Text = MetodosProductos.SiguienteCodigo().ToString()   ' numeración automática
            HabilitarCampos(True)
            txtNombre.Focus()
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnInsertar_Click(sender As Object, e As EventArgs) Handles btnInsertar.Click
        Dim precio As Decimal, stock As Integer
        If Not LeerCampos(precio, stock) Then Return
        Try
            Dim nuevo As Integer = MetodosProductos.Insertar(txtNombre.Text.Trim(), precio, stock, txtMarca.Text.Trim())
            MessageBox.Show("Producto registrado con código " & nuevo & ".", "Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Limpiar()
            HabilitarCampos(False)
            CargarGrid()
        Catch ex As Exception
            MessageBox.Show("No se pudo insertar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnActualizar_Click(sender As Object, e As EventArgs) Handles btnActualizar.Click
        If txtCodigo.Text = "" Then
            MessageBox.Show("Seleccione un producto de la lista o búsquelo por código.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim precio As Decimal, stock As Integer
        If Not LeerCampos(precio, stock) Then Return
        Try
            Dim filas As Integer = MetodosProductos.Actualizar(CInt(txtCodigo.Text), txtNombre.Text.Trim(), precio, stock, txtMarca.Text.Trim())
            If filas > 0 Then
                MessageBox.Show("Producto actualizado.", "Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information)
                CargarGrid()
            Else
                MessageBox.Show("El producto ya no existe.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            MessageBox.Show("No se pudo actualizar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnEliminar_Click(sender As Object, e As EventArgs) Handles btnEliminar.Click
        If txtCodigo.Text = "" Then
            MessageBox.Show("Seleccione un producto para eliminar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If MessageBox.Show("¿Desea eliminar el producto " & txtCodigo.Text & " - " & txtNombre.Text & "?",
                           "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        Try
            MetodosProductos.Eliminar(CInt(txtCodigo.Text))
            Limpiar()
            HabilitarCampos(False)
            CargarGrid()
        Catch ex As Exception
            MessageBox.Show("No se pudo eliminar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnBuscar_Click(sender As Object, e As EventArgs) Handles btnBuscar.Click
        ' El campo Código es de solo lectura (autonumérico), por eso se pide en una ventana
        Dim entrada As String = InputBox("Ingrese el código del producto a buscar:", "Buscar producto", txtCodigo.Text)
        If entrada.Trim() = "" Then Return   ' el usuario canceló
        Dim codigo As Integer
        If Not Integer.TryParse(entrada, codigo) Then
            MessageBox.Show("El código debe ser un número.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Try
            Dim dt = MetodosProductos.PorCodigo(codigo)
            If dt.Rows.Count = 0 Then
                MessageBox.Show("No existe un producto con ese código.", "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MostrarFila(dt.Rows(0))
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnFacturar_Click(sender As Object, e As EventArgs) Handles btnFacturar.Click
        If Not Variable.EsAdmin Then
            MessageBox.Show("Solo el administrador puede crear facturas.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Using f As New FrmFactura()
            f.ShowDialog(Me)
        End Using
        AplicarFiltro()          ' el stock pudo cambiar: recarga la lista
    End Sub

    Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
        Application.Exit()
    End Sub

    ' ---------- Grid y filtro en tiempo real ----------
    Private Sub dgvProductos_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvProductos.CellClick
        If e.RowIndex < 0 Then Return
        Dim drv = TryCast(dgvProductos.Rows(e.RowIndex).DataBoundItem, DataRowView)
        If drv IsNot Nothing Then MostrarFila(drv.Row)
    End Sub

    Private Sub AplicarFiltro() Handles txtFiltro.TextChanged, cboCampo.SelectedIndexChanged
        Try
            If txtFiltro.Text.Trim() = "" Then
                CargarGrid()
            Else
                dgvProductos.DataSource = MetodosProductos.Buscar(cboCampo.Text, txtFiltro.Text.Trim())
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
