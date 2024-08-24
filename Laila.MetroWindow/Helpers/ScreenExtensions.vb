Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Namespace Helpers
    Friend Module ScreenExtensions
        <DllImport("User32.dll")>
        Private Function MonitorFromPoint(pt As System.Drawing.Point, dwFlags As UInt32) As IntPtr
        End Function

        <DllImport("Shcore.dll")>
        Private Function GetDpiForMonitor(hmonitor As IntPtr, dpiType As DpiType, ByRef dpiX As UInt32, ByRef dpiY As UInt32) As IntPtr
        End Function

        Enum DpiType
            Effective = 0
            Angular = 1
            Raw = 2
        End Enum

        <Extension>
        Public Sub GetDpi(screen As System.Windows.Forms.Screen, ByRef dpiX As UInt32, ByRef dpiY As UInt32)
            If System.Environment.OSVersion.Version.Major < 6 _
                OrElse (System.Environment.OSVersion.Version.Major = 6 AndAlso System.Environment.OSVersion.Version.Minor < 2) Then
                ' Shcore.dll doesn't exist on anything lower than Windows 8
                dpiX = 96
                dpiX = 96
            Else
                Dim pnt = New System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1)
                Dim mon = MonitorFromPoint(pnt, 2) ' MONITOR_DEFAULTTONEAREST
                GetDpiForMonitor(mon, DpiType.Effective, dpiX, dpiY)
            End If
        End Sub
    End Module
End Namespace