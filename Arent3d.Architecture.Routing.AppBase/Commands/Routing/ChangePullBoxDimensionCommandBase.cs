using System ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ChangePullBoxDimensionCommandBase : IExternalCommand
  {
    private const string ChangePullBoxDimensionSuccessfully = "Change pull box dimension succesfully" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var level = document.ActiveView.GenLevel ;
      var scale = Model.ImportDwgMappingModel.GetDefaultSymbolMagnification( document ) ;
      var baseLengthOfLine = scale / 100d ;
      try {
        var result = document.TransactionGroup( "TransactionName.Commands.Routing.ChangePullBoxDimension".GetAppStringByKeyOrDefault( "Change Pull Box Dimension" ), _ =>
        {
          var csvStorable = document.GetCsvStorable() ;
          var conduitsModelData = csvStorable.ConduitsModelData ;
          var hiroiMasterModels = csvStorable.HiroiMasterModelData ;
          var storageDetailSymbolService = new StorageService<Level, DetailSymbolModel>( level ) ;
          var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
          var allConduits = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).OfType<Conduit>().EnumerateAll() ;
          var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;

          var pullBoxElements = document.GetAllElements<FamilyInstance>()
            .OfCategory( BuiltInCategory.OST_ElectricalFixtures )
            .Where( e => e.GetConnectorFamilyType() == ConnectorFamilyType.PullBox && e.LevelId == level.Id )
            .ToList() ;
        
          foreach ( var pullBoxElement in pullBoxElements )
            PullBoxRouteManager.ChangeDimensionOfPullBoxAndSetLabel( document, routeCache, allConduits, baseLengthOfLine, pullBoxElement, csvStorable, storageDetailSymbolService, storagePullBoxInfoServiceByLevel,
              conduitsModelData, hiroiMasterModels, PullBoxRouteManager.DefaultPullBoxLabel, null, Convert.ToBoolean( pullBoxElement.ParametersMap.get_Item( PullBoxRouteManager.IsAutoCalculatePullBoxSizeParameter ).AsString() ) ) ;
          MessageBox.Show( ChangePullBoxDimensionSuccessfully ) ;
          return Result.Succeeded ;
        } ) ;

        return result ;
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
  }
  public static class PullBoxDimensions
  {
    public const string Depth = nameof(Depth) ;
    public const string Width = nameof(Width) ;
    public const string Height = nameof(Height) ;
  }
}