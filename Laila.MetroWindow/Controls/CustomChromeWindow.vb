Imports System.ComponentModel
Imports System.Threading
Imports System.Windows
Imports System.Windows.Interop
Imports System.Windows.Shell

Namespace Controls
    Public Class CustomChromeWindow
        Inherits Window

        Private _originalTopMost As Boolean

        Public Sub Minimize()
            If Me.ResizeMode <> ResizeMode.NoResize Then
                SystemCommands.MinimizeWindow(Me)
            End If
        End Sub

        Public Sub Maximize()
            If Me.ResizeMode <> ResizeMode.CanMinimize AndAlso Me.ResizeMode <> ResizeMode.NoResize Then
                SystemCommands.MaximizeWindow(Me)
            End If
        End Sub

        Public Sub Restore()
            SystemCommands.RestoreWindow(Me)
        End Sub

        Public Sub ShowSystemMenu(p As Point)
            SystemCommands.ShowSystemMenu(Me, p)
        End Sub

        Protected Overrides Sub OnClosing(e As CancelEventArgs)
            MyBase.OnClosing(e)

            If Not e.Cancel AndAlso Not Me.Owner Is Nothing Then
                Dim deepOwner As Window = Me
                While Not deepOwner.Owner Is Nothing
                    deepOwner = deepOwner.Owner
                End While
                _originalTopMost = deepOwner.Topmost
                deepOwner.Topmost = True
            End If
        End Sub

        Protected Overrides Async Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)

            Await Task.Delay(250)
            Dim deepOwner As Window = Me
            While Not deepOwner.Owner Is Nothing
                deepOwner = deepOwner.Owner
            End While
            If _originalTopMost = False Then
                deepOwner.Topmost = False
            End If
        End Sub
    End Class
End Namespace
