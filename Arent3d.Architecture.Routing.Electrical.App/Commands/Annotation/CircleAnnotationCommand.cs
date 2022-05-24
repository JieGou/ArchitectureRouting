using System ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Annotation
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Annotation.CircleAnnotationCommand", DefaultString = "Circle \nText Box" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
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

        string? path = GetCircleAnnotationPath() ;
        if ( string.IsNullOrEmpty( path ) )
          return Result.Failed ;

        FilteredElementCollector notes = new FilteredElementCollector( document ) ;
        notes.OfCategory( BuiltInCategory.OST_GenericAnnotation ).OfClass( typeof( FamilySymbol ) ) ;
        FamilySymbol? circleAnnotation = notes.FirstOrDefault( x => x.Name.Equals( CircleAnnotationName ) ) as FamilySymbol ;
        if ( null == circleAnnotation ) {
          var transaction = new Transaction( document ) ;
          transaction.Start( TransactionName ) ;
          document.LoadFamily( path, out _ ) ;
          circleAnnotation = notes.FirstOrDefault( x => x.Name.Equals( CircleAnnotationName ) ) as FamilySymbol ;
          transaction.Commit() ;
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

    private string? GetFamilyPath( Assembly assembly, string familyName )
    {
      var resourceFullName = assembly.GetManifestResourceNames().FirstOrDefault( element => element.EndsWith( familyName ) ) ;
      if ( string.IsNullOrEmpty( resourceFullName ) )
        return null ;

      using var stream = assembly.GetManifestResourceStream( resourceFullName ) ;
      if ( null == stream )
        return null ;

      var fileData = new byte[ stream.Length ] ;
      var read = stream.Read( fileData, 0, fileData.Length ) ;

      var pathFamily = Path.Combine( Path.GetTempPath(), familyName ) ;
      File.WriteAllBytes( pathFamily, fileData ) ;

      return pathFamily ;
    }

    private string? GetCircleAnnotationPath()
    {
      Type t = typeof( Arent3d.Architecture.Routing.AppInfo ) ;
      var assembly = t.Assembly ;

      return GetFamilyPath( assembly, CircleAnnotationName + ".rfa" ) ;
    }
  }
}