using System.Collections.Generic ;
using System.Linq ;
using System.Threading ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ShowHeightSettingCommandBase : IExternalCommand
  {
    protected UIDocument UiDocument { get ; private set ; } = null! ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;

      // get data of height setting from snoop DB
      HeightSettingStorable settingStorables = document.GetHeightSettingStorable() ;

      var viewModel = new ViewModel.HeightSettingViewModel( settingStorables ) ;
      var dialog = new HeightSettingDialog( viewModel ) ;

      dialog.ShowDialog() ;

      if ( dialog.DialogResult ?? false ) {
        return document.Transaction( "TransactionName.Commands.Routing.HeightSetting", _ =>
        {
          var newStorage = viewModel.SettingStorable ;
          if ( ShouldApplySetting( document, settingStorables ) ) {
            var tokenSource = new CancellationTokenSource() ;
            using var progress = ProgressBar.ShowWithNewThread( tokenSource ) ;
            progress.Message = "Height Setting..." ;

            using ( var p = progress?.Reserve( 0.5 ) ) {
              ApplySetting( document, newStorage, p ) ;
            }

            using ( progress?.Reserve( 0.5 ) ) {
              SaveSetting( document, settingStorables ) ;
            }

            AfterApplySetting() ;
          }

          return Result.Succeeded ;
        } ) ;
      }
      else {
        return Result.Cancelled ;
      }

    }

    public void ApplySetting( Document document, HeightSettingStorable settingStorables, IProgressData? progressData = null )
    {
      if ( settingStorables == null ) return ;

      FilteredElementCollector connectorCollector = new FilteredElementCollector( document ) ;
      List<BuiltInCategory> builtInCats = new List<BuiltInCategory>() ;
      builtInCats.Add( BuiltInCategory.OST_ElectricalFixtures ) ;
      builtInCats.Add( BuiltInCategory.OST_ElectricalEquipment ) ;
      ElementMulticategoryFilter filterConnectors = new ElementMulticategoryFilter( builtInCats ) ;
      connectorCollector.WherePasses( filterConnectors ) ;
      var allConnectors = connectorCollector.OfType<FamilyInstance>() ;
      var connectors = allConnectors.GroupBy( x => x.GetLevelId() ).ToDictionary( g => g.Key, g => g.ToList() ) ;


      var allConduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit )
        .AsEnumerable()
        .OfType<Conduit>() ;
      var conduits = allConduits.GroupBy( x => x.ReferenceLevel.Id ).ToDictionary( g => g.Key, g => g.ToList() ) ;

      var totalProgress = 0.00 + allConnectors.Count() + allConduits.Count() ;
      totalProgress = totalProgress == 0 ? 1.00 : totalProgress ;

      foreach ( Level level in settingStorables.Levels ) {
        var heightConnector = settingStorables[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        // Set Elevation from floor for all connector on this floor
        if ( connectors.ContainsKey( level.Id ) ) {
          using ( var p = progressData?.Reserve( connectors[ level.Id ].Count / totalProgress ) ) {
            p.ForEach( connectors[ level.Id ].Count, connectors[ level.Id ], connector =>
            {
              var elevationFromFloor = connector.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).AsDouble() ;
              if ( elevationFromFloor != heightConnector ) {
                connector.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( heightConnector ) ;
              }
            } ) ;
          }
        }

        // Set Elevation for level
        level.Elevation = settingStorables[ level ].Elevation.MillimetersToRevitUnits() ;
      }

      // Set height for Arent shaft
      var arentShafts = document.GetAllFamilyInstances( RoutingFamilyType.Shaft ) ;
      var levels = settingStorables.Levels.OrderBy( x => x.Elevation ).ToList() ;
      Level? lowestLevel = levels.FirstOrDefault() ;
      Level? highestLevel = levels.LastOrDefault() ;
      if ( lowestLevel != null && highestLevel != null ) {
        foreach ( FamilyInstance shaft in arentShafts ) {
          shaft.LookupParameter( "高さ" ).Set( highestLevel!.Elevation ) ;
        }
      }
    }
    protected virtual void AfterApplySetting()
    {
      // TODO after apply height setting
    }
    private static void SaveSetting( Document document, HeightSettingStorable newSettings )
    {
      newSettings.Save() ;
    }

    private static bool ShouldApplySetting( Document document, HeightSettingStorable newSettings )
    {
      var old = document.GetAllStorables<HeightSettingStorable>().FirstOrDefault() ; // generates new instance from document
      return ( false == newSettings.Equals( old ) ) ;
    }
  }
}