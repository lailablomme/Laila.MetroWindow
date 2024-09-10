Imports System.ComponentModel
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Interop
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Imaging

Namespace Controls
    Public Class CustomChromeWindow
        Inherits Window

        Private Const MINIMIZE_SPEED As Integer = 250
        Private Const WM_SYSCOMMAND As Integer = &H112
        Private Const SC_MINIMIZE As Integer = &HF020
        Private Const SC_RESTORE As Integer = &HF120

        Private _originalTopMost As Boolean
        Private _minimizeImage As RenderTargetBitmap
        Private _skip As Boolean
        Private _s As System.Windows.Forms.Screen
        Private _g As System.Drawing.Graphics
        Private _isMinimized As Boolean = False

        Private Async Sub doRestoreAnimPt1(w As Window)
            w.Left = _s.WorkingArea.Left / (_g.DpiX / 96.0)
            w.Top = _s.WorkingArea.Top / (_g.DpiY / 96.0)
            w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_g.DpiX / 96.0)
            w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_g.DpiY / 96.0)
            w.Margin = New Thickness(
                (_s.WorkingArea.Left - _s.Bounds.Left) / (_g.DpiY / 96.0),
                (_s.WorkingArea.Top - _s.Bounds.Top) / (_g.DpiY / 96.0),
                (_s.Bounds.Right - _s.WorkingArea.Right) / (_g.DpiY / 96.0),
                (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_g.DpiY / 96.0))
            w.Content = New Image() With {
                .Source = _minimizeImage,
                .Width = Me.ActualWidth,
                .Height = Me.ActualHeight,
                .Margin = New Thickness(w.Width / 2 - 100, _s.WorkingArea.Bottom / (_g.DpiY / 96.0), 0, 0),
                .VerticalAlignment = VerticalAlignment.Top,
                .HorizontalAlignment = Windows.HorizontalAlignment.Left
            }

            w.Show()

            Dim ease As SineEase = New SineEase()
            ease.EasingMode = EasingMode.EaseInOut
            Dim ta As ThicknessAnimation =
                    New ThicknessAnimation(
                        CType(w.Content, Image).Margin,
                        New Thickness(Me.Left - _s.WorkingArea.Left / (_g.DpiX / 96.0), Me.Top - w.Margin.Top - _s.WorkingArea.Top / (_g.DpiY / 96.0), 0, 0), New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da As DoubleAnimation = New DoubleAnimation(200, Me.Width, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da2 As DoubleAnimation = New DoubleAnimation(0, 1, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            ta.EasingFunction = ease

            CType(w.Content, Image).BeginAnimation(Image.MarginProperty, ta)
            CType(w.Content, Image).BeginAnimation(Image.WidthProperty, da)
            CType(w.Content, Image).BeginAnimation(Image.OpacityProperty, da2)

            Await Task.Delay(MINIMIZE_SPEED)
            _skip = False
            SystemCommands.RestoreWindow(Me)

            Await Task.Delay(100)

            w.Close()
            _g.Dispose()
        End Sub

        Private Sub doMinimizeAnimPt1(w As Window)
            _minimizeImage = New RenderTargetBitmap(Me.ActualWidth, Me.ActualHeight, 96, 96, PixelFormats.Pbgra32)
            _minimizeImage.Render(Me)

            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            _s = Forms.Screen.FromHandle(hWnd)
            _g = System.Drawing.Graphics.FromHwndInternal(hWnd)

            w.Left = _s.WorkingArea.Left / (_g.DpiX / 96.0)
            w.Top = _s.WorkingArea.Top / (_g.DpiY / 96.0)
            w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_g.DpiX / 96.0)
            w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_g.DpiY / 96.0)
            w.Margin = New Thickness(
                (_s.WorkingArea.Left - _s.Bounds.Left) / (_g.DpiY / 96.0),
                (_s.WorkingArea.Top - _s.Bounds.Top) / (_g.DpiY / 96.0),
                (_s.Bounds.Right - _s.WorkingArea.Right) / (_g.DpiY / 96.0),
                (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_g.DpiY / 96.0))
            w.Content = New Image() With {
                .Source = _minimizeImage,
                .Width = Me.ActualWidth,
                .Height = Me.ActualHeight,
                .Margin = New Thickness(Me.Left - _s.WorkingArea.Left / (_g.DpiX / 96.0), Me.Top - w.Margin.Top - _s.WorkingArea.Top / (_g.DpiY / 96.0), 0, 0),
                .VerticalAlignment = VerticalAlignment.Top,
                .HorizontalAlignment = Windows.HorizontalAlignment.Left
            }

            w.Show()
        End Sub

        Private Async Sub doMinimizeAnimPt2(w As Window)
            Dim ease As SineEase = New SineEase()
            ease.EasingMode = EasingMode.EaseInOut
            Dim ta As ThicknessAnimation =
                    New ThicknessAnimation(
                        CType(w.Content, Image).Margin,
                        New Thickness(w.Width / 2 - 100, _s.WorkingArea.Bottom / (_g.DpiY / 96.0), 0, 0), New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da As DoubleAnimation = New DoubleAnimation(Me.ActualWidth, 200, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da2 As DoubleAnimation = New DoubleAnimation(1, 0, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            ta.EasingFunction = ease

            CType(w.Content, Image).BeginAnimation(Image.MarginProperty, ta)
            CType(w.Content, Image).BeginAnimation(Image.WidthProperty, da)
            CType(w.Content, Image).BeginAnimation(Image.OpacityProperty, da2)

            Await Task.Delay(MINIMIZE_SPEED)

            w.Close()
        End Sub

        Protected Overrides Sub OnSourceInitialized(e As EventArgs)
            MyBase.OnSourceInitialized(e)

            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            Dim source As HwndSource = HwndSource.FromHwnd(hWnd)
            source.AddHook(AddressOf HwndHook)
        End Sub

        Private Function makeWindow() As Window
            Return New Window() With {
                .WindowStyle = WindowStyle.None,
                .AllowsTransparency = True,
                .Background = Brushes.Transparent,
                .ShowInTaskbar = False,
                .Topmost = True
            }
        End Function

        Private Function HwndHook(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
            Select Case msg
                Case WM_SYSCOMMAND
                    Select Case wParam
                        Case SC_MINIMIZE
                            If Not _isMinimized Then
                                _isMinimized = True

                                Dim w As Window = makeWindow()

                                _skip = True
                                doMinimizeAnimPt1(w)
                                Windows.Application.Current.Dispatcher.BeginInvoke(
                                    Sub()
                                        doMinimizeAnimPt2(w)
                                    End Sub)
                            End If
                        Case SC_RESTORE
                            If _isMinimized Then
                                _isMinimized = False

                                Dim w As Window = makeWindow()

                                If _skip Then
                                    handled = True
                                    doRestoreAnimPt1(w)
                                End If
                            End If
                    End Select
            End Select

            Return IntPtr.Zero
        End Function

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
                If Not Me.Equals(deepOwner) Then
                    _originalTopMost = deepOwner.Topmost
                    deepOwner.Topmost = True
                End If
            End If
        End Sub

        Protected Overrides Async Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)

            Await Task.Delay(150)
            Dim deepOwner As Window = Me
            While Not deepOwner.Owner Is Nothing
                deepOwner = deepOwner.Owner
            End While
            If Not Me.Equals(deepOwner) Then
                If _originalTopMost = False Then
                    deepOwner.Topmost = False
                End If
                deepOwner.Activate()
            End If
        End Sub
    End Class
End Namespace
