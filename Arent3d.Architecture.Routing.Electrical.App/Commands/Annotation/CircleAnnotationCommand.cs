using System ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Annotation.CircleAnnotationCommand", DefaultString = "Circle \nText Box" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class CircleAnnotationCommand : IExternalCommand
  {
    private const string TransactionName = "Electrical.App.Commands.Annotation.CircleAnnotationCommandTrans" ;
    private const string CircleAnnotationName = "Circle Annotation" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var application = commandData.Application ;
        var uiDocument = application.ActiveUIDocument ;
        var document = uiDocument.Document ;

        using var transaction = new Transaction( document ) ;
        transaction.Start( TransactionName ) ;
        string path = AssetManager.GetFamilyPath( CircleAnnotationName ) ;
        FilteredElementCollector notes = new FilteredElementCollector( document ) ;
        notes.OfCategory( BuiltInCategory.OST_GenericAnnotation ).OfClass( typeof( FamilySymbol ) ) ;
        FamilySymbol? circleAnnotation = notes.FirstOrDefault( x => x.Name.Equals( CircleAnnotationName ) ) as FamilySymbol ;
        if ( null == circleAnnotation ) {
          document.LoadFamily( path, out _ ) ;
          circleAnnotation = notes.FirstOrDefault( x => x.Name.Equals( CircleAnnotationName ) ) as FamilySymbol ;
        }

        if ( null != circleAnnotation ) {
          ElementId catId = new ElementId( BuiltInCategory.OST_DetailComponents ) ;
          if ( document.IsDefaultFamilyTypeIdValid( catId, circleAnnotation.Id ) ) {
            document.SetDefaultFamilyTypeId( catId, circleAnnotation.Id ) ;
          }
        }
        else {
          MessageBox.Show( "Can't load circle annotation!", "Info" ) ;
        }

        transaction.Commit() ;

        var textCommandId = RevitCommandId.LookupPostableCommandId( PostableCommand.Symbol ) ;
        if ( application.CanPostCommand( textCommandId ) )
          application.PostCommand( textCommandId ) ;

        return Result.Succeeded ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
  }
}