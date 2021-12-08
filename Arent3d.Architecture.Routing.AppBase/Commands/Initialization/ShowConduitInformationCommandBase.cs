using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowConduitInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var wiresAndCablesModelData = doc.GetCsvStorable().WiresAndCablesModelData ;
      var conduitsModelData = doc.GetCsvStorable().ConduitsModelData ;
      var hiroiSetCdMasterNormalModelData = doc.GetCsvStorable().HiroiSetCdMasterNormalModelData ;
      ObservableCollection<ConduitInformationModel> conduitInformationModels =
        new ObservableCollection<ConduitInformationModel>() ;
      try {
        var pickedObjects = uiDoc.Selection
          .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
          .Where( p => p is FamilyInstance or Conduit ) ;
        foreach ( var element in pickedObjects ) {
          var conduitModel = conduitsModelData.FirstOrDefault() ;
          var wireType = wiresAndCablesModelData.FirstOrDefault() ;
          var heroiCdSet = hiroiSetCdMasterNormalModelData.FirstOrDefault() ;
          string floor = doc.GetElementById<Level>( element.GetLevelId() )?.Name ?? string.Empty ;

          conduitInformationModels.Add( new ConduitInformationModel( false, floor, wireType?.COrP,
            wireType?.WireType, wireType?.DiameterOrNominal, wireType?.NumberOfHeartsOrLogarithm,
            wireType?.NumberOfConnections, string.Empty, string.Empty, string.Empty, conduitModel?.PipingType, conduitModel?.Size,
            "", heroiCdSet?.ConstructionClassification, wireType?.Classification,
            "","", "" ) ) ;
        }
      }
      catch ( Exception ex ) {
        string e = ex.Message ;
        return Result.Cancelled ;
      }

      ConduitInformationViewModel viewModel = new ConduitInformationViewModel( conduitInformationModels ) ;
      var dialog = new ConduitInformationDialog( viewModel ) ;
      dialog.ShowDialog() ;

      if ( dialog.DialogResult ?? false ) {
        return Result.Succeeded ;
      }
      else {
        return Result.Cancelled ;
      }
    }

    private class ConduitPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return ( e.Category.Id.IntegerValue.Equals( (int) BuiltInCategory.OST_Conduit ) ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
  }
}