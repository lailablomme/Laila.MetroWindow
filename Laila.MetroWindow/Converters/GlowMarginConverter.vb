﻿Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace Converters
    Public Class GlowMarginConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Return New Thickness(System.Convert.ToDouble(value) * 1)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace