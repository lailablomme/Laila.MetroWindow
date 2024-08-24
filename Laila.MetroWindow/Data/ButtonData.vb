Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Windows.Input
Imports System.Windows.Media
Imports Laila.MetroWindow.Helpers

Namespace Data
    Public Class ButtonData
        Implements INotifyPropertyChanged

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private _isVisible As Boolean = True
        Private _isEnabled As Boolean = True
        Private _imageSource As String = Nothing
        Private _image As Byte() = Nothing
        Private _toolTip As String
        Private _action As System.Func(Of Task)
        Private _actionCommand As ICommand = Nothing

        Public Property IsVisible As Boolean
            Get
                Return _isVisible
            End Get
            Set(value As Boolean)
                SetValue(_isVisible, value)
            End Set
        End Property

        Public Property IsEnabled As Boolean
            Get
                Return _isEnabled
            End Get
            Set(value As Boolean)
                SetValue(_isEnabled, value)
            End Set
        End Property

        Public Property ImageSource As String
            Get
                Return _imageSource
            End Get
            Set(value As String)
                SetValue(_imageSource, value)
                NotifyOfPropertyChange("InternalImage")
            End Set
        End Property

        Public Property Image As Byte()
            Get
                Return _image
            End Get
            Set(value As Byte())
                SetValue(_image, value)
                NotifyOfPropertyChange("InternalImage")
            End Set
        End Property

        Public ReadOnly Property InternalImage As ImageSource
            Get
                If Not _image Is Nothing Then
                    Return ImageHelper.GetBestSize(_image, 16)
                ElseIf Not String.IsNullOrWhiteSpace(_imageSource) Then
                    Return ImageHelper.GetBestSize(_imageSource, 16)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Public Property ToolTip As String
            Get
                Return _toolTip
            End Get
            Set(value As String)
                SetValue(_toolTip, value)
            End Set
        End Property

        Public Property Action As System.Func(Of Task)
            Get
                Return _action
            End Get
            Set(value As System.Func(Of Task))
                SetValue(_action, value)
            End Set
        End Property

        Public ReadOnly Property ActionCommand As ICommand
            Get
                If _actionCommand Is Nothing Then
                    _actionCommand =
                        New RelayCommand(
                            Async Function(param) As Task
                                Await DoAction()
                            End Function,
                            Function(param)
                                Return True
                            End Function)
                End If
                Return _actionCommand
            End Get
        End Property

        Private Async Function DoAction() As Task
            Await _action.Invoke()
        End Function

        Protected Sub SetValue(Of T)(ByRef member As T, value As T, <CallerMemberName> Optional propertyName As String = "")
            member = value
            NotifyOfPropertyChange(propertyName)
        End Sub

        Protected Sub NotifyOfPropertyChange(propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class
End Namespace