using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : InitializeCommandBase
  {
    protected override bool RoutingSettingsAreInitialized( Document document )
    {
      // 電気ルートアシストには電気ルートアシスト用のファミリを追加する必要があるため、追加のチェックを入れる
      return base.RoutingSettingsAreInitialized(document) &&  document.AllFamiliesAreLoaded<ElectricalRoutingFamilyType>() ;
    }
    protected override bool Setup( Document document )
    {
      var baseSetupResult = base.Setup( document ) ;
      if ( ! baseSetupResult ) return false ;

      document.MakeCertainAllElectricalRoutingFamilies() ;
      document.LoadAllParametersFromFile<ConnectorFamilyParameter>( AssetManager.GetConnectorSharedParameterPath() ) ;
      
      RoutingElementExtensions.AddArentConduitType( document ) ;
      var connectorOneSide = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Connectors ) ;
      foreach ( var connector in connectorOneSide ) {
        connector.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
      }

      return RoutingSettingsAreInitialized( document ) ;
    }
  }
}