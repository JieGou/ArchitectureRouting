using System ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  /// <summary>
  /// Register FromToTree
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  [Regeneration( RegenerationOption.Manual )]
  public abstract class RegisterFromToTreeCommandBase : IExternalCommand
  {
    protected RegisterFromToTreeCommandBase( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor )
    {
      CreateFromToTreeUiManager( application, dpId, postCommandExecutor ) ;
    }

    /// <summary>
    /// Executes the specIfied command Data
    /// </summary>
    /// <param name="commandData"></param>
    /// <param name="message"></param>
    /// <param name="elements"></param>
    /// <returns></returns>
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      return  Initialize( commandData.Application ) ;
    }

    // view activated event

    public abstract Result Initialize( UIApplication uiApplication ) ;

    protected abstract void CreateFromToTreeUiManager( UIControlledApplication application, Guid dpId, IPostCommandExecutorBase postCommandExecutor ) ;
  }
}