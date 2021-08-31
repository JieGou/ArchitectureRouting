using System ;
using System.Collections.Generic;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewConnectorCommandBase : IExternalCommand
  {
    private static readonly double DefaultHighestLevelHeight = ( 3.0 ).MetersToRevitUnits() ;
    private static readonly double DefaultConnectorSize = ( 0.5 ).MetersToRevitUnits() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var (originX, originY, originZ) = uiDocument.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
        double sizeX = DefaultConnectorSize, sizeY = DefaultConnectorSize;

        var result = document.Transaction( "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Pipe Spaces" ), _ =>
        {
            GenerateConnector(uiDocument, originX, originY, sizeX, sizeY, originZ, uiDocument.ActiveView.GenLevel);

          return Result.Succeeded ;
        });

        return result;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void GenerateConnector(UIDocument uiDocument, double originX, double originY, double originZ, double sizeX, double sizeY, Level level)
    {
        var symbol = uiDocument.Document.GetFamilySymbol(RoutingFamilyType.ConnectorOneSide)!;
        var instance = symbol.Instantiate(new XYZ(originX, originY, originZ), level, StructuralType.NonStructural);
    }

    private static Level? GetUpperLevel( Level refRevel )
    {
      var minElevation = refRevel.Elevation + refRevel.Document.Application.ShortCurveTolerance ;
      return refRevel.Document.GetAllElements<Level>().Where( level => minElevation < level.Elevation ).MinItemOrDefault( level => level.Elevation ) ;
    }

    private static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }
    private static void SetParameter( FamilyInstance instance, BuiltInParameter parameter, double value )
    {
      instance.get_Parameter( parameter )?.Set( value ) ;
    }
  }
}