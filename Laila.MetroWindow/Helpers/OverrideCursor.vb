Imports System.Windows.Input

Namespace Helpers
    Friend Class OverrideCursor
        Implements IDisposable

        Private Shared _stack As Stack(Of Cursor)

        Shared Sub New()
            _stack = New Stack(Of Cursor)()
        End Sub

        Public Sub New()
            Me.New(Cursors.Wait)
        End Sub

        Public Sub New(cursor As Cursor)
            _stack.Push(cursor)
            UIHelper.OnUIThread(
                Sub()
                    Mouse.OverrideCursor = cursor
                End Sub)
        End Sub

        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not Me.disposedValue Then
                Me.disposedValue = True

                If disposing Then

                End If

                Dim previousCursor As Cursor = _stack.Pop()
                UIHelper.OnUIThread(
                    Sub()
                        If _stack.Count = 0 Then
                            Mouse.OverrideCursor = Nothing
                        Else
                            Mouse.OverrideCursor = _stack.Peek()
                        End If
                    End Sub)
            End If
        End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
    End Class
End Namespace