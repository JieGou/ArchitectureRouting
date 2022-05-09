using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class ShareParameterHelper
  {
    public static string CreateShareParameterInProject( Document document, string shareParameterName, CategorySet categorySet )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Edit Parameter" ) ;
      
      var filter = new FilteredElementCollector( document ) ;
      var shareParameterElement = filter.OfClass( typeof( SharedParameterElement ) ).OfType<SharedParameterElement>().SingleOrDefault( x => x.Name == shareParameterName ) ;
      if ( null != shareParameterElement ) {
        if(document.ParameterBindings.get_Item( shareParameterElement.GetDefinition() ) is not ElementBinding elementBinding)
          return "Not found the element binding!";

        if ( categorySet.OfType<Category>().All( x => elementBinding.Categories.Contains( x ) ) && categorySet.OfType<Category>().Count() == elementBinding.Categories.Size ) 
          return string.Empty ;

        elementBinding.Categories = categorySet ;
      }
      else {
        var definitionFile = document.Application.OpenSharedParameterFile() ;
        if ( null == definitionFile )
          return "Not found the share parameter file!" ;

        const string groupName = "Arent3d Routing" ;
        var definitionGroup = definitionFile.Groups.get_Item( groupName ) ?? definitionFile.Groups.Create( groupName ) ;
        var definition = definitionGroup.Definitions.get_Item( shareParameterName ) ;
        if ( null == definition ) {
          var externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions( shareParameterName, SpecTypeId.String.Text ) ;
          definition = definitionGroup.Definitions.Create( externalDefinitionCreationOptions ) ;
        }
        document.ParameterBindings.Insert( definition, document.Application.Create.NewInstanceBinding( categorySet ), BuiltInParameterGroup.PG_IDENTITY_DATA ) ;
        
      }
      
      transaction.Commit() ;
      
      return string.Empty ;
    }
  }
}