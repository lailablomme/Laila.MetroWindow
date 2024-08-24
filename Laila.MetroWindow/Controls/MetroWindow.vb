Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Interop
Imports System.Windows.Markup
Imports System.Windows.Media
Imports Laila.MetroWindow.Data
Imports Laila.MetroWindow.Helpers

Namespace Controls
    Public Class MetroWindow
        Inherits CustomChromeWindow

        Public Shared ReadOnly GlowColorProperty As DependencyProperty = DependencyProperty.Register("GlowColor", GetType(Media.Color), GetType(MetroWindow), New FrameworkPropertyMetadata(Media.Colors.Red, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly GlowStyleProperty As DependencyProperty = DependencyProperty.Register("GlowStyle", GetType(GlowStyle), GetType(MetroWindow), New FrameworkPropertyMetadata(Data.GlowStyle.Glowing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly GlowSizeProperty As DependencyProperty = DependencyProperty.Register("GlowSize", GetType(Double), GetType(MetroWindow), New FrameworkPropertyMetadata(Convert.ToDouble(15), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly CaptionHeightProperty As DependencyProperty = DependencyProperty.Register("CaptionHeight", GetType(Double), GetType(MetroWindow), New FrameworkPropertyMetadata(Convert.ToDouble(31), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly DoIntegrateMenuProperty As DependencyProperty = DependencyProperty.Register("DoIntegrateMenu", GetType(Boolean), GetType(MetroWindow), New FrameworkPropertyMetadata(True, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly LeftButtonsProperty As DependencyProperty = DependencyProperty.Register("LeftButtons", GetType(ObservableCollection(Of ButtonData)), GetType(MetroWindow), New FrameworkPropertyMetadata(Nothing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))
        Public Shared ReadOnly RightButtonsProperty As DependencyProperty = DependencyProperty.Register("RightButtons", GetType(ObservableCollection(Of ButtonData)), GetType(MetroWindow), New FrameworkPropertyMetadata(Nothing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault))

        Public Property GlowColor() As Media.Color
            Get
                Return GetValue(GlowColorProperty)
            End Get
            Set(ByVal value As Media.Color)
                SetValue(GlowColorProperty, value)
            End Set
        End Property

        Public Property GlowStyle() As Data.GlowStyle
            Get
                Return GetValue(GlowStyleProperty)
            End Get
            Set(ByVal value As Data.GlowStyle)
                SetValue(GlowStyleProperty, value)
            End Set
        End Property

        Public Property DoIntegrateMenu() As Boolean
            Get
                Return GetValue(DoIntegrateMenuProperty)
            End Get
            Set(ByVal value As Boolean)
                SetValue(DoIntegrateMenuProperty, value)
            End Set
        End Property

        Public Property GlowSize() As Double
            Get
                Return GetValue(GlowSizeProperty)
            End Get
            Set(ByVal value As Double)
                SetValue(GlowSizeProperty, value)
            End Set
        End Property

        Public Property CaptionHeight() As Double
            Get
                Return GetValue(CaptionHeightProperty)
            End Get
            Set(ByVal value As Double)
                SetValue(CaptionHeightProperty, value)
            End Set
        End Property

        Public Property LeftButtons() As ObservableCollection(Of ButtonData)
            Get
                Return GetValue(LeftButtonsProperty)
            End Get
            Set(ByVal value As ObservableCollection(Of ButtonData))
                SetValue(LeftButtonsProperty, value)
            End Set
        End Property

        Public Property RightButtons() As ObservableCollection(Of ButtonData)
            Get
                Return GetValue(RightButtonsProperty)
            End Get
            Set(ByVal value As ObservableCollection(Of ButtonData))
                SetValue(RightButtonsProperty, value)
            End Set
        End Property

        Private _originalSizeToContent As SizeToContent
        Private _titleBar As ContentControl
        Private _rootGrid As Grid
        Private _menuPlaceHolder As Grid
        Private _textBlock As TextBlock
        Private _iconButton As Button
        Private _minimizeButton As Button
        Private _maximizeRestoreButton As Button
        Private _closeButton As Button
        Private _isMenuIntegrated As Boolean = False
        Private _position As WindowPositionData = Nothing
        Private _previousPosition As WindowPositionData = New WindowPositionData()
        Private _noPositionCorrection As Boolean = False
        Private _dontUpdatePosition As Boolean = True

        Shared Sub New()
            DefaultStyleKeyProperty.OverrideMetadata(GetType(MetroWindow), New FrameworkPropertyMetadata(GetType(MetroWindow)))
        End Sub

        Public Sub New()
            Me.DefaultStyleKey = GetType(MetroWindow)
            Me.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)

            AddHandler Me.LayoutUpdated, AddressOf OnLayoutUpdated

            AddHandler Me.SizeChanged,
                Sub(sender2 As Object, e2 As EventArgs)
                    ' save size
                    If Me.WindowState = WindowState.Normal AndAlso Not _dontUpdatePosition Then
                        _previousPosition = _position.Clone()
                        _position.Width = Me.Width
                        _position.Height = Me.Height
                    End If

                    If Not _noPositionCorrection _
                        AndAlso Not (Me.ResizeMode = ResizeMode.CanResize OrElse Me.ResizeMode = ResizeMode.CanResizeWithGrip) Then
                        ' keep window within this screen
                        Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
                        Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromHwnd(hWnd)
                        Dim s As Forms.Screen = Forms.Screen.FromHandle(hWnd)
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(
                            Sub()
                                If Me.Top + Me.Height > s.WorkingArea.Bottom / (g.DpiY / 96.0) Then
                                    Me.Top = s.WorkingArea.Bottom / (g.DpiY / 96.0) - Me.Height
                                End If
                                If Me.Left + Me.Width > s.WorkingArea.Right / (g.DpiY / 96.0) Then
                                    Me.Left = s.WorkingArea.Right / (g.DpiY / 96.0) - Me.Width
                                End If
                            End Sub)
                    End If
                    _noPositionCorrection = False

                    Me.CenterTitle()
                End Sub

            AddHandler Me.Closed,
                Sub(sender As Object, e As EventArgs)
                    Me.OnSavePosition()
                End Sub
        End Sub

        Private Sub OnLayoutUpdated(sender As Object, e As EventArgs)
            If Not _isMenuIntegrated AndAlso Me.DoIntegrateMenu AndAlso Not _menuPlaceHolder Is Nothing Then
                Dim children As IEnumerable(Of Control) = FindVisualChildren(Of Control)(Me)
                For Each child In children
                    RemoveHandler child.LayoutUpdated, AddressOf OnLayoutUpdated
                    AddHandler child.LayoutUpdated, AddressOf OnLayoutUpdated
                Next

                Dim menus As IEnumerable(Of Menu) = FindVisualChildren(Of Menu)(Me)
                If menus.Count > 0 Then
                    _isMenuIntegrated = True

                    ' unhook
                    children = FindVisualChildren(Of Control)(Me)
                    For Each child In children
                        RemoveHandler child.LayoutUpdated, AddressOf OnLayoutUpdated
                    Next

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
                    _menuPlaceHolder.Children.Add(m)
                    m.Margin = New Thickness(0, 0, 5, 0)

                    AddHandler m.SizeChanged,
                        Sub()
                            Me.CenterTitle()
                        End Sub

                    Application.Current.Dispatcher.Invoke(
                        Sub()
                            Me.CenterTitle()
                        End Sub, Threading.DispatcherPriority.Loaded)
                End If
            End If
        End Sub

        Private Sub CenterTitle()
            If Me.DoIntegrateMenu AndAlso Not _menuPlaceHolder Is Nothing AndAlso Not _textBlock Is Nothing Then
                Dim p As Point = _menuPlaceHolder.TransformToAncestor(Me).Transform(New Point(0, 0))
                Dim leftCentered As Double = Me.ActualWidth / 2 - _textBlock.ActualWidth / 2
                Dim diff As Double = leftCentered - p.X - _menuPlaceHolder.ActualWidth
                If diff < 0 Then diff = 0
                _textBlock.Margin = New Thickness(diff, 0, 0, 0)
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

            Me.SetChromeWindow()

            ' window startup location
            Dim newLeft As Double, newTop As Double, s As Forms.Screen, hWnd As IntPtr, g As System.Drawing.Graphics
            If Me.WindowStartupLocation = WindowStartupLocation.CenterOwner AndAlso Not Me.Owner Is Nothing AndAlso Me.Owner.WindowState = WindowState.Normal Then
                ' center owner
                hWnd = New WindowInteropHelper(Me.Owner).Handle
                s = Forms.Screen.FromHandle(hWnd)

                newLeft = Me.Owner.Left + Me.Owner.Width / 2 - Me.Width / 2
                newTop = Me.Owner.Top + Me.Owner.Height / 2 - Me.Height / 2
            ElseIf Me.WindowStartupLocation <> WindowStartupLocation.Manual Then
                ' center screen
                hWnd = New WindowInteropHelper(Me).Handle
                s = Forms.Screen.FromHandle(hWnd)
                g = System.Drawing.Graphics.FromHwnd(hWnd)

                newLeft = (s.WorkingArea.Left + s.WorkingArea.Width / 2) / (g.DpiX / 96.0) - Me.Width / 2
                newTop = (s.WorkingArea.Top + s.WorkingArea.Height / 2) / (g.DpiY / 96.0) - Me.Height / 2
            Else
                ' manual
                newLeft = Me.Left
                newTop = Me.Top
            End If

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
                End If
            End If

            ' start recording position
            _dontUpdatePosition = False

            ' help size to content a hand
            _originalSizeToContent = Me.SizeToContent
            If Me.SizeToContent <> SizeToContent.Manual Then
                _rootGrid.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
                Dim size As Size = _rootGrid.DesiredSize
                Me.SizeToContent = SizeToContent.Manual
                If _originalSizeToContent = SizeToContent.WidthAndHeight OrElse _originalSizeToContent = SizeToContent.Width Then
                    Me.Width = size.Width
                End If
                If _originalSizeToContent = SizeToContent.WidthAndHeight OrElse _originalSizeToContent = SizeToContent.Height Then
                    Me.Height = size.Height
                End If

                Me.SizeToContent = _originalSizeToContent
            End If

            Me.CenterTitle()

            ' if this window is in exact the same position as another window in our application, cascade it
            Dim i As Integer = 1
            Dim hasBeenTheEnd As Boolean = False
            hWnd = New WindowInteropHelper(Me).Handle
            s = Forms.Screen.FromHandle(hWnd)
            g = System.Drawing.Graphics.FromHwnd(hWnd)
            For x = 0 To System.Windows.Application.Current.Windows.Count - 1
                Dim window As Window = System.Windows.Application.Current.Windows(x)
                If Not window.Equals(Me) AndAlso window.Left = Me.Left AndAlso window.Top = Me.Top AndAlso window.Width = Me.Width AndAlso window.Height = Me.Height Then
                    Me.Left += 30
                    Me.Top += 30
                    x = 0

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
                End If
            Next

            ' save pos in case this is the first time
            _position.State = Me.WindowState
            _position.Left = Me.Left
            _position.Top = Me.Top
            _position.Width = Me.Width
            _position.Height = Me.Height
            _previousPosition = _position.Clone()
        End Sub

        Protected Sub SetChromeWindow()
            SetValue(System.Windows.Shell.WindowChrome.WindowChromeProperty,
                New System.Windows.Shell.WindowChrome() With {
                     .CaptionHeight = Me.CaptionHeight,
                     .ResizeBorderThickness = New Thickness(10),
                     .CornerRadius = New CornerRadius(0),
                     .GlassFrameThickness = New Thickness(0)
                })
        End Sub

        Public Overrides Sub OnApplyTemplate()
            MyBase.OnApplyTemplate()

            _menuPlaceHolder = Me.Template.FindName("PART_MenuPlaceHolder", Me)
            _rootGrid = Me.Template.FindName("PART_RootGrid", Me)
            _titleBar = Me.Template.FindName("PART_TitleBar", Me)
            _textBlock = Me.Template.FindName("PART_Text", Me)
            _iconButton = Me.Template.FindName("PART_IconButton", Me)
            _minimizeButton = Me.Template.FindName("PART_MinimizeButton", Me)
            _maximizeRestoreButton = Me.Template.FindName("PART_MaximizeRestoreButton", Me)
            _closeButton = Me.Template.FindName("PART_CloseButton", Me)

            AddHandler _textBlock.SizeChanged,
                Sub()
                    Me.CenterTitle()
                End Sub

            If Not _titleBar Is Nothing Then
                _titleBar.Focus()
                Keyboard.ClearFocus()
                _titleBar.Focusable = False
            End If

            If Not _iconButton Is Nothing Then
                AddHandler _iconButton.Click,
                    Sub()
                        Dim hWnd As IntPtr = New WindowInteropHelper(Me).Handle
                        Dim s As System.Windows.Forms.Screen = Forms.Screen.FromHandle(hWnd)
                        Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromHwnd(hWnd)
                        Dim p As Point = _iconButton.PointToScreen(New Point(0, _iconButton.ActualHeight))
                        p.X = p.X / (g.DpiX / 96.0)
                        p.Y = p.Y / (g.DpiY / 96.0)
                        Me.ShowSystemMenu(p)
                    End Sub
                AddHandler _iconButton.MouseUp, AddressOf onShowSystemMenu
                AddHandler _iconButton.MouseDoubleClick,
                    Sub()
                        Me.Close()
                    End Sub
            End If
            If Not _minimizeButton Is Nothing Then
                AddHandler _minimizeButton.Click,
                    Sub()
                        Me.Minimize()
                    End Sub
                AddHandler _minimizeButton.MouseUp, AddressOf onShowSystemMenu
            End If
            If Not _maximizeRestoreButton Is Nothing Then
                AddHandler _maximizeRestoreButton.Click,
                    Sub()
                        If Me.WindowState = WindowState.Maximized Then
                            Me.Restore()
                            _maximizeRestoreButton.Content = "1"
                        Else
                            Me.Maximize()
                            _maximizeRestoreButton.Content = "2"
                        End If
                    End Sub
                AddHandler _maximizeRestoreButton.MouseUp, AddressOf onShowSystemMenu
            End If
            If Not _closeButton Is Nothing Then
                AddHandler _closeButton.Click,
                     Sub()
                         Me.Close()
                     End Sub
                AddHandler _closeButton.MouseUp, AddressOf onShowSystemMenu
            End If
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

            If Me.WindowState = WindowState.Normal AndAlso Not _dontUpdatePosition Then
                _previousPosition = _position.Clone()
                _position.Left = Me.Left
                _position.Top = Me.Top
            End If
        End Sub

        Protected Overrides Sub OnStateChanged(e As EventArgs)
            ' szve state
            If Not _dontUpdatePosition Then
                If Me.WindowState <> WindowState.Normal Then
                    _position = _previousPosition.Clone()
                End If
                _position.State = Me.WindowState
                _noPositionCorrection = True
            End If

            If Me.WindowState = WindowState.Maximized Then
                ' set maximized margins to not get under the taskbar
                Dim margin As Thickness = New Thickness()
                If System.Environment.OSVersion.Version.Major >= 6 Then
                    margin = New Thickness(7)
                Else
                    margin = New Thickness(4)
                End If

                Dim currentScreen As System.Windows.Forms.Screen = System.Windows.Forms.Screen.FromHandle(New WindowInteropHelper(Me).Handle)
                Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromHwndInternal(New WindowInteropHelper(Me).Handle)
                margin = New Thickness(
                     (currentScreen.WorkingArea.Left - currentScreen.Bounds.Left) / (g.DpiY / 96.0) + margin.Left,
                     (currentScreen.WorkingArea.Top - currentScreen.Bounds.Top) / (g.DpiY / 96.0) + margin.Top,
                     (currentScreen.Bounds.Right - currentScreen.WorkingArea.Right) / (g.DpiY / 96.0) + margin.Right,
                     (currentScreen.Bounds.Bottom - currentScreen.WorkingArea.Bottom) / (g.DpiY / 96.0) + margin.Bottom)

                If Not _rootGrid Is Nothing Then
                    _rootGrid.Margin = margin
                End If
            Else
                ' not maximized
                If Not _rootGrid Is Nothing Then
                    _rootGrid.Margin = New Thickness(0)
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
    End Class
End Namespace