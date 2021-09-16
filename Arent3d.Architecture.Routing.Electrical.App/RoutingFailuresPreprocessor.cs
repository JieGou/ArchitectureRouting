using Arent3d.Architecture.Routing.AppBase ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App
{
  public class RoutingFailuresPreprocessor : RoutingFailuresPreprocessorBase
  {
    public RoutingFailuresPreprocessor( RoutingExecutor executor ) : base( executor )
    {
    }

    protected override bool PreprocessFailureMessage( FailureMessageHandler handler )
    {
      if ( handler.FailureDefinitionId == BuiltInFailures.OverlapFailures.DuplicateInstances ) {
        handler.DeleteWarning() ;
        return true ;
      }

      return false ;
    }
  }
}