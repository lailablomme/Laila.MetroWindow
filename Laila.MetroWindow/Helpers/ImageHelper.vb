Imports System.IO
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Namespace Helpers
    Friend Class ImageHelper
        Friend Shared Function GetBestSize(bytes As Byte(), preferredSize As Integer) As ImageSource
            If bytes(0) = 0 AndAlso bytes(1) = 0 AndAlso bytes(2) = 1 AndAlso bytes(3) = 0 Then
                ' this is probably an .ico file
                Dim mem As MemoryStream = New MemoryStream(bytes)
                Dim imageSource As ImageSource = Nothing
                Dim decoder As IconBitmapDecoder = Nothing
                decoder = New IconBitmapDecoder(mem, BitmapCreateOptions.None, BitmapCacheOption.None)

                ' try get preferred size
                imageSource = decoder.Frames.FirstOrDefault(Function(f) f.Width = preferredSize)

                If imageSource Is Nothing Then
                    ' try get bigger
                    imageSource = decoder.Frames.OrderBy(Function(f) f.Width).FirstOrDefault(Function(f) f.Width > preferredSize)
                End If

                If imageSource Is Nothing Then
                    ' try get smaller
                    imageSource = decoder.Frames.OrderByDescending(Function(f) f.Width).FirstOrDefault(Function(f) f.Width < preferredSize)
                End If

                If imageSource Is Nothing Then
                    ' get anything
                    imageSource = decoder.Frames(0)
                End If

                Return imageSource.Clone()
                'End Using
            Else
                Dim img As BitmapImage = New BitmapImage()
                Using mem As MemoryStream = New MemoryStream(bytes)
                    mem.Position = 0
                    img.BeginInit()
                    img.CreateOptions = BitmapCreateOptions.PreservePixelFormat
                    img.CacheOption = BitmapCacheOption.OnLoad
                    img.UriSource = Nothing
                    img.StreamSource = mem
                    img.EndInit()
                End Using
                img.Freeze()
                Return img
            End If
        End Function

        Friend Shared Function GetBestSize(url As String, preferredSize As Integer) As ImageSource
            Dim imageSource As ImageSource = Nothing

            If Not String.IsNullOrWhiteSpace(url) Then
                If url.ToLower().EndsWith(".ico") Then
                    Dim decoder As IconBitmapDecoder = Nothing
                    decoder = New IconBitmapDecoder(New Uri(url), BitmapCreateOptions.None, BitmapCacheOption.None)

                    ' try get 16x16
                    imageSource = decoder.Frames.FirstOrDefault(Function(f) f.Width = preferredSize)

                    If imageSource Is Nothing Then
                        ' try get bigger
                        imageSource = decoder.Frames.OrderBy(Function(f) f.Width).FirstOrDefault(Function(f) f.Width > preferredSize)
                    End If

                    If imageSource Is Nothing Then
                        ' try get smaller
                        imageSource = decoder.Frames.OrderByDescending(Function(f) f.Width).FirstOrDefault(Function(f) f.Width < preferredSize)
                    End If

                    If imageSource Is Nothing Then
                        ' get anything
                        imageSource = decoder.Frames(0)
                    End If
                Else
                    Dim isc As ImageSourceConverter = New ImageSourceConverter()
                    imageSource = isc.ConvertFromInvariantString(url)
                End If
            End If

            Return imageSource
        End Function
    End Class
End Namespace