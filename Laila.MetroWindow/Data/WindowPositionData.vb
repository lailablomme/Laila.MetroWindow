Imports System.IO
Imports System.Windows
Imports System.Xml.Serialization

Namespace Data
    Public Class WindowPositionData
        Inherits BaseData

        Public Property State As WindowState
        Public Property Left As Double
        Public Property Top As Double
        Public Property Width As Double
        Public Property Height As Double

        Public Function Serialize() As String
            Using memoryStream As MemoryStream = New MemoryStream()
                Dim serializer As XmlSerializer = New XmlSerializer(GetType(WindowPositionData))
                serializer.Serialize(memoryStream, Me)
                Dim xmlBytes As Byte() = memoryStream.ToArray()
                Return System.Text.Encoding.UTF8.GetString(xmlBytes)
            End Using
        End Function

        Public Shared Function Deserialize(xml As String) As WindowPositionData
            Dim xmlBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(xml)
            Using memoryStream As MemoryStream = New MemoryStream(xmlBytes)
                Dim serializer As XmlSerializer = New XmlSerializer(GetType(WindowPositionData))
                Return serializer.Deserialize(memoryStream)
            End Using
        End Function
    End Class
End Namespace