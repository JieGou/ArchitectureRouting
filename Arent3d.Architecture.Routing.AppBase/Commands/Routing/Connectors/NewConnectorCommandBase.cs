using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing.Connectors
{
  public abstract class NewConnectorCommandBase : IExternalCommand
  {
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    private string _defaultConstructionItem = "未設定" ;
    private const string DefaultConnectorWidth = "100" ;
    private const string DefaultConnectorLength = "150" ;
    protected virtual ConnectorFamilyType? ConnectorType => null ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      _defaultConstructionItem = document.GetDefaultConstructionItem() ;
      try {
        var (originX, originY, originZ) = uiDocument.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;

        var result = document.Transaction( "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Pipe Spaces" ), _ =>
        {
          var level = uiDocument.ActiveView.GenLevel ;
          var heightOfConnector = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
          GenerateConnector( uiDocument, originX, originY, heightOfConnector, level ) ;

          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private void GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;

      if ( false == instance.TryGetProperty( "W", out string? connectorWidthString ) && string.IsNullOrEmpty( connectorWidthString ) ) connectorWidthString = DefaultConnectorWidth;
      if  ( false == instance.TryGetProperty( "D",out string? connectorLengthString) && string.IsNullOrEmpty( connectorLengthString )) connectorLengthString = DefaultConnectorLength;

      if ( false == int.TryParse( connectorLengthString, out var connectorWidth ) ) return ;
      if ( false == int.TryParse( connectorWidthString,out var connectorLength )) return;

      var scaleRatio = GetConnectorScaleRatio( uiDocument.Document )/100.0 ;

      instance.TrySetProperty( "W", (connectorWidth * scaleRatio).MillimetersToRevitUnits() ) ;
      instance.TrySetProperty( "D", (connectorLength * scaleRatio ).MillimetersToRevitUnits()) ;

      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, _defaultConstructionItem ) ;
       
      //Set value for isEcoMode property from default value in DB
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, uiDocument.Document.GetDefaultSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
 
      if ( ConnectorType == null ) return ;
      instance.SetConnectorFamilyType( ConnectorType ?? ConnectorFamilyType.Sensor ) ;
    }

    private double GetConnectorScaleRatio( Document doc )
    {
      var documentScale = doc.ActiveView.Scale ;

      return documentScale switch
      {
        <=20 => documentScale * 200.0 /100.0,
        <=30 => documentScale * 167.7 / 100.0,
        <=60 => documentScale * 133.3 / 100.0,
        <=150 => documentScale * 100.0 / 100.0,
        <=500 => documentScale * 76.7 / 100.0,
        <=9999 => documentScale * 50.0 / 100.0,
        _ => throw new ArgumentOutOfRangeException()
      } ;

    }
  }
}