using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
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
      ObservableCollection<ConduitInformationModel> conduitInformationModels =
        new ObservableCollection<ConduitInformationModel>() ;
      try {
        var pickedObjects = uiDoc.Selection
          .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
          .Where( p => p is FamilyInstance or Conduit ) ;
        foreach ( var element in pickedObjects ) {
          string diameter = element.LookupParameter( "Diameter(Trade Size)" ).AsValueString().Replace( " ", "" ) ;
          var conduitModel = conduitsModelData.FirstOrDefault( x => x.Size.Equals( diameter ) ) ;
          var wireType = wiresAndCablesModelData.FirstOrDefault() ;
          List<string> paramList = new List<string>() ;
          foreach ( Parameter param in element.Parameters ) {
            if(!string.IsNullOrEmpty( param.AsValueString() ) )
            {
              paramList.Add( param.Definition.Name+" - "+ param.AsValueString() );
            }
           
          }
          if ( conduitModel == null ) continue ; 
          if ( wireType != null )
            conduitInformationModels.Add( new ConduitInformationModel( true,
              element.LookupParameter( "Reference Level" ).AsValueString(), wireType.COrP, wireType.WireType,
              wireType.FinishedOuterDiameter, wireType.DiameterOrNominal, wireType.NumberOfConnections, "8", "9", "10", conduitModel.PipingType,
              conduitModel.Size, conduitModel.InnerCrossSectionalArea, string.Empty, "15", wireType.Classification,
              conduitModel.Classification, wireType.Name, "19" ) ) ;
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
