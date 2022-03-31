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
  [DisplayNameKey( "Electrical.App.Commands.Shaft.PlaceTextNote", DefaultString = "Place Text" )]
  [Image( "resources/Initialize-16.bmp", ImageType = ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class UpdateAnnotationTextNote : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var arentTextNoteType = CheckExistedOrCreateTextNoteArent(doc);
      
      TextNoteArent.Clicked = true;
      var addId = commandData.Application.ActiveAddInId ;

      CheckAndUpdateStorageLines(doc);
      var textNoteUpdater = new TextNoteUpdaterChanged( addId ) ;
      var createUpdater = new TextNoteUpdaterCreated( addId, arentTextNoteType ) ;
      if ( ! textNoteUpdater.IsRegistered() ) {
        textNoteUpdater.Register() ;
      }
      
      if ( ! createUpdater.IsRegistered() ) {
        createUpdater.Register() ;
      }

      var viewUpdater = new ViewUpdater(addId);
      if (!viewUpdater.IsRegistered())
      {
        viewUpdater.Register(doc);
      }
      
      var textId = RevitCommandId.LookupPostableCommandId(PostableCommand.Text);
      if (textId != null)
      {
        commandData.Application.PostCommand(textId);
      }
      return Result.Succeeded ;
    }
    

    private TextNoteType CheckExistedOrCreateTextNoteArent(Document doc)
    {

      var list = new FilteredElementCollector(doc).WhereElementIsElementType().OfClass(typeof(TextNoteType));
      if (list.Select(x => x.Name).Contains(TextNoteArent.ArentTextNoteType))
      {
        var result =list.First(x=>x.Name == TextNoteArent.ArentTextNoteType) as TextNoteType;
        return result!;
      }
      using var tran = new Transaction(doc);
      tran.Start("Create Arent TextNote Type");
      var newTextNoteType = ((TextNoteType)list.First()).Duplicate(TextNoteArent.ArentTextNoteType) as TextNoteType;
      //newTextNoteType.get_Parameter("Text Font").Set("Arial Narrow");
      tran.Commit();
      return newTextNoteType!;
    }
    
    private XYZ GetMiddlePoint(DetailLine line)
    {
      var (x, y, _) = line.GeometryCurve.GetEndPoint(0);
      var (x1, y1, _) = line.GeometryCurve.GetEndPoint(1);
      
      return new XYZ((x + x1)/2,(y + y1)/2,0);
    }

    private IEnumerable<XYZ> GetListPointRef(BoundingBoxXYZ bb)
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

    private void CheckAndUpdateStorageLines(Document document)
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
        TextNoteArent.StorageLines[text.Id] = listLines;
      });
    }
  }
  
}