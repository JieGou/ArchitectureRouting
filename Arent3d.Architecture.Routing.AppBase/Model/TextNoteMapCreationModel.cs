using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class TextNoteMapCreationModel
  {
    public string TextNoteId { get ; set ; }
    public int TextNoteCounter { get ; set ; }
    public XYZ TextNotePositionRef { get ; set ; }
    public XYZ? TextNotePosition { get ; set ; }
    public XYZ? TextNoteDirection { get ; set ; }

    public TextNoteMapCreationModel( string textNoteId, int textNoteCounter, XYZ textNotePositionRef, XYZ? textNotePosition, XYZ? textNoteDirection)
    {
      TextNoteId = textNoteId ;
      TextNoteCounter = textNoteCounter ;
      TextNotePositionRef = textNotePositionRef ;
      TextNotePosition = textNotePosition ;
      TextNoteDirection = textNoteDirection ;
    }
  }
}