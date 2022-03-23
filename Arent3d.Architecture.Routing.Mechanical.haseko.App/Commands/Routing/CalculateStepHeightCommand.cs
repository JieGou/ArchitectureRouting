using System ;
using System.Windows ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.Haseko.App.Commands.Routing.CalculateStepHeightCommand", DefaultString = "Step Height" )]
  [Image( "resources/step_height.png" )]
  public class CalculateStepHeightCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var doc = uiDoc.Document ;

      var firstRef = uiDoc.Selection.PickObject( ObjectType.Element, "Please pick first element." ) ;
      var secondRef = uiDoc.Selection.PickObject( ObjectType.Element, "Please pick second element." ) ;

      var firstElement = doc.GetElement( firstRef.ElementId ) ;
      var secondElement = doc.GetElement( secondRef.ElementId ) ;

      var firstBtnElevation = firstElement.get_BoundingBox( doc.ActiveView ).Min ;
      var secondBtnElevation = secondElement.get_BoundingBox( doc.ActiveView ).Min ;

      double height = 0 ;
      if ( firstBtnElevation != null && secondBtnElevation != null ) {
        height = Math.Abs( firstBtnElevation.Z - secondBtnElevation.Z ) * 304.8 ;
      }

      MessageBox.Show( $"Step Height is {Math.Round( height )} mm.\nステップの高さは{Math.Round( height )}mmです。", "Notification", MessageBoxButton.OK, MessageBoxImage.Information ) ;
      return Result.Succeeded ;
    }
  }
}