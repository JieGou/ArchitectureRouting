using System ;
using Arent3d.Architecture.Routing.Electrical.App.Helpers ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  public class ChangeWireTypeCommand : IExternalCommand
  {
    public const string ParameterName = "Location Type" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      try {
        var document = commandData.Application.ActiveUIDocument.Document ;

        var categorySet = CreateCategorySet( document ) ;
        var result = ShareParameterHelper.CreateShareParameterInProject( document, ParameterName, categorySet ) ;
        if ( !string.IsNullOrEmpty( result ) ) {
          message = result ;
          return Result.Failed ;
        }

        var linePatternElements = PatternElementHelper.GetLinePatterns( document ) ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }
    
    private CategorySet CreateCategorySet( Document document )
    {
      var categorySet = new CategorySet() ;
      categorySet.Insert( Category.GetCategory( document, BuiltInCategory.OST_Conduit ) ) ;
      categorySet.Insert( Category.GetCategory( document, BuiltInCategory.OST_ConduitFitting ) ) ;
      return categorySet ;
    }
  }
}