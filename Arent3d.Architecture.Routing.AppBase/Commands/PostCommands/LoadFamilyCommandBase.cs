using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.PostCommands
{
  public class LoadFamilyCommandParameter
  {
    public string FilePath { get ; }
    public string FamilyFileName { get ; }
    public bool IsLoaded { get ; set ; }

    public LoadFamilyCommandParameter( string filePath, string familyFileName )
    {
      FilePath = filePath ;
      FamilyFileName = familyFileName ;
      IsLoaded = false ;
    }
  }
  
  public class LoadFamilyCommandBase : RoutingExternalAppCommandBaseWithParam<List<LoadFamilyCommandParameter>>
  {
    protected override string GetTransactionName() => "TransactionName.Commands.PostCommands.CreateSymbolContentTagCommand".GetAppStringByKeyOrDefault( "Load connector family" ) ;

    protected override ExecutionResult Execute( List<LoadFamilyCommandParameter> familyParameters, Document document, TransactionWrapper transaction )
    {
      try {
        foreach ( var param in familyParameters ) {
          if ( ! string.IsNullOrEmpty( param.FamilyFileName ) ) {
            var imagePath = ConnectorFamilyManager.GetFolderPath() ;
            if ( ! Directory.Exists( imagePath ) ) Directory.CreateDirectory( imagePath ) ;
            var connectorFamilyName = param.FamilyFileName.Replace( ".rfa", "" ) ;
            if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == connectorFamilyName ) is Family ) {
              var confirmMessage = MessageBox.Show( $"モデル{connectorFamilyName}がすでに存在していますが、上書きしますか。", "Message", MessageBoxButtons.OKCancel ) ;
              if ( confirmMessage == DialogResult.Cancel ) {
                param.IsLoaded = true ;
                continue ;
              }

              document.LoadFamily( param.FilePath, new CeedViewModel.FamilyOption( true ), out var overwriteFamily ) ;
              if ( overwriteFamily == null ) {
                param.IsLoaded = true ;
                continue ;
              }
              foreach ( ElementId familySymbolId in overwriteFamily.GetFamilySymbolIds() )
                document.GetElementById<FamilySymbol>( familySymbolId ) ;
              param.IsLoaded = true ;
              continue ;
            }
          }

          document.LoadFamily( param.FilePath, new CeedViewModel.FamilyOption( true ), out var newFamily ) ;
          if ( newFamily == null ) {
            param.IsLoaded = false ;
            continue ;
          }
          foreach ( ElementId familySymbolId in newFamily.GetFamilySymbolIds() )
            document.GetElementById<FamilySymbol>( familySymbolId ) ;
          param.IsLoaded = true ;
        }

        return ExecutionResult.Succeeded ;
      }
      catch {
        return ExecutionResult.Cancelled ;
      }
    }
  }
}