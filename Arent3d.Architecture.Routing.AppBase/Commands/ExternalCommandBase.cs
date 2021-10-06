using System ;
using System.Threading ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Arent3d.Utility ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands
{
  public abstract class ExternalCommandBase<TUIResult> : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      TUIResult? uiResult = default ;
      try {
        uiResult = OperateUI( commandData, ref message, elements ) ;
      }
      catch ( OperationCanceledException ) {
        ( uiResult as IDisposable )?.Dispose() ;
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        OnException( e, uiResult ) ;
        ( uiResult as IDisposable )?.Dispose() ;
        return Result.Failed ;
      }

      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction( GetTransactionName(), transaction => Execute( document, transaction, uiResult ) ) ;
      }
      catch ( Exception e ) {
        OnException( e, uiResult ) ;
        ( uiResult as IDisposable )?.Dispose() ;
        return Result.Failed ;
      }
    }

    protected virtual void OnException( Exception e, TUIResult? uiResult )
    {
      CommandUtils.DebugAlertException( e ) ;
    }

    protected IProgressData ShowProgressBar( string progressMessage )
    {
      var tokenSource = new CancellationTokenSource() ;
      var progress = ProgressBar.ShowWithNewThread( tokenSource ) ;
      progress.Message = progressMessage ;
      return progress ;
    }

    protected abstract Result Execute( Document document, Transaction transaction, TUIResult result ) ;

    protected abstract string GetTransactionName() ;

    protected abstract TUIResult OperateUI( ExternalCommandData commandData, ref string message, ElementSet elements ) ;
  }

  public abstract class ExternalCommandBase : ExternalCommandBase<object?>
  {
    protected sealed override Result Execute( Document document, Transaction transaction, object? result ) => Execute( document, transaction ) ;

    protected abstract Result Execute( Document document, Transaction transaction ) ;

    protected sealed override object? OperateUI( ExternalCommandData commandData, ref string message, ElementSet elements ) => null ;
  }
}