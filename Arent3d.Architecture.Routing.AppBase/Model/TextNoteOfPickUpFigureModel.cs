using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class TextNoteOfPickUpFigureModel
  {
    public TextNote? TextNote { get ; set ; }
    public int Counter { get ; set ; }
    public XYZ RelatedPosition { get ; set ; }
    public XYZ? Position { get ; set ; }
    public string Content { get ; set ; }
    public WireLengthNotationAlignment PickUpAlignment { get ; set ; }
    public XYZ? Direction { get ; set ; }
    public string? BoardId { get ; set ; }

    public TextNoteOfPickUpFigureModel( TextNote? textNote, int counter, XYZ relatedPosition, XYZ? position, string content, WireLengthNotationAlignment pickUpAlignment, XYZ? direction, string? boardId )
    {
      TextNote = textNote ;
      Counter = counter ;
      RelatedPosition = relatedPosition ;
      Position = position ;
      Content = content ;
      PickUpAlignment = pickUpAlignment ;
      Direction = direction ;
      BoardId = boardId ;
    }
  }
}