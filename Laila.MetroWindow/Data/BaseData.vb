Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Windows.Media

Namespace Data
    Public Class BaseData
        Implements ICloneable, IDeepCloneable

        Public Sub Copy(toObj As Object)
            ' get all public properties
            Dim properties As IEnumerable(Of PropertyInfo) = Me.GetType().GetProperties()

            ' for each property...
            For Each prop In properties
                ' if this property is writable
                If prop.CanWrite Then
                    ' get property value
                    Dim val As Object = prop.GetValue(Me, Nothing)

                    If TypeOf val Is ICloneable Then
                        ' clone the value
                        val = CType(val, ICloneable).Clone()
                    ElseIf TypeOf val Is IList AndAlso val.GetType().IsGenericType Then
                        ' clone the list
                        Dim list As IList = Activator.CreateInstance(val.GetType())
                        For Each item In CType(val, IList)
                            If TypeOf item Is ICloneable Then
                                list.Add(CType(item, ICloneable).Clone())
                            Else
                                list.Add(item)
                            End If
                        Next
                        val = list
                    End If

                    ' assign to cloned
                    prop.SetValue(toObj, val, Nothing)
                End If
            Next
        End Sub

        Public Overridable Function Clone() As Object Implements ICloneable.Clone
            Return Me.Clone(New Dictionary(Of Object, Object))
        End Function

        Public Overridable Function Clone(dict As Dictionary(Of Object, Object)) As Object Implements IDeepCloneable.Clone
            If dict.ContainsKey(Me) Then
                Return dict(Me)
            Else
                Dim cloned As Object = Activator.CreateInstance(Me.GetType())
                dict.Add(Me, cloned)

                ' get all public properties
                Dim properties As IEnumerable(Of PropertyInfo) = Me.GetType().GetProperties()

                ' for each property...
                For Each prop In properties
                    ' if this property is writable
                    If prop.CanWrite Then
                        ' get property value
                        Dim val As Object = prop.GetValue(Me, Nothing)

                        If TypeOf val Is IDeepCloneable Then
                            ' clone the value
                            val = CType(val, IDeepCloneable).Clone(dict)
                        ElseIf TypeOf val Is ICloneable Then
                            ' clone the value
                            val = CType(val, ICloneable).Clone()
                        ElseIf TypeOf val Is IList AndAlso val.GetType().IsGenericType Then
                            ' clone the list
                            Dim list As IList = Activator.CreateInstance(val.GetType())
                            For Each item In CType(val, IList)
                                If TypeOf item Is IDeepCloneable Then
                                    list.Add(CType(item, IDeepCloneable).Clone(dict))
                                ElseIf TypeOf item Is ICloneable Then
                                    list.Add(CType(item, ICloneable).Clone())
                                Else
                                    list.Add(item)
                                End If
                            Next
                            val = list
                        End If

                        ' assign to cloned
                        prop.SetValue(cloned, val, Nothing)
                    End If
                Next

                Return cloned
            End If
        End Function
    End Class
End Namespace