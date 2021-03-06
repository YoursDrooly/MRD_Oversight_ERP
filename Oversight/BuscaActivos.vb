﻿Public Class BuscaActivos

    Private fDone As Boolean = False

    Public susername As String = ""
    Public bactive As Boolean = False
    Public bonline As Boolean = False
    Public suserfullname As String = ""
    Public suseremail As String = ""
    Public susersession As Integer = 0
    Public susermachinename As String = ""
    Public suserip As String = "0.0.0.0"

    Public querystring As String = ""

    Public iassetid As Integer = 0
    Public sassetdescription As String = ""

    Public isEdit As Boolean = False
    Public wasCreated As Boolean = False

    Private openPermission As Boolean = False

    Public messagesWindowIsAlreadyOpen As Boolean = False


    Private Sub checkMessages(ByVal username As String, ByVal x As Integer, ByVal y As Integer)

        Dim unreadmessagecount As Integer = 0
        unreadmessagecount = getSQLQueryAsInteger(0, "SELECT COUNT(*) FROM messages where susername = '" & username & "' AND bread = 0")

        If unreadmessagecount > 0 And messagesWindowIsAlreadyOpen = False Then

            Dim msg As New Mensajes
            Dim pt As Point

            msg.susername = username
            msg.bactive = bactive
            msg.bonline = bonline
            msg.suserfullname = suserfullname
            msg.suseremail = suseremail
            msg.susersession = susersession
            msg.susermachinename = susermachinename
            msg.suserip = suserip

            msg.StartPosition = FormStartPosition.Manual

            Dim tamañoPantalla As Integer = Screen.GetWorkingArea(Me).Height

            Dim tmpPt1 As Point = New Point(Me.Location.X, (tamañoPantalla - Me.Size.Height - msg.Size.Height) / 2) 'msg window
            Dim tmpPt2 As Point = New Point(Me.Location.X, tmpPt1.Y + msg.Size.Height) 'me

            If tmpPt1.Y > Screen.GetWorkingArea(Me).Location.Y Then

                pt = New Point(Me.Location.X, tmpPt1.Y)
                Me.Location = New Point(Me.Location.X, tmpPt2.Y)

            Else

                pt = New Point(x, y)

            End If

            msg.Location = pt
            msg.bAlreadyOpen = True

            messagesWindowIsAlreadyOpen = True

            msg.Show()

        End If

    End Sub


    Private Sub checkForKickoutsAndTimedOuts()

        Dim queryMySession As String = ""
        Dim dsMySession As DataSet

        queryMySession = "SELECT * FROM sessions s WHERE s.susername = '" & susername & "' AND s.susersession = '" & susersession & "' ORDER BY s.ilogindate DESC, s.slogintime DESC LIMIT 1 "

        dsMySession = getSQLQueryAsDataset(0, queryMySession)

        If dsMySession.Tables(0).Rows.Count > 0 Then

            If dsMySession.Tables(0).Rows(0).Item("btimedout") = "1" Then

                MsgBox("Tu sesión ha expirado. Es necesario que entres de nuevo al sistema con tu usuario y contraseña", MsgBoxStyle.Critical, "Sesión expirada")

                susername = ""
                bactive = False
                bonline = False
                suserfullname = ""
                suseremail = ""
                susersession = 0
                susermachinename = ""
                suserip = "0.0.0.0"

                Dim l As New Login

                l.isEdit = True

                l.ShowDialog(Me)

                If l.DialogResult <> Windows.Forms.DialogResult.OK Then

                    MsgBox("Cerrando Aplicación SIN Guardar...", MsgBoxStyle.Critical, "Intento Fallido")
                    System.Environment.Exit(0)

                End If

            End If

            If dsMySession.Tables(0).Rows(0).Item("bkickedout") = "1" Then

                MsgBox("Has sido sacado del sistema. Para continuar es necesario que entres de nuevo al sistema con tu usuario y contraseña", MsgBoxStyle.Critical, "Logged out")

                susername = ""
                bactive = False
                bonline = False
                suserfullname = ""
                suseremail = ""
                susersession = 0
                susermachinename = ""
                suserip = "0.0.0.0"

                Dim l As New Login

                l.isEdit = True

                l.ShowDialog(Me)

                If l.DialogResult <> Windows.Forms.DialogResult.OK Then

                    MsgBox("Cerrando Aplicación SIN Guardar...", MsgBoxStyle.Critical, "Intento Fallido")
                    System.Environment.Exit(0)

                End If

            End If

        End If

    End Sub


    Private Sub setControlsByPermissions(ByVal windowname As String, ByVal username As String)

        'Check for specific permissions on every window, but only for that unique window permissions, not the entire list!!

        Dim dsPermissions As DataSet

        Dim permission As String

        Dim viewPermission As Boolean = False

        dsPermissions = getSQLQueryAsDataset(0, "SELECT * FROM userpermissions WHERE susername = '" & username & "' AND swindowname = '" & windowname & "'")

        For j = 0 To dsPermissions.Tables(0).Rows.Count - 1

            Try

                permission = dsPermissions.Tables(0).Rows(j).Item("spermission")

                If permission = "Ver" Then
                    viewPermission = True
                End If

                If permission = "Nuevo" Then
                    btnCrear.Enabled = True
                End If

                If permission = "Modificar" Then
                    openPermission = True
                    btnAbrir.Enabled = True
                End If

                If permission = "Exportar" Then
                    btnExportar.Enabled = True
                End If

            Catch ex As Exception

            End Try

            permission = ""

        Next j


        If viewPermission = False Then

            Dim fecha As Integer = 0
            Dim hora As String = "00:00:00"

            fecha = getMySQLDate()
            hora = getAppTime()

            executeSQLCommand(0, "INSERT IGNORE INTO logs VALUES (" & fecha & ", '" & hora & "', '" & susername & "', " & susersession & ", '" & suserip & "', '" & susermachinename & "', 'Acceso denegado a la ventana de Buscar Activos', 'OK')")

            Dim dsUsuariosSysAdmin As DataSet

            dsUsuariosSysAdmin = getSQLQueryAsDataset(0, "SELECT susername FROM userspecialattributes WHERE bsysadmin = 1")

            If dsUsuariosSysAdmin.Tables(0).Rows.Count > 0 Then

                For i = 0 To dsUsuariosSysAdmin.Tables(0).Rows.Count - 1
                    executeSQLCommand(0, "INSERT INTO messages (susername, susersession, smessage, bread, imessagedatetime, smessagecreatorusername, iupdatedatetime, supdateusername) VALUES ('" & dsUsuariosSysAdmin.Tables(0).Rows(i).Item(0) & "', 0, 'Un usuario ha intentado propasar sus permisos. ¿Podrías revisar?', 0, '" & convertYYYYMMDDtoYYYYhyphenMMhyphenDD(fecha) & " " & hora & "', 'SYSTEM', '" & convertYYYYMMDDtoYYYYhyphenMMhyphenDD(fecha) & " " & hora & "', 'SYSTEM')")
                Next i

            End If

            MsgBox("No tienes los permisos necesarios para abrir esta Ventana. Este intento ha sido notificado al administrador.", MsgBoxStyle.Exclamation, "Access Denied")
            Me.DialogResult = Windows.Forms.DialogResult.Cancel
            Me.Close()

        End If

    End Sub


    Private Sub BuscaActivos_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing

        executeSQLCommand(0, "INSERT IGNORE INTO logs VALUES (" & getMySQLDate() & ", '" & getAppTime() & "', '" & susername & "', " & susersession & ", '" & suserip & "', '" & susermachinename & "', 'Cerró la ventana de Buscar Activo con el activo " & iassetid & " : " & sassetdescription & " seleccionado', 'OK')")

    End Sub


    Private Sub BuscaActivos_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown

        If e.KeyCode = Keys.F5 Then

            If My.Computer.Info.OSFullName.StartsWith("Microsoft Windows 7") = True Then
                NotifyIcon1.Icon = Oversight.My.Resources.winmineVista16x16
            Else
                NotifyIcon1.Icon = Oversight.My.Resources.winmineXP16x16
            End If

            NotifyIcon1.Text = "Buscaminas"

            NotifyIcon1.Visible = True

            Me.Visible = False
            Do While Not fDone
                System.Windows.Forms.Application.DoEvents()
            Loop
            fDone = False
            Me.Visible = True

        End If

    End Sub


    Private Sub BuscaActivos_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        Cursor.Current = System.Windows.Forms.Cursors.WaitCursor

        Me.KeyPreview = True

        closeTimedOutConnections()
        checkForKickoutsAndTimedOuts()
        checkMessages(susername, Me.Location.X, Me.Location.Y)
        setControlsByPermissions(Me.Name, susername)

        Dim queryBusqueda As String

        txtBuscar.Text = querystring

        Dim fechaHoy As Date = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(getMySQLDate())
        Dim fechaInicioQuincena As Date
        Dim fechaFinQuincena As Date

        Dim auxMonth As String = ""
        Dim auxMonth2 As String = ""
        Dim auxYear2 As String = ""

        If fechaHoy.Day > 15 Then

            fechaInicioQuincena = fechaHoy.AddDays((fechaHoy.Day - 1) * -1)

            If fechaHoy.Month < 10 Then

                auxMonth = "0" & fechaHoy.Month
                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & auxMonth & "16")

            Else

                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & fechaHoy.Month & "16")

            End If

        Else

            If fechaHoy.Month < 10 Then

                auxMonth = "0" & fechaHoy.Month

                If fechaHoy.Month = 12 Then
                    auxMonth2 = "01"
                    auxYear2 = fechaHoy.Year + 1
                Else
                    auxMonth2 = "0" & fechaHoy.Month + 1
                    auxYear2 = fechaHoy.Year
                End If

                fechaInicioQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & auxMonth & "15")
                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(auxYear2 & auxMonth2 & "01")

            Else

                If fechaHoy.Month = 12 Then
                    auxMonth2 = "01"
                    auxYear2 = fechaHoy.Year + 1
                Else
                    auxMonth2 = fechaHoy.Month + 1
                    auxYear2 = fechaHoy.Year
                End If

                fechaInicioQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & fechaHoy.Month & "15")
                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(auxYear2 & auxMonth2 & "01")

            End If

        End If

        queryBusqueda = "" & _
        "SELECT a.iassetid, a.sassetdescription AS 'Activo', " & _
        "FORMAT(IF(gv.dlitersdispensed IS NULL, 0, gv.dlitersdispensed), 2) AS 'Litros de Combustible usados esta Quincena', " & _
        "FORMAT(IF(gv.dgasvoucheramount IS NULL, 0, gv.dgasvoucheramount), 2) AS 'Importe Combustible usado esta Quincena', " & _
        "STR_TO_DATE(CONCAT(si.iupdatedate, ' ', si.supdatetime), '%Y%c%d %T') AS 'Fecha Ultimo Servicio' " & _
        "FROM assets a " & _
        "LEFT JOIN (SELECT igasvoucherid, iassetid, igasdate, sgastime, scarmileageatrequest, scarorigindestination, svouchercomment, ipeopleid, SUM(dlitersdispensed) AS dlitersdispensed, igastypeid, SUM(dgasvoucheramount) AS dgasvoucheramount, iupdatedate, supdatetime, supdateusername FROM gasvouchers WHERE igasdate > " & convertDDdashMMdashYYYYtoYYYYMMDD(fechaInicioQuincena) & " and igasdate < " & convertDDdashMMdashYYYYtoYYYYMMDD(fechaFinQuincena) & " GROUP BY 2) gv ON a.iassetid = gv.iassetid " & _
        "LEFT JOIN (SELECT si.isupplierinvoiceid, si.isupplierid, sia.iassetid, si.iinvoicedate, si.sinvoicetime, si.isupplierinvoicetypeid, si.ssupplierinvoicefolio, si.sexpensedescription, si.sexpenselocation, si.dIVApercentage, si.ipeopleid, si.iupdatedate, si.supdatetime, si.supdateusername FROM supplierinvoices si JOIN supplierinvoiceassets sia ON si.isupplierinvoiceid = sia.isupplierinvoiceid WHERE si.sexpensedescription LIKE '%servicio%' ORDER BY si.iinvoicedate DESC LIMIT 1) si ON a.iassetid = si.iassetid " & _
        "WHERE a.sassetdescription LIKE '%" & querystring & "%' " & _
        "ORDER BY 2 ASC "

        txtBuscar.Enabled = True

        setDataGridView(dgvActivos, queryBusqueda, True)

        dgvActivos.Columns(0).Visible = False

        dgvActivos.Columns(1).Width = 400

        executeSQLCommand(0, "INSERT IGNORE INTO logs VALUES (" & getMySQLDate() & ", '" & getAppTime() & "', '" & susername & "', " & susersession & ", '" & suserip & "', '" & susermachinename & "', 'Abrió la Ventana de Buscar Activos', 'OK')")

        dgvActivos.Select()

        txtBuscar.Select()
        txtBuscar.Focus()
        txtBuscar.SelectionStart() = txtBuscar.Text.Length

        Cursor.Current = System.Windows.Forms.Cursors.Default

    End Sub


    Private Sub NotifyIcon1_DoubleClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles NotifyIcon1.DoubleClick

        Dim n As New Loader

        n.isEdit = True

        n.ShowDialog()

        If n.DialogResult = Windows.Forms.DialogResult.OK Then

            fDone = True

        End If

    End Sub


    Private Sub txtBuscar_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles txtBuscar.KeyUp

        Dim strcaracteresprohibidos As String = "|°!#$&/()=?¡*¨[]_:;,-{}+´¿'¬^`~@\<>"
        Dim arrayCaractProhib As Char() = strcaracteresprohibidos.ToCharArray
        Dim resultado As Boolean = False

        For carp = 0 To arrayCaractProhib.Length - 1

            If txtBuscar.Text.Contains(arrayCaractProhib(carp)) Then
                txtBuscar.Text = txtBuscar.Text.Replace(arrayCaractProhib(carp), "")
                resultado = True
            End If

        Next carp

        If resultado = True Then
            txtBuscar.Select(txtBuscar.Text.Length, 0)
        End If

        txtBuscar.Text = txtBuscar.Text.Replace("--", "").Replace("'", "")

    End Sub


    Private Sub txtBuscar_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtBuscar.TextChanged

        Cursor.Current = System.Windows.Forms.Cursors.WaitCursor

        Dim queryBusqueda As String

        querystring = txtBuscar.Text

        Dim fechaHoy As Date = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(getMySQLDate())
        Dim fechaInicioQuincena As Date
        Dim fechaFinQuincena As Date

        Dim auxMonth As String = ""
        Dim auxMonth2 As String = ""
        Dim auxYear2 As String = ""

        If fechaHoy.Day > 15 Then

            fechaInicioQuincena = fechaHoy.AddDays((fechaHoy.Day - 1) * -1)

            If fechaHoy.Month < 10 Then

                auxMonth = "0" & fechaHoy.Month
                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & auxMonth & "16")

            Else

                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & fechaHoy.Month & "16")

            End If

        Else

            If fechaHoy.Month < 10 Then

                auxMonth = "0" & fechaHoy.Month

                If fechaHoy.Month = 12 Then
                    auxMonth2 = "01"
                    auxYear2 = fechaHoy.Year + 1
                Else
                    auxMonth2 = "0" & fechaHoy.Month + 1
                    auxYear2 = fechaHoy.Year
                End If

                fechaInicioQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & auxMonth & "15")
                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(auxYear2 & auxMonth2 & "01")

            Else

                If fechaHoy.Month = 12 Then
                    auxMonth2 = "01"
                    auxYear2 = fechaHoy.Year + 1
                Else
                    auxMonth2 = fechaHoy.Month + 1
                    auxYear2 = fechaHoy.Year
                End If

                fechaInicioQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(fechaHoy.Year & fechaHoy.Month & "15")
                fechaFinQuincena = convertYYYYMMDDtoDDhyphenMMhyphenYYYY(auxYear2 & auxMonth2 & "01")

            End If

        End If

        queryBusqueda = "" & _
        "SELECT a.iassetid, a.sassetdescription AS 'Activo', " & _
        "FORMAT(IF(gv.dlitersdispensed IS NULL, 0, gv.dlitersdispensed), 2) AS 'Litros de Combustible usados esta Quincena', " & _
        "FORMAT(IF(gv.dgasvoucheramount IS NULL, 0, gv.dgasvoucheramount), 2) AS 'Importe Combustible usado esta Quincena', " & _
        "STR_TO_DATE(CONCAT(si.iupdatedate, ' ', si.supdatetime), '%Y%c%d %T') AS 'Fecha Ultimo Servicio' " & _
        "FROM assets a " & _
        "LEFT JOIN (SELECT igasvoucherid, iassetid, igasdate, sgastime, scarmileageatrequest, scarorigindestination, svouchercomment, ipeopleid, SUM(dlitersdispensed) AS dlitersdispensed, igastypeid, SUM(dgasvoucheramount) AS dgasvoucheramount, iupdatedate, supdatetime, supdateusername FROM gasvouchers WHERE igasdate > " & convertDDdashMMdashYYYYtoYYYYMMDD(fechaInicioQuincena) & " and igasdate < " & convertDDdashMMdashYYYYtoYYYYMMDD(fechaFinQuincena) & " GROUP BY 2) gv ON a.iassetid = gv.iassetid " & _
        "LEFT JOIN (SELECT si.isupplierinvoiceid, si.isupplierid, sia.iassetid, si.iinvoicedate, si.sinvoicetime, si.isupplierinvoicetypeid, si.ssupplierinvoicefolio, si.sexpensedescription, si.sexpenselocation, si.dIVApercentage, si.ipeopleid, si.iupdatedate, si.supdatetime, si.supdateusername FROM supplierinvoices si JOIN supplierinvoiceassets sia ON si.isupplierinvoiceid = sia.isupplierinvoiceid WHERE si.sexpensedescription LIKE '%servicio%' ORDER BY si.iinvoicedate DESC LIMIT 1) si ON a.iassetid = si.iassetid " & _
        "WHERE a.sassetdescription LIKE '%" & querystring & "%' " & _
        "ORDER BY 2 ASC "

        txtBuscar.Enabled = True

        setDataGridView(dgvActivos, queryBusqueda, True)

        dgvActivos.Columns(0).Visible = False

        dgvActivos.Columns(1).Width = 400

        Cursor.Current = System.Windows.Forms.Cursors.Default

    End Sub


    Private Sub dgvActivos_CellClick(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles dgvActivos.CellClick

        Try

            If dgvActivos.CurrentRow Is Nothing Then
                Exit Sub
            End If

            iassetid = CInt(dgvActivos.Rows(e.RowIndex).Cells(0).Value)
            sassetdescription = dgvActivos.Rows(e.RowIndex).Cells(1).Value

        Catch ex As Exception

            iassetid = 0
            sassetdescription = ""

        End Try

    End Sub


    Private Sub dgvActivos_CellContentClick(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles dgvActivos.CellContentClick

        Try

            If dgvActivos.CurrentRow Is Nothing Then
                Exit Sub
            End If

            iassetid = CInt(dgvActivos.Rows(e.RowIndex).Cells(0).Value)
            sassetdescription = dgvActivos.Rows(e.RowIndex).Cells(1).Value

        Catch ex As Exception

            iassetid = 0
            sassetdescription = ""

        End Try

    End Sub


    Private Sub dgvActivos_SelectionChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles dgvActivos.SelectionChanged

        Try

            If dgvActivos.CurrentRow Is Nothing Then
                Exit Sub
            End If

            iassetid = CInt(dgvActivos.CurrentRow.Cells(0).Value)
            sassetdescription = dgvActivos.CurrentRow.Cells(1).Value

        Catch ex As Exception

            iassetid = 0
            sassetdescription = ""

        End Try

    End Sub


    Private Sub dgvActivos_CellContentDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles dgvActivos.CellContentDoubleClick

        Cursor.Current = System.Windows.Forms.Cursors.WaitCursor

        Try

            If dgvActivos.CurrentRow Is Nothing Then
                Exit Sub
            End If

            iassetid = CInt(dgvActivos.Rows(e.RowIndex).Cells(0).Value)
            sassetdescription = dgvActivos.Rows(e.RowIndex).Cells(1).Value

        Catch ex As Exception

            iassetid = 0
            sassetdescription = ""

        End Try

        If dgvActivos.SelectedRows.Count = 1 Then

            If isEdit = False Then

                Me.DialogResult = Windows.Forms.DialogResult.OK
                Me.Close()

                Cursor.Current = System.Windows.Forms.Cursors.Default
                Exit Sub

            End If

            Dim aa As New AgregarActivo

            aa.susername = susername
            aa.bactive = bactive
            aa.bonline = bonline
            aa.suserfullname = suserfullname

            aa.suseremail = suseremail
            aa.susersession = susersession
            aa.susermachinename = susermachinename
            aa.suserip = suserip

            aa.iassetid = iassetid

            aa.isEdit = True

            If Me.WindowState = FormWindowState.Maximized Then
                aa.WindowState = FormWindowState.Maximized
            End If

            Me.Visible = False
            aa.ShowDialog(Me)
            Me.Visible = True

        Else

            Cursor.Current = System.Windows.Forms.Cursors.Default
            Exit Sub

        End If

        Cursor.Current = System.Windows.Forms.Cursors.Default

    End Sub


    Private Sub dgvActivos_CellDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Forms.DataGridViewCellEventArgs) Handles dgvActivos.CellDoubleClick

        Cursor.Current = System.Windows.Forms.Cursors.WaitCursor

        Try

            If dgvActivos.CurrentRow Is Nothing Then
                Exit Sub
            End If

            iassetid = CInt(dgvActivos.Rows(e.RowIndex).Cells(0).Value)
            sassetdescription = dgvActivos.Rows(e.RowIndex).Cells(1).Value

        Catch ex As Exception

            iassetid = 0
            sassetdescription = ""

        End Try

        If dgvActivos.SelectedRows.Count = 1 Then

            If isEdit = False Then

                Me.DialogResult = Windows.Forms.DialogResult.OK
                Me.Close()

                Cursor.Current = System.Windows.Forms.Cursors.Default
                Exit Sub

            End If

            Dim aa As New AgregarActivo

            aa.susername = susername
            aa.bactive = bactive
            aa.bonline = bonline
            aa.suserfullname = suserfullname

            aa.suseremail = suseremail
            aa.susersession = susersession
            aa.susermachinename = susermachinename
            aa.suserip = suserip

            aa.iassetid = iassetid

            aa.isEdit = True

            If Me.WindowState = FormWindowState.Maximized Then
                aa.WindowState = FormWindowState.Maximized
            End If

            Me.Visible = False
            aa.ShowDialog(Me)
            Me.Visible = True

        Else

            Cursor.Current = System.Windows.Forms.Cursors.Default
            Exit Sub

        End If

        Cursor.Current = System.Windows.Forms.Cursors.Default

    End Sub


    Private Sub btnCancelar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCancelar.Click

        querystring = ""

        iassetid = 0
        sassetdescription = ""

        wasCreated = False

        Me.DialogResult = Windows.Forms.DialogResult.Cancel
        Me.Close()

    End Sub


    Private Sub btnAbrir_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnAbrir.Click

        Cursor.Current = System.Windows.Forms.Cursors.WaitCursor

        If dgvActivos.SelectedRows.Count = 1 Then

            If isEdit = False Then

                Me.DialogResult = Windows.Forms.DialogResult.OK
                Me.Close()

                Cursor.Current = System.Windows.Forms.Cursors.Default
                Exit Sub

            End If

            Dim aa As New AgregarActivo

            aa.susername = susername
            aa.bactive = bactive
            aa.bonline = bonline
            aa.suserfullname = suserfullname

            aa.suseremail = suseremail
            aa.susersession = susersession
            aa.susermachinename = susermachinename
            aa.suserip = suserip

            aa.iassetid = iassetid

            aa.isEdit = True

            If Me.WindowState = FormWindowState.Maximized Then
                aa.WindowState = FormWindowState.Maximized
            End If

            Me.Visible = False
            aa.ShowDialog(Me)
            Me.Visible = True

        Else

            Cursor.Current = System.Windows.Forms.Cursors.Default
            Exit Sub

        End If

        Cursor.Current = System.Windows.Forms.Cursors.Default

    End Sub


    Private Sub btnCrear_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnCrear.Click

        Cursor.Current = System.Windows.Forms.Cursors.WaitCursor

        Dim aa As New AgregarActivo

        aa.susername = susername
        aa.bactive = bactive
        aa.bonline = bonline
        aa.suserfullname = suserfullname
        aa.suseremail = suseremail
        aa.susersession = susersession
        aa.susermachinename = susermachinename
        aa.suserip = suserip

        aa.isEdit = False

        If Me.WindowState = FormWindowState.Maximized Then
            aa.WindowState = FormWindowState.Maximized
        End If

        Me.Visible = False
        aa.ShowDialog(Me)
        Me.Visible = True

        If aa.DialogResult = Windows.Forms.DialogResult.OK Then

            Me.iassetid = aa.iassetid
            Me.sassetdescription = aa.sassetdescription

            If aa.wasCreated = True Then
                wasCreated = True
            End If

            Me.DialogResult = Windows.Forms.DialogResult.OK
            Me.Close()

        End If

        Cursor.Current = System.Windows.Forms.Cursors.Default

    End Sub


    Private Sub btnExportar_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnExportar.Click

        Try

            Dim resultado As Boolean = False

            Dim fecha As String = ""
            Dim dayAux As String = ""
            Dim monthAux As String = ""
            Dim hourAux As String = ""
            Dim minuteAux As String = ""
            Dim secondAux As String = ""

            If DateTime.Today.Month.ToString.Length < 2 Then
                monthAux = "0" & DateTime.Today.Month
            Else
                monthAux = DateTime.Today.Month
            End If

            If DateTime.Today.Day.ToString.Length < 2 Then
                dayAux = "0" & DateTime.Today.Day
            Else
                dayAux = DateTime.Today.Day
            End If

            If Date.Now.Hour.ToString.Length < 2 Then
                hourAux = "0" & DateTime.Now.Hour
            Else
                hourAux = DateTime.Now.Hour
            End If

            If Date.Now.Minute.ToString.Length < 2 Then
                minuteAux = "0" & DateTime.Now.Minute
            Else
                minuteAux = DateTime.Now.Minute
            End If

            If Date.Now.Second.ToString.Length < 2 Then
                secondAux = "0" & DateTime.Now.Second
            Else
                secondAux = DateTime.Now.Second
            End If

            fecha = DateTime.Today.Year & monthAux & dayAux & hourAux & minuteAux & secondAux



            msSaveFileDialog.FileName = "Activos " & fecha
            msSaveFileDialog.Filter = "Excel Files (*.xls) |*.xls"
            msSaveFileDialog.DefaultExt = "*.xls"

            If msSaveFileDialog.ShowDialog() = DialogResult.OK Then

                Cursor.Current = System.Windows.Forms.Cursors.WaitCursor
                resultado = ExportToExcel(msSaveFileDialog.FileName)

                Cursor.Current = System.Windows.Forms.Cursors.Default

                If resultado = True Then
                    MsgBox("Activos Exportados Correctamente!" & Chr(13) & "El archivo se abrirá al dar click en OK", MsgBoxStyle.OkOnly, "Exportación Completada")
                    System.Diagnostics.Process.Start(msSaveFileDialog.FileName)
                Else
                    MsgBox("No se ha podido exportar los Activos. Intente nuevamente.", MsgBoxStyle.OkOnly, "Error al exportar los Activos")
                End If

            End If

        Catch ex As Exception

        End Try

    End Sub


    Private Function ExportToExcel(ByVal pth As String) As Boolean

        Try

            Dim fs As New IO.StreamWriter(pth, False)
            fs.WriteLine("<?xml version=""1.0""?>")
            fs.WriteLine("<?mso-application progid=""Excel.Sheet""?>")
            fs.WriteLine("<Workbook xmlns=""urn:schemas-microsoft-com:office:spreadsheet"" xmlns:o=""urn:schemas-microsoft-com:office:office"" xmlns:x=""urn:schemas-microsoft-com:office:excel"" xmlns:ss=""urn:schemas-microsoft-com:office:spreadsheet"" xmlns:html=""http://www.w3.org/TR/REC-html40"">")

            ' Create the styles for the worksheet
            fs.WriteLine("  <Styles>")

            ' Style for the document name
            fs.WriteLine("  <Style ss:ID=""1"">")
            fs.WriteLine("   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("   <Borders>")
            fs.WriteLine("  <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("  <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("  <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("  <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("   </Borders>")
            fs.WriteLine("   <Font ss:FontName=""Arial"" ss:Size=""12"" ss:Bold=""1""></Font>")
            fs.WriteLine("   <Interior ss:Color=""#FF9900"" ss:Pattern=""Solid""></Interior>")
            fs.WriteLine("   <NumberFormat></NumberFormat>")
            fs.WriteLine("   <Protection></Protection>")
            fs.WriteLine("  </Style>")

            ' Style for the column headers
            fs.WriteLine("   <Style ss:ID=""2"">")
            fs.WriteLine("   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Center"" ss:WrapText=""1""></Alignment>")
            fs.WriteLine("   <Borders>")
            fs.WriteLine("  <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("  <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("  <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("  <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""></Border>")
            fs.WriteLine("   </Borders>")
            fs.WriteLine("   <Font ss:FontName=""Arial"" ss:Size=""9"" ss:Bold=""1""></Font>")
            fs.WriteLine("  </Style>")


            ' Style for the left sided info
            fs.WriteLine("    <Style ss:ID=""9"">")
            fs.WriteLine("      <Font ss:FontName=""Arial"" ss:Size=""10""></Font>")
            fs.WriteLine("      <Alignment ss:Horizontal=""Left"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("    </Style>")

            ' Style for the right sided info
            fs.WriteLine("    <Style ss:ID=""10"">")
            fs.WriteLine("      <Font ss:FontName=""Arial"" ss:Size=""10""></Font>")
            fs.WriteLine("      <Alignment ss:Horizontal=""Right"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("    </Style>")

            ' Style for the middle sided info
            fs.WriteLine("    <Style ss:ID=""11"">")
            fs.WriteLine("      <Font ss:FontName=""Arial"" ss:Size=""10""></Font>")
            fs.WriteLine("      <Alignment ss:Horizontal=""Center"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("    </Style>")

            ' Style for the SUBtotals labels
            fs.WriteLine("    <Style ss:ID=""12"">")
            fs.WriteLine("      <Font ss:FontName=""Arial"" ss:Size=""9""></Font>")
            fs.WriteLine("   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("   <Interior ss:Color=""#FFCC00"" ss:Pattern=""Solid""></Interior>")
            fs.WriteLine("   <NumberFormat></NumberFormat>")
            fs.WriteLine("    </Style>")

            ' Style for the totals labels
            fs.WriteLine("    <Style ss:ID=""13"">")
            fs.WriteLine("      <Font ss:FontName=""Arial"" ss:Size=""9""></Font>")
            fs.WriteLine("      <Alignment ss:Horizontal=""Right"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("   <Interior ss:Color=""#FF9900"" ss:Pattern=""Solid""></Interior>")
            fs.WriteLine("   <NumberFormat></NumberFormat>")
            fs.WriteLine("    </Style>")

            ' Style for the totals
            fs.WriteLine("    <Style ss:ID=""14"">")
            fs.WriteLine("      <Font ss:FontName=""Arial"" ss:Size=""9""></Font>")
            fs.WriteLine("   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Center""></Alignment>")
            fs.WriteLine("   <Interior ss:Color=""#FF9900"" ss:Pattern=""Solid""></Interior>")
            fs.WriteLine("   <NumberFormat ss:Format=""Standard""></NumberFormat>")
            fs.WriteLine("    </Style>")

            fs.WriteLine("  </Styles>")

            ' Write the worksheet contents
            fs.WriteLine("<Worksheet ss:Name=""Hoja1"">")
            fs.WriteLine("  <Table ss:DefaultColumnWidth=""60"" ss:DefaultRowHeight=""15"">")

            'Write the project header info
            fs.WriteLine("   <Column ss:Width=""294.75""/>")
            fs.WriteLine("   <Column ss:AutoFitWidth=""0"" ss:Width=""119.25""/>")
            fs.WriteLine("   <Column ss:Width=""112.5""/>")
            fs.WriteLine("   <Column ss:Width=""84.75""/>")
            fs.WriteLine("   <Column ss:AutoFitWidth=""0"" ss:Width=""119.25""/>")
            fs.WriteLine("   <Column ss:AutoFitWidth=""0"" ss:Width=""126.75""/>")
            fs.WriteLine("   <Column ss:AutoFitWidth=""0"" ss:Width=""147.75""/>")
            fs.WriteLine("   <Column ss:AutoFitWidth=""0"" ss:Width=""72""/>")
            fs.WriteLine("   <Column ss:Width=""126""/>")

            fs.WriteLine("   <Row ss:AutoFitHeight=""0"">")
            fs.WriteLine("  <Cell ss:MergeAcross=""3"" ss:StyleID=""1""><Data ss:Type=""String"">ACTIVOS</Data></Cell>")
            fs.WriteLine("   </Row>")


            fs.WriteLine(String.Format("    <Row ss:AutoFitHeight=""0"">"))
            fs.WriteLine(String.Format("      <Cell ss:StyleID=""9""><Data ss:Type=""String"">{0}</Data></Cell>", "Fecha:"))
            fs.WriteLine(String.Format("      <Cell ss:StyleID=""9""><Data ss:Type=""String"">{0}</Data></Cell>", convertYYYYMMDDtoDDhyphenMMhyphenYYYY(getMySQLDate()) & " " & getAppTime()))
            fs.WriteLine("    </Row>")


            'Write the grid headers columns (taken out since columns are already defined)
            'For Each col As DataGridViewColumn In dgv.Columns
            '    If col.Visible Then
            '        fs.WriteLine(String.Format("    <Column ss:Width=""{0}""></Column>", col.Width))
            '    End If
            'Next

            'Write the grid headers
            fs.WriteLine("    <Row ss:AutoFitHeight=""0"" ss:Height=""22.5"">")

            For Each col As DataGridViewColumn In dgvActivos.Columns
                If col.Visible Then
                    fs.WriteLine(String.Format("      <Cell ss:StyleID=""2""><Data ss:Type=""String"">{0}</Data></Cell>", col.HeaderText))
                End If
            Next

            fs.WriteLine("    </Row>")

            ' Write contents for each cell
            For Each row As DataGridViewRow In dgvActivos.Rows

                If dgvActivos.AllowUserToAddRows = True And row.Index = dgvActivos.Rows.Count - 1 Then
                    Exit For
                End If

                fs.WriteLine(String.Format("    <Row ss:AutoFitHeight=""0"">"))

                For Each col As DataGridViewColumn In dgvActivos.Columns

                    If col.Visible Then

                        If row.Cells(col.Name).Value.ToString = "" Then

                            fs.WriteLine(String.Format("      <Cell ss:StyleID=""9""></Cell>"))

                        Else

                            fs.WriteLine(String.Format("      <Cell ss:StyleID=""9""><Data ss:Type=""String"">{0}</Data></Cell>", row.Cells(col.Name).Value.ToString))

                        End If

                    End If

                Next col

                fs.WriteLine("    </Row>")

            Next row

            'Write the separation between results and totals
            fs.WriteLine(String.Format("    <Row ss:AutoFitHeight=""0"">"))
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("    </Row>")

            'Write the totals 
            fs.WriteLine(String.Format("    <Row ss:AutoFitHeight=""0"">"))
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("    </Row>")

            fs.WriteLine(String.Format("    <Row ss:AutoFitHeight=""0"">"))
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("    </Row>")

            fs.WriteLine(String.Format("    <Row ss:AutoFitHeight=""0"">"))
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("      <Cell ss:StyleID=""9""></Cell>")
            fs.WriteLine("    </Row>")


            ' Close up the document
            fs.WriteLine("  </Table>")

            fs.WriteLine("  <WorksheetOptions xmlns=""urn:schemas-microsoft-com:office:excel"">")
            fs.WriteLine("   <PageSetup>")
            fs.WriteLine("  <Layout x:Orientation=""Landscape""/>")
            fs.WriteLine("  <Header x:Margin=""0.51181102362204722""/>")
            fs.WriteLine("  <Footer x:Margin=""0.51181102362204722""/>")
            fs.WriteLine("  <PageMargins x:Bottom=""0.98425196850393704"" x:Left=""0.74803149606299213"" x:Right=""0.74803149606299213"" x:Top=""0.98425196850393704""/>")
            fs.WriteLine("   </PageSetup>")
            fs.WriteLine("   <Unsynced/>")
            fs.WriteLine("   <Print>")
            fs.WriteLine("  <ValidPrinterInfo/>")
            fs.WriteLine("  <PaperSizeIndex>9</PaperSizeIndex>")
            fs.WriteLine("  <Scale>65</Scale>")
            fs.WriteLine("  <HorizontalResolution>200</HorizontalResolution>")
            fs.WriteLine("  <VerticalResolution>200</VerticalResolution>")
            fs.WriteLine("   </Print>")
            fs.WriteLine("   <Zoom>75</Zoom>")
            fs.WriteLine("   <Selected/>")
            fs.WriteLine("   <Panes>")
            fs.WriteLine("  <Pane>")
            fs.WriteLine("   <Number>3</Number>")
            fs.WriteLine("   <ActiveRow>16</ActiveRow>")
            fs.WriteLine("   <ActiveCol>7</ActiveCol>")
            fs.WriteLine("  </Pane>")
            fs.WriteLine("   </Panes>")
            fs.WriteLine("   <ProtectObjects>False</ProtectObjects>")
            fs.WriteLine("   <ProtectScenarios>False</ProtectScenarios>")
            fs.WriteLine("  </WorksheetOptions>")


            fs.WriteLine("</Worksheet>")
            fs.WriteLine("</Workbook>")
            fs.Close()

            Return True

        Catch ex As Exception
            Return False
        End Try

    End Function


End Class