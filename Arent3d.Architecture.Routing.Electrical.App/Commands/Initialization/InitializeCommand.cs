using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
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
      // 電気ルートアシスト用のファミリを追加する必要があるため、追加のチェックを入れる
      return base.RoutingSettingsAreInitialized( document ) && document.AllFamiliesAreLoaded<ElectricalRoutingFamilyType>() && document.AllElectricalRoutingParametersAreRegistered() ;
    }

    protected override void BeforeInitialize( Document document )
    {
      FilterHelper.InitialFilters( document ) ;
    }
    
    protected override void AfterInitialize( Document document )
    {
      LoadDefaultElectricalDb( document ) ;
    }

    protected override bool Setup( Document document )
    {
      var baseSetupResult = base.Setup( document ) ;
      if ( ! baseSetupResult ) return false ;

      document.MakeCertainAllElectricalRoutingFamilies() ;
      document.MakeElectricalRoutingElementParameters() ;

      RoutingElementExtensions.AddArentConduitType( document ) ;
      var connectorOneSide = document.GetAllElements<FamilyInstance>().OfCategory( BuiltInCategorySets.Connectors ) ;
      foreach ( var connector in connectorOneSide ) {
        connector.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
      }

      return RoutingSettingsAreInitialized( document ) ;
    }

    private void LoadDefaultElectricalDb( Document document )
    {
      var activeViewName = document.ActiveView.Name ;
      var defaultSettingStorable = document.GetDefaultSettingStorable() ;
      var setupPrintStorable = document.GetSetupPrintStorable() ;
      var scale = setupPrintStorable.Scale ;
      var defaultSettingViewModel = new DefaultSettingViewModel( new UIDocument( document ), defaultSettingStorable,
        scale, activeViewName ) ;
      defaultSettingViewModel.LoadDefaultDb() ;
    }
  }
}