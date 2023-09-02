Imports CHR.Common
Imports DevExpress.XtraEditors
Imports DevExpress.XtraGrid
Imports System.Data.OleDb

Public Class clsData
    Implements iclsList
    Dim dt As DataTable
    Dim template As New ClsTemplate
    Dim SqlQuery As String
    Dim PriorityDescription, TaskDescription, StatusDescription, AssignedTo, IsCompleted As String
    Dim TaskDate, DateCompleted, DateStart, DateDeadline As DateTime
    Dim UserTable as datatable = ExeDt("User", {""}, {""})
    Dim PriorityTable As DataTable = ExeDt("Priority", {""}, {""})
    Dim StatusTable As DataTable = ExeDt("Status", {""}, {""})
    Public Function Action(DataRow As DataRow, ActionType As String) As Boolean Implements iclsList.Action
        Action = False
        If ActionType = cRefresh Then
            Action = True
            Exit Function
        End If
        If Not ActionType = cNew And IsNothing(DataRow) Then Exit Function
        If ActionType = cNew Or ActionType = cEdit Or ActionType = cDelete Or ActionType = cView Then
            Dim l_Dt As DataTable = Nothing
            Dim l_schema As DataTable = Nothing
            Dim l As New List(Of Object)

            If ActionType = cView Or ActionType = cDelete Then
                l_Dt = dt.Copy
                l_schema = GetDataTable("SELECT ID, TP.PriorityDescription, TS.StatusDescription, TaskDescription, TaskDate, DateStart, DateCompleted, DateDeadline, AssignedTo, IsCompleted FROM   ((TaskTable TT LEFT OUTER JOIN
             TaskPriority TP ON TP.PID = TT.PID) LEFT OUTER JOIN
             TaskStatus TS ON TS.SID = TT.SID) WHERE (TT.IsCompleted = 0) ", Constr, SchemaOnly:=False)
            Else
                l_Dt = GetDataTable("SELECT 0 AS ID,'' AS PriorityDescription,'' AS StatusDescription,'' AS TaskDescription,DATE() AS TaskDate,Now() AS DateStart,Now() AS DateCompleted,Now() AS DateDeadline,'' AS AssignedTo,'No'AS IsCompleted;", Constr)
                l_schema = GetDataTable("SELECT 0 AS ID,'' AS PriorityDescription,'' AS StatusDescription,'' AS TaskDescription,DATE() AS TaskDate,Now() AS DateStart,Now() AS DateCompleted,Now() AS DateDeadline,'' AS AssignedTo,'No'AS IsCompleted;", Constr, SchemaOnly:=False)
            End If
            l_Dt.Clear()
            If ActionType = cNew Then
                l_Dt.Rows.Add()
                l_Dt.Rows(0).Item("ID") = 0
                l_Dt.Rows(0).Item("TaskDescription") = ""
            Else
                l_Dt.ImportRow(DataRow)
            End If

            l.Add(New clsCommonDetailItem With {.RowName = "ID",
            .RowFriendlyName = "ID",
            .RowDescription = "ID",
            .AllowNull = True,
            .IsReadOnly = True,
            .IsVisible = If(ActionType = cNew, False, True),
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_LookupEdit With {.RowName = "PriorityDescription",
            .RowFriendlyName = "Prioritas",
            .RowDescription = "Prioritas",
            .ValueMember = "PriorityDescription",
            .DisplayMember = "PriorityDescription",
            .DataSource = PriorityTable,
            .AllowNull = True,
            .AllowTrimEmptyString = True})


            l.Add(New clsCommonDetailItem_MemoExEdit With {.RowName = "TaskDescription",
            .RowFriendlyName = "Task Description",
            .RowDescription = "Task Description",
            .AllowNull = True,
            .AllowTrimEmptyString = False})


            l.Add(New clsCommonDetailItem_LookupEdit With {.RowName = "StatusDescription",
            .RowFriendlyName = "Job Status",
            .RowDescription = "Job Status",
            .ValueMember = "StatusDescription",
            .DisplayMember = "StatusDescription",
            .DataSource = StatusTable,
            .AllowNull = True,
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_DateEdit With {.RowName = "TaskDate",
            .RowFriendlyName = "Task Date",
            .RowDescription = "Task Created Date",
            .AllowNull = True,
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_DateEdit With {.RowName = "DateStart",
            .RowFriendlyName = "Start Date",
            .RowDescription = "Start Date",
            .AllowNull = True,
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_DateEdit With {.RowName = "DateCompleted",
            .RowFriendlyName = "Completed Date",
            .RowDescription = "Completed Date",
            .AllowNull = True,
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_DateEdit With {.RowName = "DateDeadline",
            .RowFriendlyName = "Deadline Date",
            .RowDescription = "Deadline Date",
            .AllowNull = True,
            .IsVisible = If(Role = 1, True, False),
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_LookupEdit With {.RowName = "AssignedTo",
            .RowFriendlyName = "Assigned To",
            .RowDescription = "Assigned To",
            .ValueMember = "UserName",
            .DisplayMember = "UserName",
            .DataSource = UserTable,
            .AllowNull = True,
            .AllowTrimEmptyString = True})

            l.Add(New clsCommonDetailItem_RadioGroup With {.RowName = "IsCompleted",
            .RowFriendlyName = "Completed",
            .RowDescription = "Completed",
            .Items = {"No", "Yes"},
            .AllowNull = True,
            .AllowTrimEmptyString = True})

            Dim f As New frmCommonDetail With {.Text = ActionType,
                                         .DataSource = l_Dt,
                                         .Schema = l_schema,
                                         .ListCustomItem = l,
                                         .ButtonSaveText = If(ActionType = cNew Or ActionType = cEdit, "Save", "Delete"),
                                         .IsForView = ActionType = cView,
                                         .IsForDelete = ActionType = cDelete}

            Dim dialogresult As DialogResult = Nothing
            Try
                Do
                    dialogresult = f.ShowDialog
                    If dialogresult = DialogResult.OK Then
                        Action = Save(f.Result, ActionType)
                    End If
                Loop Until Action Or Not dialogresult
            Catch ex As Exception
                mbError(ex.ToString)
            End Try
            f.Dispose()
        End If

    End Function

    Public Function Save(DataRow As DataRow, ActionType As String) As Boolean
        Save = False
        Dim oConn As OleDbConnection = Nothing
        Dim oCmd As OleDbCommand
        ReadData(DataRow)
        Dim Lanjut As Boolean = isValid(DataRow, ActionType)

        If Lanjut = True Then
            Try
                oConn = New OleDbConnection(Constr)
                oConn.Open()
                If ActionType = cNew Then
                    Dim PID As Integer = GetOneData("SELECT PID FROM TaskPriority Where PriorityDescription = " & StrSQL(PriorityDescription), 0, Constr)
                    Dim SID As Integer = GetOneData("SELECT TOP 1 SID FROM TaskStatus Where StatusDescription= " & StrSQL(StatusDescription), 0, Constr)
                    SqlQuery = "
                    INSERT INTO TaskTable(PID, SID, TaskDescription, TaskDate, DateStart, DateCompleted, DateDeadline, AssignedTo, IsCompleted)
                    VALUES(" & PID & "," & SID & "," & StrSQL(TaskDescription) & "," & SQLDatetime(TaskDate) & "," & SQLDatetime(DateStart) & "," & SQLDatetime(DateCompleted) & "," & SQLDatetime(DateDeadline) & "," & StrSQL(AssignedTo) & "," & If(IsCompleted = "Yes", 1, 0) & ")"
                ElseIf ActionType = cEdit Then
                    SqlQuery = "UPDATE TaskTable 
                            SET TaskDescription =" & StrSQL(TaskDescription) & ", DateStart =" & SQLDatetime(DateStart) & ", DateCompleted =" & SQLDatetime(DateCompleted) & ",DateDeadline =" & SQLDatetime(DateDeadline) & ", AssignedTo =" & StrSQL(AssignedTo) & ",IsCompleted =" & If(IsCompleted = "Yes", 1, 0) &
                            " WHERE ID =" & DataRow.Item("ID")
                ElseIf ActionType = cDelete Then
                    SqlQuery = "DELETE FROM TaskTable Where ID =" & DataRow.Item("ID")
                End If
                oCmd = New OleDbCommand(SqlQuery, oConn)
                oCmd.ExecuteNonQuery()
            Catch ex As Exception
                Throw ex
            End Try
        End If
        Save = True
        mbInfo("Action Success")

    End Function

    Private Sub ReadData(ByVal DataRow As DataRow)
        With DataRow
            TaskDescription = IsNulls(DataRow.Item("TaskDescription"), "")
            StatusDescription = IsNulls(DataRow.Item("StatusDescription"), "")
            PriorityDescription = IsNulls(DataRow.Item("PriorityDescription"), "")
            DateCompleted = IsNulls(DataRow.Item("DateCompleted"), Now.Date)
            DateDeadline = IsNulls(DataRow.Item("DateDeadline"), Now.Date)
            DateStart = IsNulls(DataRow.Item("DateStart"), Now.Date)
            TaskDate = IsNulls(DataRow.Item("TaskDate"), Now.Date)
            IsCompleted = IsNulls(DataRow.Item("IsCompleted"), "")
            AssignedTo = IsNulls(DataRow.Item("AssignedTo"), "")
        End With
    End Sub
    Public Function isValid(ByVal DataRow As DataRow, ActionType As String) As Boolean
        isValid = False
        If DateDeadline < TaskDate and ActionType <> cdelete Then
            Throw New Exception("Deadline date must not be earlier than the start date of the task")
        End If

        If Role > 1 And ActionType = cDelete Then
            Throw New Exception("You didn't have access")
        End If

        isValid = True
        Return isValid
    End Function



    Public Function AddonText() As String Implements iclsList.AddonText
        Return Nothing
    End Function

    Public Function CallTypeList() As List(Of iclsCallType) Implements iclsList.CallTypeList
        Return template.CRUD
    End Function

    Public Function ColumnNumToFreeze() As String Implements iclsList.ColumnNumToFreeze
        Return Nothing
    End Function

    Public Function ConditionalFormat() As Dictionary(Of String, FormatConditionRuleExpression) Implements iclsList.ConditionalFormat
        Return Nothing
    End Function

    Public Function CustomFormat() As Dictionary(Of String, String) Implements iclsList.CustomFormat
        Return Nothing
    End Function

    Public Function RefreshData() As DataTable Implements iclsList.RefreshData
        dt = ExeDt("ListTask", {""}, {""})
        Return dt
    End Function

    Public Function SummaryColumn() As List(Of GridColumnSummaryItem) Implements iclsList.SummaryColumn
        Return Nothing
    End Function

End Class
