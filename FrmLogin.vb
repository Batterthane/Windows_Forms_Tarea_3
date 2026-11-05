Public Class FrmLogin

    Private Sub btnIngresar_Click(sender As Object, e As EventArgs) Handles btnIngresar.Click
        If txtUsuario.Text.Trim() = "" OrElse txtClave.Text = "" Then
            MessageBox.Show("Ingrese usuario y contraseña.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim estado As String = Seguridad.Validar(txtUsuario.Text.Trim(), txtClave.Text)
            If estado = "" Then
                MessageBox.Show("Usuario o contraseña incorrectos.", "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Error)
                txtClave.Clear()
                txtClave.Focus()
                Return
            End If

            Variable.Usuario = txtUsuario.Text.Trim()
            Variable.Estado = estado

            Dim f As New FrmProductos()
            f.Show()
            Me.Hide()
        Catch ex As Exception
            MessageBox.Show("No se pudo conectar: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnSalir_Click(sender As Object, e As EventArgs) Handles btnSalir.Click
        Application.Exit()
    End Sub
End Class
