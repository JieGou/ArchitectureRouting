using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ChangeFamilyGradeCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      try {
        var document = commandData.Application.ActiveUIDocument.Document ;
        var instances = new FilteredElementCollector( document ).OfClass( typeof( FamilyInstance ) ).Cast<FamilyInstance>().Where( a => a.HasParameter( DefaultSettingCommandBase.Grade3FieldName ) ).ToList() ;
        // select mode
        var isInGrade3Mode = instances.Any( item => item.GetPropertyBool( DefaultSettingCommandBase.Grade3FieldName ) ) ;
        var dialog = new ChangeFamilyGradeDialog( commandData.Application, isInGrade3Mode ) ;
        dialog.ShowDialog() ;
        if ( dialog.DialogResult == false ) return Result.Cancelled ;
        isInGrade3Mode = dialog.SelectedMode == GradeMode.GradeFrom3To7 ;

        // update property グレード3 of instances
        using Transaction t = new(document, "Update grade") ;
        t.Start() ;
        foreach ( var instance in instances )
          instance.SetProperty( DefaultSettingCommandBase.Grade3FieldName, isInGrade3Mode ) ;
        t.Commit() ;

        return Result.Succeeded ;
      }
      catch ( OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }
  }
}