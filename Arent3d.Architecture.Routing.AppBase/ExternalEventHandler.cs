using System ;
using System.Collections.Generic ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase
{
  public class ExternalEventHandler : IExternalEventHandler
  {
    private IList<Action>? _actions ;

    public IList<Action> Actions
    {
      get { return _actions ??= new List<Action>() ; }
      set => _actions = value ;
    }

    public ExternalEvent? AddAction( Action action )
    {
      Actions.Add(action);
      return ExternalEvent ;
    }

    public void Execute( UIApplication app )
    {
      try {
        foreach ( var action in Actions )
          action() ;
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