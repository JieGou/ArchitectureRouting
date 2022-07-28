using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class TextNoteOfPickUpFigureModel
  {
    public string Id { get ; set ; }
    public int Counter { get ; set ; }
    public XYZ RelatedPosition { get ; set ; }
    public XYZ? Position { get ; set ; }
    public string Content { get ; set ; }
    public WireLengthNotationAlignment PickUpAlignment { get ; set ; }
    public XYZ? Direction { get ; set ; }
    public int? RoomId { get ; set ; }

    public TextNoteOfPickUpFigureModel( string id, int counter, XYZ relatedPosition, XYZ? position, string content, WireLengthNotationAlignment pickUpAlignment, XYZ? direction, int? roomId )
    {
      Id = id ;
      Counter = counter ;
      RelatedPosition = relatedPosition ;
      Position = position ;
      Content = content ;
      PickUpAlignment = pickUpAlignment ;
      Direction = direction ;
      RoomId = roomId ;
    }
  }
}