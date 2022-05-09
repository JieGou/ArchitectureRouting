using System ;
using System.Collections.Generic ;
using System.Linq ;
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
        var result = CreateShareParameterInProject( document, ParameterName, categorySet ) ;
        if ( !string.IsNullOrEmpty( result ) ) {
          message = result ;
          return Result.Failed ;
        }
        
        

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    // private List<(string LineStyleName, ElementId LinePatternId)> GetLinePatterns()
    // {
    //   var linePatterns = new List<(string LineStyleName, ElementId LinePatternId)> { ("天井隠蔽配管", LinePatternElement.GetSolidPatternId()) } ;
    //
    //   var linePatternName = "床隠蔽配管" ;
    //   
    //
    //   return linePatterns ;
    // }

    private string CreateShareParameterInProject( Document document, string shareParameterName, CategorySet categorySet )
    {
      using var transaction = new Transaction( document ) ;

      var filter = new FilteredElementCollector( document ) ;
      var shareParameterElement = filter.OfClass( typeof( SharedParameterElement ) ).OfType<SharedParameterElement>().SingleOrDefault( x => x.Name == shareParameterName ) ;
      if ( null != shareParameterElement ) {
        if(document.ParameterBindings.get_Item( shareParameterElement.GetDefinition() ) is not ElementBinding elementBinding)
          return "Not found the element binding!";

        if ( categorySet.OfType<Category>().All( x => elementBinding.Categories.Contains( x ) ) && categorySet.OfType<Category>().Count() == elementBinding.Categories.Size ) 
          return string.Empty ;
        
        transaction.Start( "Edit Parameter" ) ;
        elementBinding.Categories = categorySet ;
        transaction.Commit() ;
      }
      else {
        var definitionFile = document.Application.OpenSharedParameterFile() ;
        if ( null == definitionFile )
          return "Not found the share parameter file!" ;

        transaction.Start( "Create Parameter" ) ;
        const string groupName = "Arent3d Routing" ;
        var definitionGroup = definitionFile.Groups.get_Item( groupName ) ?? definitionFile.Groups.Create( groupName ) ;
        var definition = definitionGroup.Definitions.get_Item( shareParameterName ) ;
        if ( null == definition ) {
          var externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions( shareParameterName, SpecTypeId.String.Text ) ;
          definition = definitionGroup.Definitions.Create( externalDefinitionCreationOptions ) ;
        }
        document.ParameterBindings.Insert( definition, document.Application.Create.NewInstanceBinding( categorySet ), BuiltInParameterGroup.PG_IDENTITY_DATA ) ;
        transaction.Commit() ;
      }
      
      return string.Empty ;
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