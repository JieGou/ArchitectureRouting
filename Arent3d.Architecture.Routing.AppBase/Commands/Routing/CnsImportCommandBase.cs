using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arent3d.Architecture.Routing.AppBase.Forms;
using Arent3d.Architecture.Routing.AppBase.ViewModel;
using Arent3d.Architecture.Routing.Extensions;
using Arent3d.Architecture.Routing.Storable;
using Arent3d.Architecture.Routing.Storable.Model;
using Arent3d.Revit;
using Arent3d.Revit.UI ;
using Arent3d.Revit.UI.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class CnsImportCommandBase : IExternalCommand
  {
    protected UIDocument UiDocument { get ; private set ; } = null! ;

    private CnsImportViewModel _viewModel { get; set; } = null!;
    
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UiDocument = commandData.Application.ActiveUIDocument ;
      Document document = UiDocument.Document ;

      // get data of Cns Category from snoop DB
      CnsImportStorable cnsStorables = document.GetCnsImportStorable() ;
      _viewModel = new CnsImportViewModel( cnsStorables ) ;
      var dialog = new CnsImportDialog(_viewModel) ;

      dialog.ShowDialog() ;
      if ( dialog.DialogResult ?? false ) {
        return document.Transaction( "TransactionName.Commands.Routing.CNSImport", _ =>
        {
          if ( ShouldSaveCnsList( document, cnsStorables ) ) {
            var tokenSource = new CancellationTokenSource() ;
            using var progress = ProgressBar.ShowWithNewThread( tokenSource ) ;
            progress.Message = "Importing CNS..." ;
            using ( progress?.Reserve( 0.5 ) ) {
              SaveCnsList( document, cnsStorables ) ;
            }
          }
          return Result.Succeeded ;
        }) ;
      }
      else {
        return Result.Cancelled ;
      }
    }
    
    private static void SaveCnsList( Document document, CnsImportStorable list )
    {
      list.Save() ;
    }
    
    private static bool ShouldSaveCnsList( Document document, CnsImportStorable newSettings )
    {
      // var old = document.GetAllStorables<CnsImportStorable>().FirstOrDefault() ; // generates new instance from document
      // return ( false == newSettings.Equals( old ) ) ;
      return true;
    }

  }
}