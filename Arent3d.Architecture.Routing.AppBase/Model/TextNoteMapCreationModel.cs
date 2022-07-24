using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class TextNoteMapCreationModel
  {
    public string Id { get ; set ; }
    public int Counter { get ; set ; }
    public XYZ PositionRef { get ; set ; }
    public XYZ? Position { get ; set ; }
    public string Content { get ; set ; }
    public TextNotePickUpAlignment PickUpAlignment { get ; set ; }
    public XYZ? Direction { get ; set ; }
    public int? RoomId { get ; set ; }

    public TextNoteMapCreationModel( string id, int counter, XYZ positionRef, XYZ? position, string content, TextNotePickUpAlignment pickUpAlignment, XYZ? direction, int? roomId )
    {
      Id = id ;
      Counter = counter ;
      PositionRef = positionRef ;
      Position = position ;
      Content = content ;
      PickUpAlignment = pickUpAlignment ;
      Direction = direction ;
      RoomId = roomId ;
    }
  }
}