using System ;
using System.IO ;
using System.Linq ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class ShareParameterHelper
  {
    public static (string Notify, Guid ParameterGuid) FindOrCreateShareParameter( Document document, string shareParameterName, CategorySet categorySet )
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Edit Parameter" ) ;

      Guid parameterGuid ;

      var shareParameterElement = new FilteredElementCollector( document ).OfClass( typeof( SharedParameterElement ) ).OfType<SharedParameterElement>().SingleOrDefault( x => x.Name == shareParameterName ) ;
      if ( null != shareParameterElement ) {
        if ( document.ParameterBindings.get_Item( shareParameterElement.GetDefinition() ) is not ElementBinding elementBinding ) {
          document.Delete( shareParameterElement.Id ) ;
          parameterGuid = CreateShareParameter( document, shareParameterName, categorySet ) ;
        }
        else {
          parameterGuid = shareParameterElement.GuidValue ;
          if ( categorySet.OfType<Category>().All( x => elementBinding.Categories.Contains( x ) ) && categorySet.OfType<Category>().Count() == elementBinding.Categories.Size )
            return ( string.Empty, parameterGuid ) ;

          elementBinding.Categories = categorySet ;
        }
      }
      else {
        parameterGuid = CreateShareParameter( document, shareParameterName, categorySet ) ;
      }

      transaction.Commit() ;

      return ( string.Empty, parameterGuid ) ;
    }

    private static Guid CreateShareParameter( Document document, string shareParameterName, CategorySet categorySet )
    {
      var definitionFile = document.Application.OpenSharedParameterFile() ;
      if ( null == definitionFile ) {
        var tempFile = Path.Combine( Path.GetTempPath(), "Arent Share Parameter.txt" ) ;
        if ( File.Exists( tempFile ) )
          File.Delete( tempFile ) ;

        using var fileStream = File.Create( tempFile ) ;
        fileStream.Close() ;

        document.Application.SharedParametersFilename = tempFile ;
        definitionFile = document.Application.OpenSharedParameterFile() ;
      }

      const string groupName = "Electrical" ;
      var definitionGroup = definitionFile.Groups.get_Item( groupName ) ?? definitionFile.Groups.Create( groupName ) ;
      if ( definitionGroup.Definitions.get_Item( shareParameterName ) is not ExternalDefinition externalDefinition ) {
#if (REVIT2020 || REVIT2019 || REVIT2021)
        var externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions( shareParameterName, ParameterType.Text ) ;
#else
        var externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions( shareParameterName, SpecTypeId.String.Text ) ;
#endif
        externalDefinition = ( definitionGroup.Definitions.Create( externalDefinitionCreationOptions ) as ExternalDefinition )! ;
      }

      document.ParameterBindings.Insert( externalDefinition, document.Application.Create.NewInstanceBinding( categorySet ), BuiltInParameterGroup.PG_IDENTITY_DATA ) ;
      return externalDefinition.GUID ;
    }
  }
}