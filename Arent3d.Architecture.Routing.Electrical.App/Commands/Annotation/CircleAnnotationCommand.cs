using System ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Annotation.CircleAnnotationCommand", DefaultString = "Circle \nText Box" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class CircleAnnotationCommand : IExternalCommand
  {
    private const string TransactionName = "Electrical.App.Commands.Annotation.CircleAnnotationCommandTrans" ;
    private const string CircleAnnotationName = "Circle Annotation" ;
    private const string StatusPrompt = "配置場所を選択して下さい。" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var application = commandData.Application ;
        var uiDocument = application.ActiveUIDocument ;
        var document = uiDocument.Document ;

        using var transaction = new Transaction( document ) ;
        transaction.Start( TransactionName ) ;
        
        var (originX, originY, _) = uiDocument.Selection.PickPoint( StatusPrompt) ;
        var level = uiDocument.ActiveView.GenLevel ;
        var heightOfConnector = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        GenerateCircleAnnotation( uiDocument, new XYZ(originX, originY, heightOfConnector), level) ;
         
        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    
    private static Element GenerateCircleAnnotation( UIDocument uiDocument, XYZ xyz, Level level )
    {
      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.CircleAnnotation ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return routingSymbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
    }
  }
}