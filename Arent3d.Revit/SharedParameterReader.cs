using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Utility ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public class SharedParameterReader : IDisposable
  {
    public static IReadOnlyCollection<Definition> GetSharedParameters( Application application, string sharedParameterFilename )
    {
      using var reader = new SharedParameterReader( application, sharedParameterFilename ) ;
      return reader.ParameterDefinitionGroups.SelectMany( group => group.Definitions ).EnumerateAll() ;
    }
    
    private readonly Application _app ;
    private readonly string _orgSharedParametersFilename ;

    private DefinitionGroups ParameterDefinitionGroups => _app.OpenSharedParameterFile().Groups ;

    private SharedParameterReader( Application application, string sharedParameterFilename )
    {
      _app = application ;
      _orgSharedParametersFilename = _app.SharedParametersFilename ;
      _app.SharedParametersFilename = sharedParameterFilename ;
    }

    public void Dispose()
    {
      GC.SuppressFinalize( this ) ;

      RevertSharedParametersFilename() ;
    }

    ~SharedParameterReader()
    {
      RevertSharedParametersFilename() ;
    }

    private void RevertSharedParametersFilename()
    {
      _app.SharedParametersFilename = _orgSharedParametersFilename ;
    }
  }
}