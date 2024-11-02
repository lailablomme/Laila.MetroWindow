Imports System.Windows
Imports System.Windows.Media.Animation

Namespace Helpers
    Public Class GridLengthAnimation
        Inherits AnimationTimeline

        Private _isCompleted As Boolean

        ''' <summary>
        ''' Marks the animation as completed
        ''' </summary>
        Public Property IsCompleted As Boolean
            Get
                Return _isCompleted
            End Get
            Set(value As Boolean)
                _isCompleted = value
            End Set
        End Property

        ''' <summary>
        ''' Sets the reverse value for the second animation
        ''' </summary>
        Public Property ReverseValue As Double
            Get
                Return CDbl(GetValue(ReverseValueProperty))
            End Get
            Set(value As Double)
                SetValue(ReverseValueProperty, value)
            End Set
        End Property

        ''' <summary>
        ''' Dependency property. Sets the reverse value for the second animation
        ''' </summary>
        Public Shared ReadOnly ReverseValueProperty As DependencyProperty =
            DependencyProperty.Register("ReverseValue", GetType(Double), GetType(GridLengthAnimation), New UIPropertyMetadata(0.0))

        ''' <summary>
        ''' Returns the type of object to animate
        ''' </summary>
        Public Overrides ReadOnly Property TargetPropertyType As Type
            Get
                Return GetType(GridLength)
            End Get
        End Property

        ''' <summary>
        ''' Creates an instance of the animation object
        ''' </summary>
        ''' <returns>Returns the instance of the GridLengthAnimation</returns>
        Protected Overrides Function CreateInstanceCore() As Freezable
            Return New GridLengthAnimation()
        End Function

        ''' <summary>
        ''' Dependency property for the From property
        ''' </summary>
        Public Shared ReadOnly FromProperty As DependencyProperty =
            DependencyProperty.Register("From", GetType(GridLength), GetType(GridLengthAnimation))

        ''' <summary>
        ''' CLR Wrapper for the From dependency property
        ''' </summary>
        Public Property From As GridLength
            Get
                Return CType(GetValue(FromProperty), GridLength)
            End Get
            Set(value As GridLength)
                SetValue(FromProperty, value)
            End Set
        End Property

        ''' <summary>
        ''' Dependency property for the To property
        ''' </summary>
        Public Shared ReadOnly ToProperty As DependencyProperty =
            DependencyProperty.Register("To", GetType(GridLength), GetType(GridLengthAnimation))

        ''' <summary>
        ''' CLR Wrapper for the To property
        ''' </summary>
        Public Property [To] As GridLength
            Get
                Return CType(GetValue(ToProperty), GridLength)
            End Get
            Set(value As GridLength)
                SetValue(ToProperty, value)
            End Set
        End Property

        Private clock As AnimationClock

        ''' <summary>
        ''' Registers to the completed event of the animation clock
        ''' </summary>
        ''' <param name="clock">The animation clock to notify completion status</param>
        Private Sub VerifyAnimationCompletedStatus(clock As AnimationClock)
            If Me.clock Is Nothing Then
                Me.clock = clock
                AddHandler Me.clock.Completed, Sub(sender, e) isCompleted = True
            End If
        End Sub

        ''' <summary>
        ''' Animates the GridLength property
        ''' </summary>
        ''' <param name="defaultOriginValue">The original value to animate</param>
        ''' <param name="defaultDestinationValue">The final value</param>
        ''' <param name="animationClock">The animation clock (timer)</param>
        ''' <returns>Returns the new GridLength to set</returns>
        Public Overrides Function GetCurrentValue(defaultOriginValue As Object,
                                                 defaultDestinationValue As Object,
                                                 animationClock As AnimationClock) As Object
            ' Check the animation clock event
            VerifyAnimationCompletedStatus(animationClock)

            ' Check if the animation was completed
            If isCompleted Then
                Return CType(defaultDestinationValue, GridLength)
            End If

            Dim fromVal As Double = Me.From.Value
            Dim toVal As Double = Me.To.Value

            ' Check if the value is already collapsed
            If CType(defaultOriginValue, GridLength).Value = toVal Then
                fromVal = toVal
                toVal = Me.ReverseValue
            ElseIf animationClock.CurrentProgress.Value = 1.0 Then
                Return [To]
            End If

            If fromVal > toVal Then
                Return New GridLength((1 - animationClock.CurrentProgress.Value) * (fromVal - toVal) + toVal,
                                      If(Me.From.IsStar, GridUnitType.Star, GridUnitType.Pixel))
            Else
                Return New GridLength(animationClock.CurrentProgress.Value * (toVal - fromVal) + fromVal,
                                      If(Me.From.IsStar, GridUnitType.Star, GridUnitType.Pixel))
            End If
        End Function
    End Class
End Namespace