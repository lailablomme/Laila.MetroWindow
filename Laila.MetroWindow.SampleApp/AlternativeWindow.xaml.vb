Imports Laila.MetroWindow.Data
Imports System.Collections.ObjectModel

Public Class AlternativeWindow
    Private Sub AlternativeWindow_Loaded(sender As Object, e As RoutedEventArgs)
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

End Class
