using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Electrical.App.Commands.Updater ;
using Arent3d.Revit;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Shaft.PlaceTextNoteDoubleBorder", DefaultString = "Note\nDouble Border" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class UpdateAnnotationTextNoteDoubleBorder : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var arentTextNoteType = CheckExistedOrCreateTextNoteArent(doc);
      
      var addId = commandData.Application.ActiveAddInId ;

      CheckAndUpdateStorageLines(doc);
      var textNoteUpdater = new TextNoteUpdaterChanged( addId, true ) ;
      var createUpdater = new TextNoteUpdaterCreated( addId, arentTextNoteType, true ) ;
      var viewUpdater = new ViewUpdater(addId, true);
      if ( ! textNoteUpdater.IsRegistered() ) 
        textNoteUpdater.Register() ;
      if ( ! createUpdater.IsRegistered() ) 
        createUpdater.Register() ;
      if (!viewUpdater.IsRegistered())
        viewUpdater.Register(doc);
      var textId = RevitCommandId.LookupPostableCommandId(PostableCommand.Text);
      if (textId != null)
      {
        commandData.Application.PostCommand(textId);
      }
      return Result.Succeeded ;
    }
    

    private static TextNoteType CheckExistedOrCreateTextNoteArent(Document doc)
    {

      var list = new FilteredElementCollector(doc).WhereElementIsElementType().OfClass(typeof(TextNoteType));
      if (list.Select(x => x.Name).Contains(ArentTextNote.ArentTextNoteType))
      {
        var result =list.First(x=>x.Name == ArentTextNote.ArentTextNoteType) as TextNoteType;
        return result!;
      }
      using var tran = new Transaction(doc);
      tran.Start("Create Arent TextNote Type");
      var newTextNoteType = ((TextNoteType)list.First()).Duplicate(ArentTextNote.ArentTextNoteType) as TextNoteType;
      //newTextNoteType.get_Parameter("Text Font").Set("Arial Narrow");
      tran.Commit();
      return newTextNoteType!;
    }
    
    private static XYZ GetMiddlePoint(DetailLine line)
    {
      var (x, y, _) = line.GeometryCurve.GetEndPoint(0);
      var (x1, y1, _) = line.GeometryCurve.GetEndPoint(1);
      
      return new XYZ((x + x1)/2,(y + y1)/2,0);
    }

    private static IEnumerable<XYZ> GetListPointRef(BoundingBoxXYZ bb)
    {
      var minX = bb.Min.X;
      var minY = bb.Min.Y;
      var maxX = bb.Max.X;
      var maxY = bb.Max.Y;

      return new List<XYZ>()
      {
        new XYZ((minX + maxX)/2, minY, 0),
        new XYZ(maxX, (maxY+minY)/2, 0),
        new XYZ((minX + maxX)/2, maxY, 0),
        new XYZ(minX, (maxY+minY)/2, 0),
      };
    }

    private static void CheckAndUpdateStorageLines(Document document)
    {
      var allText = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType().OfClass(typeof(TextNote)).Cast<TextNote>().ToList();
      var allLines = new FilteredElementCollector(document, document.ActiveView.Id).WhereElementIsNotElementType().OfClass(typeof(CurveElement)).Where(x=>x is DetailLine).Cast<DetailLine>().ToList();
      
      allText.ForEach(text =>
      {
        var refPoints = GetListPointRef(text.get_BoundingBox(document.ActiveView));
        var listLines = allLines.Where(line =>
        {
          return refPoints.Any(p => p.IsAlmostEqualTo(GetMiddlePoint(line)));
        }).Select(x=>x.Id).ToList();
        ArentTextNote.StorageLines[text.Id] = listLines;
      });
    }
  }
  
}