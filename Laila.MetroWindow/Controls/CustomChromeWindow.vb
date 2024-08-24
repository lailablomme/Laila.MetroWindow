Imports System.ComponentModel
Imports System.Threading
Imports System.Windows
Imports System.Windows.Interop
Imports System.Windows.Shell

Namespace Controls
    Public Class CustomChromeWindow
        Inherits Window

        Private Const SC_MINIMIZE As Integer = &HF020
        Private Const WM_SYSCOMMAND As Integer = &H112

        Private _hWnd As IntPtr
        Private _isMinimizing As Boolean = False
        Private _originalTopMost As Boolean
        Private Shared _originalOwnerTopMost As Boolean?
        Private _deepOwner As Window = Nothing
        Private Shared _lock As SemaphoreSlim = New SemaphoreSlim(1, 1)

        Public Sub New()
            MyBase.New()

            AddHandler Me.Loaded, AddressOf Window_Loaded
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            _hWnd = New WindowInteropHelper(Me).Handle
            HwndSource.FromHwnd(_hWnd).AddHook(AddressOf WindowProc)
        End Sub

        Private Function WindowProc(hwnd As IntPtr, msg As Int32, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
            Select Case msg
                Case WM_SYSCOMMAND
                    Select Case wParam.ToInt32()
                        Case SC_MINIMIZE
                            ' hack
                            _isMinimizing = True
                            _originalTopMost = Me.Topmost
                            Me.Topmost = True
                    End Select
            End Select
        End Function

        Protected Overrides Sub OnStateChanged(e As EventArgs)
            MyBase.OnStateChanged(e)

            If _isMinimizing Then
                Me.Topmost = _originalTopMost
                _isMinimizing = False
            End If
        End Sub

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
                _lock.Wait()
                If Not _originalOwnerTopMost.HasValue Then
                    _deepOwner = Me
                    While Not _deepOwner.Owner Is Nothing
                        _deepOwner = _deepOwner.Owner
                    End While
                    _originalOwnerTopMost = _deepOwner.Topmost
                    _deepOwner.Topmost = True
                End If
                _lock.Release()
            End If
        End Sub

        Protected Overrides Async Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)

            If Not _deepOwner Is Nothing Then
                Await Task.Delay(250)
                Await _lock.WaitAsync()
                _deepOwner.Topmost = _originalOwnerTopMost
                _originalOwnerTopMost = Nothing
                _lock.Release()
            End If
        End Sub
    End Class
End Namespace
