using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class SymbolInformationCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      //Get pickObject.
      //1. If object is null or object isn't SymbolInformation type => get pickPosition => create new SymbolInformation.
      //2. If object isn't null => get SymbolInformation from that one
      //3. Show dialog


      try {
        var uiDocument = commandData.Application.ActiveUIDocument ;
        var document = uiDocument.Document ;
        var symbolInformations = document.GetSymbolInformationStorable().AllSymbolInformationModelData ;
        SymbolInformationModel model ;
        XYZ xyz = XYZ.Zero ;

        var pickedObject = uiDocument.Selection.PickObject( ObjectType.Element, message ) ;
        //pickedObject isn't null
        if ( null != pickedObject ) {
          var symbolInformation = symbolInformations.FirstOrDefault( x => x.Id == pickedObject.ElementId.ToString() ) ;
          //pickedObject is SymbolInformationModel
          if ( null != symbolInformation ) {
            model = symbolInformation ;
          }
          //pickedObject ISN'T SymbolInformationModel
          else {
            var element = document.GetElement( pickedObject.ElementId ) ;
            if ( null != element.Location ) {
              xyz = element.Location is LocationPoint pPoint ? pPoint.Point : XYZ.Zero;
            }

            var symbolInformationInstance = GenerateSymbolInformation( uiDocument, xyz ) ;
            model = new SymbolInformationModel( symbolInformationInstance.Id.ToString(), null, null ) ;
          }
        }
        else {
          xyz = uiDocument.Selection.PickPoint( "SymbolInformationの配置場所を選択して下さい。" ) ;
          var symbolInformationInstance = GenerateSymbolInformation( uiDocument, xyz ) ;
          model = new SymbolInformationModel( symbolInformationInstance.Id.ToString(), null, null ) ;
        }

        var viewModel = new SymbolInformationViewModel( document, model ) ;
        var dialog = new SymbolInformationDialog( viewModel ) ;

        var resultDialog = dialog.ShowDialog() ;
        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Cancelled ;
      }
    }

    private FamilyInstance GenerateSymbolInformation( UIDocument uiDocument, XYZ xyz )
    {
      var level = uiDocument.ActiveView.GenLevel ;
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolStar ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return symbol.Instantiate( xyz, level, StructuralType.NonStructural ) ;
    }
  }
}