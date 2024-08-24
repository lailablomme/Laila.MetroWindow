Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Xml.Serialization
Imports Laila.MetroWindow.Data

Class MainWindow
    Private Sub MetroWindow_Loaded(sender As Object, e As RoutedEventArgs)
        Dim undoButton As ButtonData = New ButtonData() With {
            .ImageSource = "pack://application:,,,/Laila.MetroWindow.SampleApp;component/Images/undo.ico",
            .Action = Async Function() As Task
                          MessageBox.Show("undo")
                          undoButton.IsEnabled = False
                      End Function
        }
        Dim helpButton As ButtonData = New ButtonData() With {
            .ImageSource = "pack://application:,,,/Laila.MetroWindow.SampleApp;component/Images/help.ico",
            .Action = Async Function() As Task
                          MessageBox.Show("help")
                      End Function
        }
        Dim infoButton As ButtonData = New ButtonData() With {
            .ImageSource = "pack://application:,,,/Laila.MetroWindow.SampleApp;component/Images/info.ico",
            .Action = Async Function() As Task
                          Await Task.Delay(1000)
                          MessageBox.Show("info")
                          infoButton.IsVisible = False
                      End Function
        }

        Me.LeftButtons = New ObservableCollection(Of Data.ButtonData) From {
            undoButton
        }
        Me.RightButtons = New ObservableCollection(Of Data.ButtonData) From {
            helpButton, infoButton
        }
    End Sub

    Private Const POSITION_FILENAME As String = "Laila.MetroWindow.SampleApp.WindowPosition.dat"

    Public Overrides Sub OnLoadPosition()
        ' load position from disk
        If File.Exists(Path.Combine(Path.GetTempPath(), POSITION_FILENAME)) Then
            Dim xml As String = File.ReadAllText(Path.Combine(Path.GetTempPath(), POSITION_FILENAME))
            Dim xmlBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(xml)
            Using memoryStream As MemoryStream = New MemoryStream(xmlBytes)
                Dim serializer As XmlSerializer = New XmlSerializer(GetType(WindowPositionData))
                Me.Position = serializer.Deserialize(memoryStream)
            End Using
        End If
    End Sub

    Public Overrides Sub OnSavePosition()
        ' write position to disk
        Using memoryStream As MemoryStream = New MemoryStream()
            Dim serializer As XmlSerializer = New XmlSerializer(GetType(WindowPositionData))
            serializer.Serialize(memoryStream, Me.Position)
            Dim xmlBytes As Byte() = memoryStream.ToArray()
            IO.File.WriteAllText(Path.Combine(Path.GetTempPath(), POSITION_FILENAME), System.Text.Encoding.UTF8.GetString(xmlBytes))
        End Using
    End Sub

    Private Sub ShowAlternativeWindowButton_Click(sender As Object, e As RoutedEventArgs) Handles ShowAlternativeWindowButton.Click
        Dim aw As AlternativeWindow = New AlternativeWindow()
        aw.Owner = Me
        aw.Show()
    End Sub
End Class
