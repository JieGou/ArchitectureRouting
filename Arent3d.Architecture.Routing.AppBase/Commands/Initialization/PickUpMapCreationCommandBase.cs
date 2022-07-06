using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class PickUpMapCreationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;

      var viewModel = new PickUpMapCreationViewModel( document ) ;
      var dialog = new PickUpMapCreationDialog( viewModel ) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        if ( viewModel.IsDoconEnable ) {
          var level = document.ActiveView.GenLevel.Name ;
          var codeList = viewModel.GetCodeList() ;
      
          foreach ( var code in codeList ) {
            var conduitPickUpModels = viewModel.PickUpModels.Where( p => p.Specification2 == code && p.Floor == level && p.EquipmentType == PickUpViewModel.ProductType.Conduit.GetFieldName() ).GroupBy( x => x.ProductCode, ( key, p ) => new { ProductCode = key, PickUpModels = p.ToList() } ) ;
            PickUpShow(document, viewModel,conduitPickUpModels.First().PickUpModels ) ;
          }
        }
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }
    
    private void PickUpShow(Document document, PickUpMapCreationViewModel viewModel , List<PickUpModel> pickUpModels )
    {
      if ( ! pickUpModels.Any() ) return ;
      var pickUpNumbers = viewModel.GetPickUpNumbersList( pickUpModels ) ;
      var pickUpModel = pickUpModels.First() ;
      var routeName = pickUpModel.RouteName ;
      Dictionary<string, int> trajectory = new Dictionary<string, int>() ;
      foreach ( var pickUpNumber in pickUpNumbers ) {
        double seenQuantity = 0 ;
        Dictionary<string, double> notSeenQuantities = new Dictionary<string, double>() ;
        var items = pickUpModels.Where( p => p.PickUpNumber == pickUpNumber ).ToList() ;
        foreach ( var item in items.Where( item => ! string.IsNullOrEmpty( item.Quantity ) ) ) {
          double.TryParse( item.Quantity, out var quantity ) ;
          if ( ! string.IsNullOrEmpty( item.Direction ) ) {
            if ( ! notSeenQuantities.Keys.Contains( item.Direction ) ) {
              notSeenQuantities.Add( item.Direction, 0 ) ;
            }
            notSeenQuantities[ item.Direction ] += quantity ;
          }
          else
            seenQuantity += quantity ;
        }
      }

      var allConduits = new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_ConduitFitting ) ;
      
      var routeNames = allConduits.Where( conduit => conduit.GetRouteName() == routeName );
      
    }
  }
}