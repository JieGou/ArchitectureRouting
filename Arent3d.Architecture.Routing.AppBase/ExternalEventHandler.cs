using System ;
using System.Collections.Generic ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public class ExternalEventHandler : IExternalEventHandler
  {
    private IList<Action> _actions ;

    public ExternalEventHandler()
    {
      _actions = new List<Action>() ;
    }
    
    public ExternalEvent? AddAction( Action action )
    {
      _actions.Add(action);
      return ExternalEvent ;
    }

    public void Execute( UIApplication app )
    {
      try {
        foreach ( var action in _actions )
          action() ;
        _actions = new List<Action>() ;
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
      }
    }

    public string GetName()
    {
      return "Arent Inc" ;
    }

    public ExternalEvent? ExternalEvent { get ; set ; }
  }
}