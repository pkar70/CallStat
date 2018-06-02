' The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

Imports Windows.ApplicationModel.Calls
Imports Windows.ApplicationModel.DataTransfer
Imports Windows.UI.Popups
''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>

Public Class GrupaRozmow
    Public Property Nazwa As String
    Public Property iMissed As Integer = 0
    Public Property iCountOut As Integer = 0
    Public Property iCountIn As Integer = 0
    Public Property iMinOut As Integer = 0
    Public Property iMinIn As Integer = 0
End Class


Public NotInheritable Class MainPage
    Inherits Page

    Private mlDane As List(Of GrupaRozmow)
    Private miSort As Integer = 2
    Private mbSortDesc As Boolean = True



    Private Async Function ReadData() As Task
        mlDane = New List(Of GrupaRozmow)

        Dim oCallHist As PhoneCallHistoryStore = Nothing
        Dim sError = ""
        Try
            oCallHist = Await PhoneCallHistoryManager.RequestStoreAsync(PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite)
        Catch ex As Exception
            sError = ex.Message
        End Try

        If sError <> "" Then
            App.DialogBoxRes("errGetHist", sError)
            Exit Function
        End If

        If oCallHist Is Nothing Then
            App.DialogBoxRes("errCallHistNothing", Nothing)
            Exit Function
        End If

        Dim oHistReader = oCallHist.GetEntryReader()
        Dim sFirstDate = ""
        Dim iCallsMissed = 0
        Dim iCallsNumOut = 0
        Dim iCallsNumIn = 0
        Dim iCallsTimeOut = 0
        Dim iCallsTimeIn = 0
        Dim sFullData = ""
        Dim oHistData As IReadOnlyList(Of PhoneCallHistoryEntry)

        Dim iGuard = 0

        While iGuard < 1000
            iGuard += 1

            oHistData = Await oHistReader.ReadBatchAsync()
            If oHistData.Count < 1 Then Exit While

            For Each oCall In oHistData

                sFirstDate = oCall.StartTime.ToString   ' bo zaczyna od najmlodszego, czyli najstarsze bedzie ostatnie


                Dim oItem = mlDane.Find(Function(x) x.Nazwa.Equals(oCall.Address.DisplayName))
                If oItem Is Nothing Then
                    ' If Not mlDane.Exists(Function(x) x.Nazwa.Equals(oCall.Address.DisplayName)) Then
                    oItem = New GrupaRozmow
                    oItem.Nazwa = oCall.Address.DisplayName
                    mlDane.Add(oItem)
                End If

                ' 20180601: Crash [timespan_getvalue ze niby w PageLoaded, ale to moze byc tylko tu]
                '               "Or" na "OrElse", bez siegania do Duration gdy IsMissed
                '               oraz dodany warunek Is Nothing 
                If oCall.IsMissed OrElse oCall.Duration Is Nothing OrElse oCall.Duration.Value.TotalSeconds = 0 Then
                    oItem.iMissed += 1
                    iCallsMissed += 1
                Else
                    If oCall.IsIncoming Then
                        oItem.iCountIn += 1
                        oItem.iMinIn += oCall.Duration.Value.TotalMinutes

                        iCallsNumIn += 1
                        iCallsTimeIn += oCall.Duration.Value.TotalMinutes
                    Else
                        oItem.iCountOut += 1
                        oItem.iMinOut += oCall.Duration.Value.TotalMinutes

                        iCallsNumOut += 1
                        iCallsTimeOut += oCall.Duration.Value.TotalMinutes
                    End If
                End If

                sFullData = sFullData & oCall.Address.DisplayName & vbTab &
                    oCall.IsIncoming & vbTab &
                    oCall.StartTime.ToString & vbTab &
                    oCall.Duration.Value.TotalMinutes & vbCrLf
            Next
        End While

        Dim oClipCont = New DataPackage
        oClipCont.RequestedOperation = DataPackageOperation.Copy
        oClipCont.SetText(sFullData)
        Clipboard.SetContent(oClipCont)

        Stat2Page(sFirstDate, iCallsNumOut, iCallsNumIn, iCallsTimeOut, iCallsTimeIn)
    End Function

    Private Sub Stat2Page(sFirstDate As String, iCallsNumOut As Integer, iCallsNumIn As Integer, iCallsTimeOut As Integer, iCallsTimeIn As Integer)
        uiFirstDate.Text = "First date: " & sFirstDate

        uiCallsNumOut.Text = App.GetLangString("uiTxtOutCnt") & ": " & iCallsNumOut
        uiCallsNumIn.Text = App.GetLangString("uiTxtInCnt") & ": " & iCallsNumIn
        If iCallsNumOut > 0 Then
            uiCallsTimeOut.Text = App.GetLangString("uiTxtOutMin") & ": " & iCallsTimeOut & App.GetLangString("uiTxtMinAvg") & CInt(iCallsTimeOut / iCallsNumOut)
        Else
            uiCallsTimeOut.Text = App.GetLangString("uiTxtOutMin") & ": " & iCallsTimeOut & App.GetLangString("uiTxtMinAvg") & "0"
        End If
        If iCallsNumIn > 0 Then
            uiCallsTimeIn.Text = App.GetLangString("uiTxtInMin") & ": " & iCallsTimeIn & App.GetLangString("uiTxtMinAvg") & CInt(iCallsTimeIn / iCallsNumIn)
        Else
            uiCallsTimeIn.Text = App.GetLangString("uiTxtInMin") & ": " & iCallsTimeIn & App.GetLangString("uiTxtMinAvg") & "0"
        End If


    End Sub
    Private Sub ShowPersons()
        Select Case miSort
            Case 1
                If mbSortDesc Then
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iCountIn + c.iCountOut Descending
                Else
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iCountIn + c.iCountOut
                End If
            Case 2
                If mbSortDesc Then
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iMinIn + c.iMinOut Descending
                Else
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iMinIn + c.iMinOut
                End If
            Case 3
                If mbSortDesc Then
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iCountIn Descending
                Else
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iCountIn
                End If
            Case 4
                If mbSortDesc Then
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iMinIn Descending
                Else
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iMinIn
                End If
            Case 5
                If mbSortDesc Then
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iCountOut Descending
                Else
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iCountOut
                End If
            Case 6
                If mbSortDesc Then
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iMinOut Descending
                Else
                    ListItemsSklepu.ItemsSource = From c In mlDane Order By c.iMinOut
                End If
        End Select

    End Sub

    Private Async Sub uiPage_Loaded(sender As Object, e As RoutedEventArgs)

        Await ReadData() ' wraz z global stats
        uiResetMarks(App.GetSettingsInt("SortMode", 2), App.GetSettingsBool("SortDesc", True))
    End Sub

    Private Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)
        ShowPersons()
    End Sub

    Private Sub uiResetMarks(iMode As Integer, bDesc As Boolean)
        uiSortAllCnt.IsChecked = If(iMode = 1, True, False)
        uiSortAllMin.IsChecked = If(iMode = 2, True, False)
        uiSortInCnt.IsChecked = If(iMode = 3, True, False)
        uiSortInMin.IsChecked = If(iMode = 4, True, False)
        uiSortOutCnt.IsChecked = If(iMode = 5, True, False)
        uiSortOutMin.IsChecked = If(iMode = 6, True, False)
        uiSortDesc.IsChecked = If(bDesc, True, False)

        miSort = iMode
        mbSortDesc = bDesc
        App.SetSettingsInt("SortMode", miSort)
        App.SetSettingsBool("SortDesc", mbSortDesc)
        uiRefresh_Click(Nothing, Nothing)
    End Sub
    Private Sub uiSortDesc_Click(sender As Object, e As RoutedEventArgs) Handles uiSortDesc.Click
        uiResetMarks(miSort, Not mbSortDesc)
    End Sub

    Private Sub uiSortOutMin_Click(sender As Object, e As RoutedEventArgs) Handles uiSortOutMin.Click
        uiResetMarks(6, mbSortDesc)
    End Sub

    Private Sub uiSortOutCnt_Click(sender As Object, e As RoutedEventArgs) Handles uiSortOutCnt.Click
        uiResetMarks(5, mbSortDesc)
    End Sub

    Private Sub uiSortInMin_Click(sender As Object, e As RoutedEventArgs) Handles uiSortInMin.Click
        uiResetMarks(4, mbSortDesc)
    End Sub

    Private Sub uiSortInCnt_Click(sender As Object, e As RoutedEventArgs) Handles uiSortInCnt.Click
        uiResetMarks(3, mbSortDesc)
    End Sub

    Private Sub uiSortAllMin_Click(sender As Object, e As RoutedEventArgs) Handles uiSortAllMin.Click
        uiResetMarks(2, mbSortDesc)
    End Sub

    Private Sub uiSortAllCnt_Click(sender As Object, e As RoutedEventArgs) Handles uiSortAllCnt.Click
        uiResetMarks(1, mbSortDesc)
    End Sub
End Class
