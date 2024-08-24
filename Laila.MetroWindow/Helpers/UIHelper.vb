Imports System.Windows

Namespace Helpers
    Friend Class UIHelper
        Friend Shared Sub OnUIThread(action As System.Action)
            Dim app As Application = Application.Current
            If Not app Is Nothing Then
                If Not app.Dispatcher.CheckAccess() Then
                    app.Dispatcher.Invoke(
                        Sub()
                            action.Invoke()
                        End Sub)
                Else
                    action.Invoke()
                End If
            End If
        End Sub
    End Class
End Namespace