Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace Converters
    Public Class ShadowMarginConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Return New Thickness(0, 0, System.Convert.ToDouble(value), System.Convert.ToDouble(value))
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace