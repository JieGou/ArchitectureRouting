using System ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ChangePullBoxDimensionCommandBase : IExternalCommand
  {
    private const string ChangePullBoxDimensionSuccesfully = "Change pull box dimension succesfully" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var level = document.ActiveView.GenLevel ;
      try {
        var csvStorable = document.GetCsvStorable() ;
        var conduitsModelData = csvStorable.ConduitsModelData ;
        var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
        var storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( document.ActiveView.GenLevel ) ;
        var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;

        var pullBoxElements = document.GetAllElements<FamilyInstance>()
          .OfCategory( BuiltInCategory.OST_ElectricalFixtures )
          .Where( e => e.GetConnectorFamilyType() == ConnectorFamilyType.PullBox )
          .Where( e => Convert.ToBoolean( e.ParametersMap.get_Item( PullBoxRouteManager.IsAutoCalculatePullBoxSizeParameter ).AsString() ) )
          .ToList() ;

        foreach ( var pullBoxElement in pullBoxElements )
          PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, pullBoxElement, csvStorable, storageDetailSymbolService, storagePullBoxInfoServiceByLevel,
            conduitsModelData, hiroiMasterModels, PullBoxRouteManager.DefaultPullBoxLabel, null, true ) ;
        MessageBox.Show( ChangePullBoxDimensionSuccesfully ) ;
        return Result.Succeeded ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    protected abstract AddInType GetAddInType() ;

    public class PullBoxDimensions
    {
      public const string Depth = "Depth" ;
      public const string Width = "Width" ;
      public const string Height = "Height" ;
    }
  }
}