Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Globalization
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Interop
Imports System.Windows.Markup
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Media.Effects
Imports System.Windows.Media.Imaging
Imports Laila.MetroWindow.Data
Imports Laila.MetroWindow.Helpers

Namespace Controls
    Public Class MetroWindow
        Inherits Window

        Public Shared ReadOnly GlowSizeProperty As DependencyProperty = DependencyProperty.Register("GlowSize", GetType(Double), GetType(MetroWindow), New FrameworkPropertyMetadata(Convert.ToDouble(15), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly GlowStyleProperty As DependencyProperty = DependencyProperty.Register("GlowStyle", GetType(GlowStyle), GetType(MetroWindow), New FrameworkPropertyMetadata(Data.GlowStyle.Glowing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly GlowColorProperty As DependencyProperty = DependencyProperty.Register("GlowColor", GetType(Media.Color), GetType(MetroWindow), New FrameworkPropertyMetadata(Media.Colors.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly CaptionHeightProperty As DependencyProperty = DependencyProperty.Register("CaptionHeight", GetType(Double), GetType(MetroWindow), New FrameworkPropertyMetadata(Convert.ToDouble(31), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly DoIntegrateMenuProperty As DependencyProperty = DependencyProperty.Register("DoIntegrateMenu", GetType(Boolean), GetType(MetroWindow), New FrameworkPropertyMetadata(True, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly DoShowChromeProperty As DependencyProperty = DependencyProperty.Register("DoShowChrome", GetType(Boolean), GetType(MetroWindow), New FrameworkPropertyMetadata(True, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly LeftButtonsProperty As DependencyProperty = DependencyProperty.Register("LeftButtons", GetType(ObservableCollection(Of ButtonData)), GetType(MetroWindow), New FrameworkPropertyMetadata(Nothing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly RightButtonsProperty As DependencyProperty = DependencyProperty.Register("RightButtons", GetType(ObservableCollection(Of ButtonData)), GetType(MetroWindow), New FrameworkPropertyMetadata(Nothing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly ActualWindowStateProperty As DependencyProperty = DependencyProperty.Register("ActualWindowState", GetType(WindowState), GetType(MetroWindow), New FrameworkPropertyMetadata(WindowState.Normal, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))

        Public Property GlowColor() As Media.Color
            Get
                Return GetValue(GlowColorProperty)
            End Get
            Set(ByVal value As Media.Color)
                SetCurrentValue(GlowColorProperty, value)
            End Set
        End Property

        Public Property DoIntegrateMenu() As Boolean
            Get
                Return GetValue(DoIntegrateMenuProperty)
            End Get
            Set(ByVal value As Boolean)
                SetCurrentValue(DoIntegrateMenuProperty, value)
            End Set
        End Property

        Public Property DoShowChrome() As Boolean
            Get
                Return GetValue(DoShowChromeProperty)
            End Get
            Set(ByVal value As Boolean)
                SetCurrentValue(DoShowChromeProperty, value)
            End Set
        End Property

        Public Property CaptionHeight() As Double
            Get
                Return GetValue(CaptionHeightProperty)
            End Get
            Set(ByVal value As Double)
                SetCurrentValue(CaptionHeightProperty, value)
            End Set
        End Property

        Public Property LeftButtons() As ObservableCollection(Of ButtonData)
            Get
                Return GetValue(LeftButtonsProperty)
            End Get
            Set(ByVal value As ObservableCollection(Of ButtonData))
                SetCurrentValue(LeftButtonsProperty, value)
            End Set
        End Property

        Public Property RightButtons() As ObservableCollection(Of ButtonData)
            Get
                Return GetValue(RightButtonsProperty)
            End Get
            Set(ByVal value As ObservableCollection(Of ButtonData))
                SetCurrentValue(RightButtonsProperty, value)
            End Set
        End Property

        Private Const MINIMIZE_SPEED As Integer = 250
        Private Const MAXIMIZE_SPEED As Integer = 200
        Private Const CLOSE_SPEED As Integer = 250
        Private Const WM_SYSCOMMAND As Integer = &H112
        Private Const SC_MINIMIZE As Integer = &HF020
        Private Const SC_MAXIMIZE As Integer = &HF030
        Private Const SC_RESTORE As Integer = &HF120
        Private Const WM_NCLBUTTONDBLCLK As Integer = &HA3

        Private _minimizeImage As RenderTargetBitmap
        Private _maximizeImage As RenderTargetBitmap
        Private _closeImage As RenderTargetBitmap
        Private _skipMinimize As Boolean
        Private _skipMaximize As Boolean
        Private _s As System.Windows.Forms.Screen
        Private _dpi As DpiScale
        Private _isMinimized As Boolean = False
        Private _wasMaximized As Boolean = False
        Private _windowGuid As String = "9bf6faf8-7fdc-4525-93e5-9cd8f97209a4"
        Private _originalSizeToContent As SizeToContent
        Private PART_TitleBar As ContentControl
        Private PART_RootBorder As Border
        Private PART_MainBorder As Border
        Private PART_MainGrid As Grid
        Private PART_MenuPlaceHolder As Grid
        Private PART_Text As TextBlock
        Private PART_IconButton As Button
        Private PART_MinimizeButton As Button
        Private PART_MaximizeRestoreButton As Button
        Private PART_CloseButton As Button
        Private _isMenuIntegrated As Boolean = False
        Private _position As WindowPositionData = Nothing
        Private _previousPosition As WindowPositionData = New WindowPositionData()
        Private _noPositionCorrection As Boolean = False
        Private _dontUpdatePosition As Boolean = True
        Private _isAnimating As Boolean
        Private _isReallyClosing As Boolean
        Private _renderingTier As Integer

        Shared Sub New()
            DefaultStyleKeyProperty.OverrideMetadata(GetType(MetroWindow), New FrameworkPropertyMetadata(GetType(MetroWindow)))
        End Sub

        Public Sub New()
            Me.DefaultStyleKey = GetType(MetroWindow)
            Me.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)

            _renderingTier = RenderCapability.Tier >> 16

            AddHandler Me.Loaded,
                Sub(sender As Object, e As EventArgs)
                    If Me.DoShowChrome Then
                        integrateMenu()
                        Me.CenterTitle()
                    End If
                End Sub

            AddHandler Me.SizeChanged,
                Sub(sender2 As Object, e2 As EventArgs)
                    If Me.DoShowChrome Then
                        ' save size
                        If Me.WindowState = WindowState.Normal AndAlso Not _dontUpdatePosition Then
                            _previousPosition = _position.Clone()
                            _position.Width = Me.Width
                            _position.Height = Me.Height
                        End If

                        If Not _noPositionCorrection _
                        AndAlso Not (Me.ResizeMode = ResizeMode.CanResize OrElse Me.ResizeMode = ResizeMode.CanResizeWithGrip) Then
                            ' keep window within this screen
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                                Sub()
                                    Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
                                    Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromHwnd(hWnd)
                                    Dim s As Forms.Screen = Forms.Screen.FromHandle(hWnd)
                                    Me.MaxWidth = s.WorkingArea.Width / (g.DpiX / 96.0)
                                    Me.MaxHeight = s.WorkingArea.Height / (g.DpiX / 96.0)
                                    If Me.Top + Me.Height > s.WorkingArea.Bottom / (g.DpiY / 96.0) Then
                                        Me.Top = s.WorkingArea.Bottom / (g.DpiY / 96.0) - Me.Height
                                    End If
                                    If Me.Left + Me.Width > s.WorkingArea.Right / (g.DpiY / 96.0) Then
                                        Me.Left = s.WorkingArea.Right / (g.DpiY / 96.0) - Me.Width
                                    End If
                                    g.Dispose()
                                End Sub)
                        End If
                        _noPositionCorrection = False

                        Me.CenterTitle()
                    End If
                End Sub

            AddHandler Me.Closed,
                Sub(sender As Object, e As EventArgs)
                    If Me.DoShowChrome Then Me.OnSavePosition()
                End Sub
        End Sub

        Private Sub buttonCollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
            Me.CenterTitle()
        End Sub

        Private Sub integrateMenu()
            If Not _isMenuIntegrated AndAlso Me.DoIntegrateMenu AndAlso Not PART_MenuPlaceHolder Is Nothing Then
                Dim menus As IEnumerable(Of Menu) = FindVisualChildren(Of Menu)(Me)
                If menus.Count > 0 Then
                    _isMenuIntegrated = True

                    ' integrate menu
                    Dim m As Menu = menus(0)
                    Dim p As DependencyObject = m.Parent
                    If TypeOf p Is Panel Then
                        CType(p, Panel).Children.Remove(m)
                    ElseIf TypeOf p Is Decorator Then
                        CType(p, Decorator).Child = Nothing
                    ElseIf TypeOf p Is ContentPresenter Then
                        CType(p, ContentPresenter).Content = Nothing
                    ElseIf TypeOf p Is ContentControl Then
                        CType(p, ContentControl).Content = Nothing
                    End If
                    PART_MenuPlaceHolder.Children.Add(m)
                    m.Margin = New Thickness(0, 0, 5, 0)

                    AddHandler m.SizeChanged,
                        Sub()
                            Me.CenterTitle()
                        End Sub
                End If
            End If
        End Sub

        Private Sub CenterTitle()
            If Me.DoIntegrateMenu AndAlso Not PART_MenuPlaceHolder Is Nothing AndAlso Not PART_Text Is Nothing AndAlso Me.DoShowChrome Then
                Dim leftCentered As Double = (PART_MainBorder.ActualWidth - PART_Text.ActualWidth) / 2
                PART_Text.Margin = New Thickness(leftCentered, PART_Text.Margin.Top, PART_Text.Margin.Right, PART_Text.Margin.Bottom)
                PART_Text.UpdateLayout()
            End If
        End Sub

        Public Shared Iterator Function FindVisualChildren(Of T As DependencyObject)(depObj As DependencyObject) As IEnumerable(Of T)
            For i = 0 To VisualTreeHelper.GetChildrenCount(depObj) - 1
                Dim child As DependencyObject = VisualTreeHelper.GetChild(depObj, i)
                If Not child Is Nothing AndAlso TypeOf child Is T Then
                    Yield child
                Else
                    For Each childOfChild In FindVisualChildren(Of T)(child)
                        Yield childOfChild
                    Next
                End If
            Next
        End Function

        Protected Overrides Sub OnSourceInitialized(e As EventArgs)
            MyBase.OnSourceInitialized(e)

            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            Dim source As HwndSource = HwndSource.FromHwnd(hWnd)
            source.AddHook(AddressOf HwndHook)

            If Me.DoShowChrome Then
                Me.SetChromeWindow()

                ' help size to content a hand
                _originalSizeToContent = Me.SizeToContent
                If Me.SizeToContent <> SizeToContent.Manual Then
                    PART_RootBorder.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
                    Dim size As Size = PART_RootBorder.DesiredSize
                    Me.SizeToContent = SizeToContent.Manual
                    If _originalSizeToContent = SizeToContent.WidthAndHeight OrElse _originalSizeToContent = SizeToContent.Width Then
                        Me.Width = size.Width
                    End If
                    If _originalSizeToContent = SizeToContent.WidthAndHeight OrElse _originalSizeToContent = SizeToContent.Height Then
                        Me.Height = size.Height
                    End If

                    Me.SizeToContent = _originalSizeToContent
                End If

                ' window startup location
                Dim newLeft As Double, newTop As Double, s As Forms.Screen, g As System.Drawing.Graphics
                If Me.WindowStartupLocation = WindowStartupLocation.CenterOwner Then
                    ' center owner
                    Dim owner As Window = If(Not Me.Owner Is Nothing, Me.Owner, Application.Current.MainWindow)
                    If Not owner Is Nothing AndAlso owner.WindowState = WindowState.Normal Then
                        newLeft = owner.Left + owner.Width / 2 - Me.Width / 2
                        newTop = owner.Top + owner.Height / 2 - Me.Height / 2
                    ElseIf Not owner Is Nothing AndAlso owner.WindowState = WindowState.Maximized Then
                        hWnd = New WindowInteropHelper(owner).Handle
                        s = Forms.Screen.FromHandle(hWnd)
                        g = System.Drawing.Graphics.FromHwnd(hWnd)
                        newLeft = s.WorkingArea.Left / (g.DpiX / 96.0) + s.WorkingArea.Width / (g.DpiX / 96.0) / 2 - Me.Width / 2
                        newTop = s.WorkingArea.Top / (g.DpiX / 96.0) + s.WorkingArea.Height / (g.DpiX / 96.0) / 2 - Me.Height / 2
                        g.Dispose()
                    Else
                        hWnd = New WindowInteropHelper(Me).Handle
                        s = Forms.Screen.FromHandle(hWnd)
                        g = System.Drawing.Graphics.FromHwnd(hWnd)
                        newLeft = s.WorkingArea.Left / (g.DpiX / 96.0) + s.WorkingArea.Width / (g.DpiX / 96.0) / 2 - Me.Width / 2
                        newTop = s.WorkingArea.Top / (g.DpiX / 96.0) + s.WorkingArea.Height / (g.DpiX / 96.0) / 2 - Me.Height / 2
                        g.Dispose()
                    End If
                ElseIf Me.WindowStartupLocation <> WindowStartupLocation.Manual Then
                    ' center screen
                    hWnd = New WindowInteropHelper(Me).Handle
                    s = Forms.Screen.FromHandle(hWnd)
                    g = System.Drawing.Graphics.FromHwnd(hWnd)
                    newLeft = (s.WorkingArea.Left + s.WorkingArea.Width / 2) / (g.DpiX / 96.0) - Me.Width / 2
                    newTop = (s.WorkingArea.Top + s.WorkingArea.Height / 2) / (g.DpiY / 96.0) - Me.Height / 2
                    g.Dispose()
                Else
                    ' manual
                    newLeft = Me.Left
                    newTop = Me.Top
                End If

                Me.Left = newLeft
                Me.Top = newTop

                Me.OnLoadPosition()

                If _position Is Nothing Then
                    ' don't restore position
                    _position = New WindowPositionData()
                Else
                    ' determine how much percent of the titlebar is visible on all screens combined
                    Dim visiblePercent As Double = 0
                    For Each screen In System.Windows.Forms.Screen.AllScreens
                        Dim dpiX As UInt32, dpiY As UInt32
                        screen.GetDpi(dpiX, dpiY)
                        Dim rectWindow As Rect = New Rect(
                            _position.Left / (dpiY / 96.0),
                            _position.Top / (dpiY / 96.0),
                            _position.Width / (dpiY / 96.0),
                            Me.CaptionHeight / (dpiY / 96.0))
                        Dim rectScreen As Rect = New Rect(
                            screen.WorkingArea.X,
                            screen.WorkingArea.Y,
                            screen.WorkingArea.Right - screen.WorkingArea.Left,
                            screen.WorkingArea.Bottom - screen.WorkingArea.Top)
                        Dim intersectRect As Rect = Rect.Intersect(rectWindow, rectScreen)
                        If Not Math.Abs(intersectRect.Width) = Double.PositiveInfinity AndAlso Not Math.Abs(intersectRect.Height) = Double.PositiveInfinity Then
                            visiblePercent += (intersectRect.Width * intersectRect.Height) / (rectWindow.Width * rectWindow.Height) * 100
                        End If
                    Next

                    ' if 25% or more of the titlebar is visible on all screens, restore position
                    If visiblePercent >= 25 Then
                        Me.Width = _position.Width
                        Me.Height = _position.Height
                        Me.Left = _position.Left
                        Me.Top = _position.Top
                    End If

                    ' restore state, unless it was minimized
                    If _position.State <> WindowState.Minimized Then
                        Me.WindowState = _position.State
                        Me.ActualWindowState = Me.WindowState
                    End If
                End If

                ' start recording position
                _dontUpdatePosition = False

                Me.CenterTitle()

                ' if this window is in exact the same position as another window in our application, cascade it
                Dim i As Integer = 1
                Dim hasBeenTheEnd As Boolean = False
                For x = 0 To System.Windows.Application.Current.Windows.Count - 1
                    Dim window As Window = System.Windows.Application.Current.Windows(x)
                    If Not window.Equals(Me) _
                        AndAlso Math.Abs(window.Left - Me.Left) < 2 AndAlso Math.Abs(window.Top - Me.Top) < 2 _
                        AndAlso Math.Abs(window.Width - Me.Width) < 2 AndAlso Math.Abs(window.Height - Me.Height) < 2 Then
                        Me.Left += 30
                        Me.Top += 30
                        x = 0

                        hWnd = New WindowInteropHelper(window).Handle
                        s = Forms.Screen.FromHandle(hWnd)
                        g = System.Drawing.Graphics.FromHwnd(hWnd)

                        If Me.Top + Me.Height > s.WorkingArea.Bottom / (g.DpiY / 96.0) Then
                            Me.Left = i * 30
                            Me.Top = 30
                            i += 1
                        End If
                        If Me.Left + Me.Width > s.WorkingArea.Right / (g.DpiY / 96.0) Then
                            If Not hasBeenTheEnd Then
                                i = 1
                                Me.Left = i * 30
                                Me.Top = 30
                                hasBeenTheEnd = True
                            Else
                                ' give up
                                Exit For
                            End If
                        End If

                        g.Dispose()
                    End If
                Next

                ' save pos in case this is the first time
                _position.State = Me.WindowState
                _position.Left = Me.Left
                _position.Top = Me.Top
                _position.Width = Me.Width
                _position.Height = Me.Height
                _previousPosition = _position.Clone()
            End If
        End Sub

        Protected Sub SetChromeWindow()
            Dim border As Double = 0
            If Me.GlowStyle = GlowStyle.Glowing Then
                border = Me.GlowSize
            ElseIf Me.GlowStyle = GlowStyle.Shadow Then
                border = Me.GlowSize / 2
            End If

            SetValue(System.Windows.Shell.WindowChrome.WindowChromeProperty,
                New System.Windows.Shell.WindowChrome() With {
                    .CaptionHeight = Me.CaptionHeight,
                    .ResizeBorderThickness = If(Me.WindowState = WindowState.Maximized, New Thickness(5),
                        If(Me.ResizeMode = ResizeMode.CanResize OrElse Me.ResizeMode = ResizeMode.CanResizeWithGrip, New Thickness(7 + border), New Thickness(0))),
                    .CornerRadius = New CornerRadius(0),
                    .GlassFrameThickness = If(Me.GlowStyle = GlowStyle.None, New Thickness(0, 0, 0, 1), New Thickness(0))
                })
        End Sub

        Public Overrides Sub OnApplyTemplate()
            MyBase.OnApplyTemplate()

            PART_MenuPlaceHolder = Me.Template.FindName("PART_MenuPlaceHolder", Me)
            PART_RootBorder = Me.Template.FindName("PART_RootBorder", Me)
            PART_MainBorder = Me.Template.FindName("PART_MainBorder", Me)
            PART_MainGrid = Me.Template.FindName("PART_MainGrid", Me)
            PART_TitleBar = Me.Template.FindName("PART_TitleBar", Me)
            PART_Text = Me.Template.FindName("PART_Text", Me)
            PART_IconButton = Me.Template.FindName("PART_IconButton", Me)
            PART_MinimizeButton = Me.Template.FindName("PART_MinimizeButton", Me)
            PART_MaximizeRestoreButton = Me.Template.FindName("PART_MaximizeRestoreButton", Me)
            PART_CloseButton = Me.Template.FindName("PART_CloseButton", Me)

            If Me.DoShowChrome Then
                If Not PART_RootBorder Is Nothing Then
                    Dim paddingDescriptor As DependencyPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(Border.PaddingProperty, GetType(Border))
                    paddingDescriptor.AddValueChanged(PART_RootBorder,
                        Sub(sender As Object, e As EventArgs)
                            Me.CenterTitle()
                        End Sub)
                End If

                If Not PART_MainBorder Is Nothing Then
                    AddHandler PART_MainBorder.SizeChanged,
                        Sub()
                            Me.CenterTitle()
                        End Sub
                End If

                If Not PART_TitleBar Is Nothing Then
                    PART_TitleBar.Focus()
                    Keyboard.ClearFocus()
                    PART_TitleBar.Focusable = False
                End If

                If Not PART_IconButton Is Nothing Then
                    AddHandler PART_IconButton.Click,
                        Sub()
                            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
                            Dim s As System.Windows.Forms.Screen = Forms.Screen.FromHandle(hWnd)
                            Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromHwnd(hWnd)
                            Dim p As Point = PART_IconButton.PointToScreen(New Point(0, PART_IconButton.ActualHeight))
                            p.X = p.X / (g.DpiX / 96.0)
                            p.Y = p.Y / (g.DpiY / 96.0)
                            Me.ShowSystemMenu(p)
                            g.Dispose()
                        End Sub
                    AddHandler PART_IconButton.MouseUp, AddressOf onShowSystemMenu
                    AddHandler PART_IconButton.MouseDoubleClick,
                        Sub()
                            Me.Close()
                        End Sub
                End If

                If Not PART_MinimizeButton Is Nothing Then
                    AddHandler PART_MinimizeButton.Click,
                        Sub()
                            Me.Minimize()
                        End Sub
                    AddHandler PART_MinimizeButton.MouseUp, AddressOf onShowSystemMenu
                End If

                If Not PART_MaximizeRestoreButton Is Nothing Then
                    AddHandler PART_MaximizeRestoreButton.Click,
                        Sub()
                            If Me.WindowState = WindowState.Maximized Then
                                Me.Restore()
                            Else
                                Me.Maximize()
                            End If
                        End Sub
                    AddHandler PART_MaximizeRestoreButton.MouseUp, AddressOf onShowSystemMenu
                End If

                If Not PART_CloseButton Is Nothing Then
                    AddHandler PART_CloseButton.Click,
                        Sub()
                            Me.Close()
                        End Sub
                    AddHandler PART_CloseButton.MouseUp, AddressOf onShowSystemMenu
                End If
            End If

            setMargin()
        End Sub

        Private Sub onShowSystemMenu(sender As Object, e As MouseButtonEventArgs)
            If e.RightButton = MouseButtonState.Released Then
                Dim p As Point = e.GetPosition(Me)
                p.X += Me.Left
                p.Y += Me.Top
                Me.ShowSystemMenu(p)
            End If
        End Sub

        Protected Overrides Sub OnLocationChanged(e As EventArgs)
            MyBase.OnLocationChanged(e)

            If Me.WindowState = WindowState.Normal AndAlso Not _dontUpdatePosition AndAlso Me.DoShowChrome Then
                _previousPosition = _position.Clone()
                _position.Left = Me.Left
                _position.Top = Me.Top
            End If
        End Sub

        Protected Overrides Sub OnStateChanged(e As EventArgs)
            If Me.DoShowChrome Then
                SetChromeWindow()

                ' save state
                If Not _dontUpdatePosition Then
                    If Me.WindowState <> WindowState.Normal Then
                        _position = _previousPosition.Clone()
                    End If
                    _position.State = Me.WindowState
                    _noPositionCorrection = True
                End If

                setMargin()

                If Me.WindowState = WindowState.Maximized AndAlso Not _isAnimating Then
                    Me.PART_RootBorder.Padding = New Thickness()
                ElseIf Me.WindowState = WindowState.Normal AndAlso Not _isAnimating Then
                    Me.PART_RootBorder.Padding = New Thickness(
                            If(Me.GlowStyle = GlowStyle.Glowing, Me.GlowSize, 0),
                            If(Me.GlowStyle = GlowStyle.Glowing, Me.GlowSize, 0),
                            Me.GlowSize,
                            Me.GlowSize)
                    Me.ActualWindowState = WindowState.Normal
                End If
            End If
        End Sub

        Private Sub setMargin()
            If Me.WindowState = WindowState.Maximized Then
                ' set maximized margins to not get under the taskbar
                Dim margin As Thickness = New Thickness()
                If System.Environment.OSVersion.Version.Major >= 6 Then
                    margin = New Thickness(6)
                Else
                    margin = New Thickness(4)
                End If

                If Me.WindowStyle = WindowStyle.None AndAlso Me.DoShowChrome Then
                    Dim currentScreen As System.Windows.Forms.Screen = System.Windows.Forms.Screen.FromHandle(New WindowInteropHelper(Me).Handle)
                    Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromHwndInternal(New WindowInteropHelper(Me).Handle)
                    margin = New Thickness(
                        (currentScreen.WorkingArea.Left - currentScreen.Bounds.Left) / (g.DpiY / 96.0) + margin.Left,
                        (currentScreen.WorkingArea.Top - currentScreen.Bounds.Top) / (g.DpiY / 96.0) + margin.Top,
                        (currentScreen.Bounds.Right - currentScreen.WorkingArea.Right) / (g.DpiY / 96.0) + margin.Right,
                        (currentScreen.Bounds.Bottom - currentScreen.WorkingArea.Bottom) / (g.DpiY / 96.0) + margin.Bottom)
                    g.Dispose()
                End If

                If Not PART_RootBorder Is Nothing Then
                    PART_RootBorder.Margin = margin
                End If
            Else
                ' not maximized
                If Not PART_RootBorder Is Nothing Then
                    PART_RootBorder.Margin = New Thickness(0)
                End If
            End If
        End Sub

        Public Overridable Sub OnLoadPosition()
        End Sub

        Public Overridable Sub OnSavePosition()
        End Sub

        Public Property Position As WindowPositionData
            Get
                Return _position
            End Get
            Set(value As WindowPositionData)
                _position = value
            End Set
        End Property

        Private Function makeWindow() As Window
            Return New Window() With {
                .WindowStyle = WindowStyle.None,
                .AllowsTransparency = True,
                .Background = Brushes.Transparent,
                .ShowInTaskbar = False,
                .Topmost = True,
                .Tag = _windowGuid,
                .IsHitTestVisible = False
            }
        End Function

        Private Async Sub doRestoreAnimPt1(w As Window, wasMaximized As Boolean, callback As Action)
            w.Left = _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0)
            w.Top = _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0)
            w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_dpi.PixelsPerInchX / 96.0)
            w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_dpi.PixelsPerInchY / 96.0)
            w.Margin = New Thickness(
                (_s.WorkingArea.Left - _s.Bounds.Left) / (_dpi.PixelsPerInchX / 96.0),
                (_s.WorkingArea.Top - _s.Bounds.Top) / (_dpi.PixelsPerInchY / 96.0),
                (_s.Bounds.Right - _s.WorkingArea.Right) / (_dpi.PixelsPerInchX / 96.0),
                (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0))
            w.Content = New Image() With {
                .Source = _minimizeImage,
                .Width = 200,
                .Height = Me.ActualHeight,
                .Margin = New Thickness(If(Me.ShowInTaskbar, w.Width / 2 - 100, 0), _s.WorkingArea.Bottom / (_dpi.PixelsPerInchY / 96.0), 0, 0),
                .VerticalAlignment = VerticalAlignment.Top,
                .HorizontalAlignment = Windows.HorizontalAlignment.Left
            }

            w.Show()

            Dim ease As SineEase = New SineEase()
            ease.EasingMode = EasingMode.EaseInOut
            Dim ta As ThicknessAnimation =
                    New ThicknessAnimation(
                        CType(w.Content, Image).Margin,
                        New Thickness(If(wasMaximized, w.Left, Me.Left) - _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0),
                                      If(wasMaximized, w.Top, Me.Top) - _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0), 0, 0),
                        New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da As DoubleAnimation = New DoubleAnimation(200, If(wasMaximized, w.ActualWidth, Me.Width), New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da2 As DoubleAnimation = New DoubleAnimation(0, 1, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            ta.EasingFunction = ease
            da.EasingFunction = ease
            da2.EasingFunction = ease

            CType(w.Content, Image).BeginAnimation(Image.MarginProperty, ta)
            CType(w.Content, Image).BeginAnimation(Image.WidthProperty, da)
            CType(w.Content, Image).BeginAnimation(Image.OpacityProperty, da2)

            Me.ActualWindowState = WindowState.Normal
            Await Task.Delay(MINIMIZE_SPEED)
            _skipMinimize = False
            SystemCommands.RestoreWindow(Me)

            Await Task.Delay(100)

            w.Close()
            CType(w.Content, Image).Source = Nothing
            _minimizeImage = Nothing
            w = Nothing
            Me.ActualWindowState = Me.WindowState
            GC.Collect()

            If Not callback Is Nothing Then
                callback.Invoke()
            End If
        End Sub

        Private Sub doMinimizeAnimPt1(w As Window, isMaximized As Boolean)
            _minimizeImage = New RenderTargetBitmap(Me.ActualWidth, Me.ActualHeight, 96, 96, PixelFormats.Pbgra32)
            _minimizeImage.Render(Me)

            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            _s = Forms.Screen.FromHandle(hWnd)
            _dpi = VisualTreeHelper.GetDpi(Me)

            w.Left = _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0)
            w.Top = _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0)
            w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_dpi.PixelsPerInchX / 96.0)
            w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_dpi.PixelsPerInchY / 96.0)
            w.Margin = New Thickness(
                (_s.WorkingArea.Left - _s.Bounds.Left) / (_dpi.PixelsPerInchX / 96.0),
                (_s.WorkingArea.Top - _s.Bounds.Top) / (_dpi.PixelsPerInchY / 96.0),
                (_s.Bounds.Right - _s.WorkingArea.Right) / (_dpi.PixelsPerInchX / 96.0),
                (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0))
            w.Content = New Image() With {
                .Source = _minimizeImage,
                .Width = Me.ActualWidth,
                .Height = Me.ActualHeight,
                .Margin = New Thickness(If(isMaximized, w.Left, Me.Left) - _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0),
                                        If(isMaximized, w.Top, Me.Top) - _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0), 0, 0),
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
                        New Thickness(If(Me.ShowInTaskbar, w.Width / 2 - 100, 0), _s.WorkingArea.Bottom / (_dpi.PixelsPerInchY / 96.0), 0, 0),
                        New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da As DoubleAnimation = New DoubleAnimation(Me.ActualWidth, 200, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            Dim da2 As DoubleAnimation = New DoubleAnimation(1, 0, New Duration(TimeSpan.FromMilliseconds(MINIMIZE_SPEED)))
            ta.EasingFunction = ease
            da.EasingFunction = ease
            da2.EasingFunction = ease

            CType(w.Content, Image).BeginAnimation(Image.MarginProperty, ta)
            CType(w.Content, Image).BeginAnimation(Image.WidthProperty, da)
            CType(w.Content, Image).BeginAnimation(Image.OpacityProperty, da2)

            Await Task.Delay(MINIMIZE_SPEED)

            Me.ActualWindowState = WindowState.Minimized
            w.Close()
        End Sub

        Private Sub doMaximizeAnimPt1(w As Window)
            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            _s = Forms.Screen.FromHandle(hWnd)
            _dpi = VisualTreeHelper.GetDpi(Me)

            _maximizeImage = New RenderTargetBitmap(Me.ActualWidth * _dpi.DpiScaleX, Me.ActualHeight * _dpi.DpiScaleY, _dpi.PixelsPerInchX, _dpi.PixelsPerInchY, PixelFormats.Pbgra32)
            _maximizeImage.Render(Me)

            w.Left = _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0)
            w.Top = _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0)
            w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_dpi.PixelsPerInchX / 96.0)
            w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_dpi.PixelsPerInchY / 96.0)
            w.Margin = New Thickness(
                (_s.WorkingArea.Left - _s.Bounds.Left) / (_dpi.PixelsPerInchX / 96.0),
                (_s.WorkingArea.Top - _s.Bounds.Top) / (_dpi.PixelsPerInchY / 96.0),
                (_s.Bounds.Right - _s.WorkingArea.Right) / (_dpi.PixelsPerInchX / 96.0),
                (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0))
            w.Content = New Image() With {
                .Source = _maximizeImage,
                .Width = _maximizeImage.PixelWidth / _dpi.DpiScaleX,
                .Height = _maximizeImage.PixelHeight / _dpi.DpiScaleY,
                .Margin = New Thickness(Me.Left, Me.Top, 0, 0),
                .VerticalAlignment = VerticalAlignment.Top,
                .HorizontalAlignment = Windows.HorizontalAlignment.Left
            }

            w.Show()

            Me.Opacity = 0
        End Sub

        Private Async Sub doMaximizeAnimPt2(w As Window, left As Double, top As Double, width As Double, height As Double)
            _isAnimating = True
            Await Task.Delay(50)

            Me.Maximize()

            Me.PART_RootBorder.Padding = New Thickness(
                left + Me.PART_RootBorder.Padding.Left,
                top + Me.PART_RootBorder.Padding.Top,
                (_s.WorkingArea.Right) / (_dpi.PixelsPerInchY / 96.0) - (Me.Left + Me.Width) + Me.PART_RootBorder.Padding.Right,
                (_s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0) - (Me.Top + Me.Height) + Me.PART_RootBorder.Padding.Bottom)
            Me.Opacity = 1

            Await Task.Delay(50)

            w.Close()
            CType(w.Content, Image).Source = Nothing
            _maximizeImage = Nothing
            w = Nothing
            GC.Collect()

            _noPositionCorrection = True

            Dim ease As SineEase = New SineEase()
            ease.EasingMode = EasingMode.EaseInOut
            Dim ta As ThicknessAnimation = New ThicknessAnimation(Me.PART_RootBorder.Padding, New Thickness(), New Duration(TimeSpan.FromMilliseconds(MAXIMIZE_SPEED)))
            ta.EasingFunction = ease
            AddHandler ta.Completed,
                Sub(s, e)
                    If _isAnimating Then
                        _isAnimating = False
                        Me.PART_RootBorder.BeginAnimation(Border.PaddingProperty, Nothing)
                        Me.PART_RootBorder.Padding = New Thickness()
                        Me.ActualWindowState = WindowState.Maximized
                        _noPositionCorrection = False
                    End If
                End Sub
            Me.PART_RootBorder.BeginAnimation(Border.PaddingProperty, ta)
        End Sub


        Private Async Sub doRestoreAnimPt2()
            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            _s = Forms.Screen.FromHandle(hWnd)
            _dpi = VisualTreeHelper.GetDpi(Me)

            _isAnimating = True

            Dim targetPadding As Thickness = New Thickness(
                If(Me.GlowStyle = GlowStyle.Glowing, Me.GlowSize, 0) + _position.Left,
                If(Me.GlowStyle = GlowStyle.Glowing, Me.GlowSize, 0) + _position.Top,
                Me.GlowSize + (_s.WorkingArea.Right) / (_dpi.PixelsPerInchY / 96.0) - (_position.Left + _position.Width),
                Me.GlowSize + (_s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0) - (_position.Top + _position.Height))
            Dim ease As SineEase = New SineEase()
            ease.EasingMode = EasingMode.EaseInOut
            Dim ta As ThicknessAnimation = New ThicknessAnimation(Me.PART_RootBorder.Padding, targetPadding, New Duration(TimeSpan.FromMilliseconds(MAXIMIZE_SPEED)))
            ta.EasingFunction = ease
            AddHandler ta.Completed,
                Async Sub(s, e)
                    If _isAnimating Then
                        Await Task.Delay(10)

                        Application.Current.Dispatcher.Invoke(
                            Sub()
                            End Sub, Threading.DispatcherPriority.ContextIdle)

                        _maximizeImage = New RenderTargetBitmap(Me.ActualWidth * _dpi.DpiScaleX, Me.ActualHeight * _dpi.DpiScaleY, _dpi.PixelsPerInchX, _dpi.PixelsPerInchY, PixelFormats.Pbgra32)
                        _maximizeImage.Render(Me)

                        Dim effect As Effect = Me.PART_MainBorder.Effect
                        Me.PART_MainBorder.Effect = Nothing

                        Dim w As Window = makeWindow()

                        w.Left = _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0)
                        w.Top = _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0)
                        w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_dpi.PixelsPerInchX / 96.0)
                        w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_dpi.PixelsPerInchY / 96.0)
                        w.Margin = New Thickness(
                            (_s.WorkingArea.Left - _s.Bounds.Left) / (_dpi.PixelsPerInchX / 96.0),
                            (_s.WorkingArea.Top - _s.Bounds.Top) / (_dpi.PixelsPerInchY / 96.0),
                            (_s.Bounds.Right - _s.WorkingArea.Right) / (_dpi.PixelsPerInchX / 96.0),
                            (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0))
                        w.Content = New Image() With {
                            .Source = _maximizeImage,
                            .Width = _maximizeImage.PixelWidth / _dpi.DpiScaleX,
                            .Height = _maximizeImage.PixelHeight / _dpi.DpiScaleY,
                            .Margin = New Thickness(0, 0, 0, 0),
                            .VerticalAlignment = VerticalAlignment.Top,
                            .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                            .UseLayoutRounding = True,
                            .SnapsToDevicePixels = True
                        }
                        w.WindowState = WindowState.Maximized

                        w.Show()
                        Me.Opacity = 0

                        Application.Current.Dispatcher.Invoke(
                            Sub()
                            End Sub, Threading.DispatcherPriority.ContextIdle)

                        Me.Restore()

                        Me.PART_RootBorder.BeginAnimation(Border.PaddingProperty, Nothing)
                        Me.PART_RootBorder.Padding = New Thickness(
                            If(Me.GlowStyle = GlowStyle.Glowing, Me.GlowSize, 0),
                            If(Me.GlowStyle = GlowStyle.Glowing, Me.GlowSize, 0),
                            Me.GlowSize,
                            Me.GlowSize)

                        Me.Opacity = 1
                        _isAnimating = False
                        Me.PART_MainBorder.Effect = effect

                        Application.Current.Dispatcher.Invoke(
                            Sub()
                            End Sub, Threading.DispatcherPriority.ContextIdle)

                        w.Close()
                        CType(w.Content, Image).Source = Nothing
                        _maximizeImage = Nothing
                        w = Nothing
                        GC.Collect()
                    End If
                End Sub
            Me.PART_RootBorder.BeginAnimation(Border.PaddingProperty, ta)

            Me.ActualWindowState = WindowState.Normal
        End Sub

        Private Async Sub doCloseAnimation()
            Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
            _s = Forms.Screen.FromHandle(hWnd)
            _dpi = VisualTreeHelper.GetDpi(Me)

            Dim w As Window = makeWindow()

            _closeImage = New RenderTargetBitmap(Me.ActualWidth * _dpi.DpiScaleX, Me.ActualHeight * _dpi.DpiScaleY, _dpi.PixelsPerInchX, _dpi.PixelsPerInchY, PixelFormats.Pbgra32)
            _closeImage.Render(Me)

            w.Left = _s.WorkingArea.Left / (_dpi.PixelsPerInchX / 96.0)
            w.Top = _s.WorkingArea.Top / (_dpi.PixelsPerInchY / 96.0)
            w.Width = (_s.WorkingArea.Right - _s.WorkingArea.Left) / (_dpi.PixelsPerInchX / 96.0)
            w.Height = (_s.WorkingArea.Bottom - _s.WorkingArea.Top) / (_dpi.PixelsPerInchY / 96.0)
            w.Margin = New Thickness(
                (_s.WorkingArea.Left - _s.Bounds.Left) / (_dpi.PixelsPerInchX / 96.0),
                (_s.WorkingArea.Top - _s.Bounds.Top) / (_dpi.PixelsPerInchY / 96.0),
                (_s.Bounds.Right - _s.WorkingArea.Right) / (_dpi.PixelsPerInchX / 96.0),
                (_s.Bounds.Bottom - _s.WorkingArea.Bottom) / (_dpi.PixelsPerInchY / 96.0))
            w.Content = New Image() With {
                .Source = _closeImage,
                .Width = _closeImage.PixelWidth / _dpi.DpiScaleX,
                .Height = _closeImage.PixelHeight / _dpi.DpiScaleY,
                .Margin = If(Me.WindowState = WindowState.Maximized, New Thickness(), New Thickness(Me.Left, Me.Top, 0, 0)),
                .VerticalAlignment = VerticalAlignment.Top,
                .HorizontalAlignment = Windows.HorizontalAlignment.Left,
                .Stretch = Stretch.Fill
            }
            w.WindowState = Me.WindowState

            w.Show()

            Await Task.Delay(50)

            Me.Opacity = 0

            Dim ease As SineEase = New SineEase()
            ease.EasingMode = EasingMode.EaseInOut
            Dim da As DoubleAnimation = New DoubleAnimation(w.Opacity, 0, New Duration(TimeSpan.FromMilliseconds(CLOSE_SPEED)))
            Dim ta As ThicknessAnimation = New ThicknessAnimation(CType(w.Content, Image).Margin,
                        New Thickness(CType(w.Content, Image).Margin.Left + Me.ActualWidth * 0.1,
                                      CType(w.Content, Image).Margin.Top + Me.ActualHeight * 0.1,
                                      0, 0), New Duration(TimeSpan.FromMilliseconds(CLOSE_SPEED)))
            Dim da2 As DoubleAnimation = New DoubleAnimation(CType(w.Content, Image).Width, Math.Max(CType(w.Content, Image).Width - Me.ActualWidth * 0.2, 1), New Duration(TimeSpan.FromMilliseconds(CLOSE_SPEED)))
            Dim da3 As DoubleAnimation = New DoubleAnimation(CType(w.Content, Image).Height, Math.Max(CType(w.Content, Image).Height - Me.ActualHeight * 0.2, 1), New Duration(TimeSpan.FromMilliseconds(CLOSE_SPEED)))
            da.EasingFunction = ease
            ta.EasingFunction = ease
            da2.EasingFunction = ease
            da3.EasingFunction = ease
            AddHandler da.Completed,
                Sub(s2 As Object, e2 As EventArgs)
                    w.Close()
                    CType(w.Content, Image).Source = Nothing
                    _closeImage = Nothing
                    w = Nothing

                    _isReallyClosing = True
                    ' sometimes it throws, no good reason :(
                    Me.Close()
                End Sub
            w.BeginAnimation(Window.OpacityProperty, da)
            CType(w.Content, Image).BeginAnimation(Image.MarginProperty, ta)
            CType(w.Content, Image).BeginAnimation(Image.WidthProperty, da2)
            CType(w.Content, Image).BeginAnimation(Image.HeightProperty, da3)
        End Sub

        Private Function HwndHook(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
            Select Case msg
                Case WM_SYSCOMMAND
                    Select Case wParam
                        Case SC_MINIMIZE
                            If Not _isMinimized Then
                                _isMinimized = True
                                _skipMinimize = True
                                _wasMaximized = Me.WindowState = WindowState.Maximized

                                Dim w As Window = makeWindow()

                                doMinimizeAnimPt1(w, _wasMaximized)
                                Windows.Application.Current.Dispatcher.BeginInvoke(
                                    Sub()
                                        doMinimizeAnimPt2(w)
                                    End Sub)
                            End If
                        Case SC_RESTORE
                            If _isMinimized Then
                                _isMinimized = False

                                Dim w As Window = makeWindow()

                                If _skipMinimize Then
                                    handled = True
                                    doRestoreAnimPt1(w, _wasMaximized, Nothing)
                                End If
                            ElseIf Me.WindowState = WindowState.Maximized AndAlso _renderingTier = 2 Then
                                If Not _skipMaximize Then
                                    handled = True
                                    doRestoreAnimPt2()
                                    _skipMaximize = True
                                Else
                                    _skipMaximize = False
                                End If
                            End If
                        Case SC_MAXIMIZE
                            If _renderingTier = 2 Then
                                If _isMinimized Then
                                    _isMinimized = False

                                    Dim w As Window = makeWindow()

                                    handled = True
                                    doRestoreAnimPt1(w, _wasMaximized,
                                        Sub()
                                            If Not _wasMaximized Then
                                                w = makeWindow()
                                                doMaximizeAnimPt1(w)
                                                doMaximizeAnimPt2(w, Me.Left, Me.Top, Me.Width, Me.Height)
                                            End If
                                            _skipMaximize = True
                                            If _wasMaximized Then Me.Maximize()
                                        End Sub)
                                Else
                                    If Not _skipMaximize Then
                                        handled = True
                                        Dim w As Window = makeWindow()
                                        doMaximizeAnimPt1(w)
                                        doMaximizeAnimPt2(w, Me.Left, Me.Top, Me.Width, Me.Height)
                                        _skipMaximize = True
                                    Else
                                        _skipMaximize = False
                                    End If
                                End If
                            End If
                    End Select
                Case WM_NCLBUTTONDBLCLK
                    If Me.WindowState = WindowState.Maximized Then
                        handled = True
                        doRestoreAnimPt2()
                        _skipMaximize = True
                    Else
                        handled = True
                        Dim w As Window = makeWindow()
                        doMaximizeAnimPt1(w)
                        doMaximizeAnimPt2(w, Me.Left, Me.Top, Me.Width, Me.Height)
                        _skipMaximize = True
                    End If
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
            If Not _isReallyClosing Then MyBase.OnClosing(e)

            If Not e.Cancel Then
                If Not _isReallyClosing AndAlso Not Me.WindowState = WindowState.Minimized Then
                    e.Cancel = True
                    doCloseAnimation()
                Else
                    ' avoid main window disappearing after close
                    If Not Me.Owner Is Nothing Then
                        Dim deepOwner As Window = Me
                        While Not deepOwner.Owner Is Nothing
                            deepOwner = deepOwner.Owner
                        End While
                        If Not Me.Equals(deepOwner) Then
                            deepOwner.Activate()
                        End If
                    End If
                End If
            End If
        End Sub

        Protected Overrides Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)

            If Not Me.Owner Is Nothing Then
                Me.Owner.Activate()
            End If

            For Each w As Window In Application.Current.Windows
                If _windowGuid.Equals(w.Tag) Then
                    w.Close()
                End If
            Next
        End Sub

        Public Property GlowSize() As Double
            Get
                Return GetValue(GlowSizeProperty)
            End Get
            Set(ByVal value As Double)
                SetCurrentValue(GlowSizeProperty, value)
            End Set
        End Property

        Public Property GlowStyle() As Data.GlowStyle
            Get
                Return GetValue(GlowStyleProperty)
            End Get
            Set(ByVal value As Data.GlowStyle)
                SetCurrentValue(GlowStyleProperty, value)
            End Set
        End Property

        Public Property ActualWindowState As WindowState
            Get
                Return GetValue(ActualWindowStateProperty)
            End Get
            Set(ByVal value As WindowState)
                SetCurrentValue(ActualWindowStateProperty, value)
            End Set
        End Property
    End Class
End Namespace