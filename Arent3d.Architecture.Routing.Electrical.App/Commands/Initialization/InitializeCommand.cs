using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit.UI ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Updater ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using MoreLinq ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.InitializeCommand", DefaultString = "Initialize" )]
  [Image( "resources/Initialize.png", ImageType = ImageType.Large )]
  public class InitializeCommand : InitializeCommandBase
  {
    private const string DefaultRegisterSymbolCompressionFileName = "2D Symbol DWG.zip" ;
    
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
      LoadDefaultRegisterSymbols( document ) ;
      RegisterLegendUpdater( document ) ;
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

    private static void LoadDefaultElectricalDb( Document document )
    {
      var uiDocument = new UIDocument( document ) ;
      var defaultSettingStorable = document.GetDefaultSettingStorable() ;
      var setupPrintStorable = document.GetSetupPrintStorable() ;
      var scale = setupPrintStorable.Scale ;
      var defaultSettingViewModel = new DefaultSettingViewModel( uiDocument, defaultSettingStorable, scale ) ;
      defaultSettingViewModel.LoadDefaultDb() ;
      RoutingAppUI.CeedModelDockPanelProvider?.CustomInitiator( uiDocument ) ;
    }
    
    private static void LoadDefaultRegisterSymbols( Document document )
    {
      var path = AssetManager.GetFolderCompressionFilePath( AssetManager.AssetPath, DefaultRegisterSymbolCompressionFileName ) ;
      if ( path == null ) return ;

      var level = document.ActiveView?.GenLevel ?? document.GetAllInstances<Level>().OrderBy( x => x.Elevation ).First() ;
      var registerSymbolStorable = new StorageService<Level,RegisterSymbolModel>( level )
      {
        Data =
        {
          FolderSelectedPath = path,
          BrowseFolderPath = path
        }
      } ;

      registerSymbolStorable.SaveChange() ;
    }

    private static void RegisterLegendUpdater(Document document)
    {
      var viewUpdater = new ViewUpdater( document.Application.ActiveAddInId ) ;
      if ( UpdaterRegistry.IsUpdaterRegistered( viewUpdater.GetUpdaterId() ) ) 
        return ;

      UpdaterRegistry.RegisterUpdater( viewUpdater, document ) ;
      var filter = new ElementMulticlassFilter( new List<Type> { typeof( Viewport ), typeof( ScheduleSheetInstance ) } ) ;
      var changeType = Element.GetChangeTypeElementAddition() ;
      UpdaterRegistry.AddTrigger( viewUpdater.GetUpdaterId(), document, filter, changeType ) ;
    }
  }
}