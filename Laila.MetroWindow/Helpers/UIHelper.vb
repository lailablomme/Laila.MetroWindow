Imports System.Windows
Imports System.Windows.Threading

Namespace Helpers
    Friend Class UIHelper
        Friend Shared Sub OnUIThread(action As System.Action, Optional priority As DispatcherPriority = DispatcherPriority.Normal)
            Dim app As Application = Application.Current
            If Not app Is Nothing Then
                If Not app.Dispatcher.CheckAccess() OrElse priority <> DispatcherPriority.Normal Then
                    app.Dispatcher.Invoke(
                        Sub()
                            action.Invoke()
                        End Sub, priority)
                Else
                    action.Invoke()
                End If
            End If
        End Sub

        Friend Shared Sub OnUIThreadAsync(action As System.Action, Optional priority As DispatcherPriority = DispatcherPriority.Normal)
            Dim app As Application = Application.Current
            If Not app Is Nothing Then
                app.Dispatcher.BeginInvoke(
                    Sub()
                        action.Invoke()
                    End Sub, priority)
            End If
        End Sub
    End Class
End Namespace