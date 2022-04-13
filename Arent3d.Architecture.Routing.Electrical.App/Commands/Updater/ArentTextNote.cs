using System.Collections.Generic ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Updater
{
    public static class ArentTextNote
  {
    private const double BorderOffset = 0.15 ;
    private const double Padding = 0.5 ;
    public static bool Clicked;
    public const string ArentTextNoteType = "ArrentTextNoteType";
    public static Dictionary<ElementId, List<ElementId>> StorageLines = new();
    public static void CreateSingleBorderText(TextNote text )
    {
      var document = text.Document ;
      var bb = text.get_BoundingBox( document.ActiveView ) ;
      var min = bb.Min + new XYZ(Padding, Padding, 0) ;
      var max = bb.Max - new XYZ(Padding, Padding, 0) ;

      var curs = new CurveArray() ;
      curs.Append( Line.CreateBound( min, new XYZ( max.X, min.Y, min.Z ) )  );
      curs.Append( Line.CreateBound(new XYZ( max.X, min.Y, min.Z ), max )  );
      curs.Append( Line.CreateBound( max, new XYZ( min.X, max.Y, min.Z ) )  );
      curs.Append( Line.CreateBound( new XYZ( min.X, max.Y, min.Z ), min ) );
      
      var curveArray = document.Create.NewDetailCurveArray( document.ActiveView, curs) ;
      var listCurves = (from DetailCurve curve in curveArray select curve.Id).ToList();
      StorageLines[text.Id] = listCurves;
    }
    
    public static void CreateDoubleBorderText(TextNote text )
    {
      var document = text.Document ;
      var bb = text.get_BoundingBox( document.ActiveView ) ;
      var min = bb.Min + new XYZ(Padding, Padding, 0) ;
      var max = bb.Max - new XYZ(Padding, Padding, 0);

      var curs = new CurveArray() ;
      curs.Append( Line.CreateBound( min, new XYZ( max.X, min.Y, min.Z ) ) );
      curs.Append( Line.CreateBound( min + new XYZ(-1, -1, 0) * BorderOffset, new XYZ( max.X, min.Y, min.Z ) + new XYZ(1, -1, 0) * BorderOffset ) );
      
      curs.Append( Line.CreateBound(new XYZ( max.X, min.Y, min.Z ), max )  );
      curs.Append( Line.CreateBound(new XYZ( max.X, min.Y, min.Z ) + new XYZ(1, -1, 0) * BorderOffset, max + new XYZ(1, 1, 0) * BorderOffset ) );
      
      curs.Append( Line.CreateBound( max, new XYZ( min.X, max.Y, min.Z ) )  );
      curs.Append( Line.CreateBound( max + new XYZ(1, 1, 0) * BorderOffset, new XYZ( min.X, max.Y, min.Z ) + new XYZ(-1, 1, 0) * BorderOffset )  );
      
      curs.Append( Line.CreateBound( new XYZ( min.X, max.Y, min.Z ), min ) );
      curs.Append( Line.CreateBound( new XYZ( min.X, max.Y, min.Z ) + new XYZ(-1, 1, 0) * BorderOffset, min + new XYZ(-1, -1, 0) * BorderOffset) );
      
      var curveArray = document.Create.NewDetailCurveArray( document.ActiveView, curs) ;
      var listCurves = (from DetailCurve curve in curveArray select curve.Id).ToList();
      StorageLines[text.Id] = listCurves;
    }

    public static bool CheckIdIsDeleted(Document doc, ElementId id)
    {
      return doc.GetElement(id) != null;
    }
  }
}