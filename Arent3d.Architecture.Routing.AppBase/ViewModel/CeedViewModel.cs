using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedViewModel : ViewModelBase
  {
    public List<CeedModel> CeedModels { get ; }
    public CeedStorable CeedStorable { get ; }
    public readonly List<string> CeedModelNumbers = new List<string>() ;
    public readonly List<string> ModelNumbers = new List<string>() ;

    public CeedViewModel( CeedStorable ceedStorable )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedStorable.CeedModelData ;
      AddModelNumber( CeedModels ) ;
    }

    public CeedViewModel( CeedStorable ceedStorable, List<CeedModel> ceedModels )
    {
      CeedStorable = ceedStorable ;
      CeedModels = ceedModels ;
      AddModelNumber( ceedModels ) ;
    }

    private void AddModelNumber( List<CeedModel> ceedModels )
    {
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.CeedModelNumber ) ) ) {
        if ( ! CeedModelNumbers.Contains( ceedModel.CeedModelNumber ) ) CeedModelNumbers.Add( ceedModel.CeedModelNumber ) ;
      }
      foreach ( var ceedModel in ceedModels.Where( ceedModel => ! string.IsNullOrEmpty( ceedModel.ModelNumber ) ) ) {
        var modelNumbers = ceedModel.ModelNumber.Split( '\n' ) ;
        foreach ( var modelNumber in modelNumbers ) {
          if ( ! ModelNumbers.Contains( modelNumber ) ) ModelNumbers.Add( modelNumber ) ;
        }
      }
    }

    public static List<string> LoadConnectorFamily( Document document, List<string> connectorFamilyPaths )
    {
      List<string> connectorFamilyFiles = new() ;

      var existsConnectorFamilies = CheckExistsConnectorFamilies( document, connectorFamilyPaths ) ;
      if ( existsConnectorFamilies.Any() ) {
        var confirmMessage = MessageBox.Show( "モデルがすでに存在していますが、上書きしますか。", "Confirm Message", MessageBoxButton.OKCancel ) ;
        if ( confirmMessage == MessageBoxResult.Cancel ) {
          connectorFamilyPaths = connectorFamilyPaths.Where( p => ! existsConnectorFamilies.ContainsValue( p ) ).ToList() ;
          connectorFamilyFiles.AddRange( existsConnectorFamilies.Values ) ;
        }
      }
      
      using Transaction loadTransaction = new ( document, "Load connector's family" ) ;
      loadTransaction.Start() ;
      foreach ( var connectorFamilyPath in connectorFamilyPaths ) {
        var connectorFamilyFile = Path.GetFileName( connectorFamilyPath ) ;
        var connectorFamily = LoadFamily( document, connectorFamilyPath ) ;
        if ( connectorFamily != null ) connectorFamilyFiles.Add( connectorFamilyFile ) ;
      }

      loadTransaction.Commit() ;
      return connectorFamilyFiles ;
    }

    private static Family? LoadFamily( Document document, string filePath )
    {
      try {
        document.LoadFamily( filePath, new FamilyOption( true ), out var family ) ;
        if ( family == null ) return family ;
        foreach ( ElementId familySymbolId in family.GetFamilySymbolIds() ) {
          document.GetElementById<FamilySymbol>( familySymbolId ) ;
        }

        return family ;
      }
      catch {
        return null ;
      }
    }

    private static Dictionary<string, string> CheckExistsConnectorFamilies( Document document, IEnumerable<string> connectorFamilyPaths )
    {
      Dictionary<string, string> existsConnectorFamilies = new() ;
      foreach ( var connectorFamilyPath in connectorFamilyPaths ) {
        var connectorFamilyFile = Path.GetFileName( connectorFamilyPath ) ;
        var connectorFamilyName = connectorFamilyFile.Replace( ".rfa", "" ) ;
        if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == connectorFamilyName ) is Family family ) {
          existsConnectorFamilies.Add( family.UniqueId, connectorFamilyPath ) ;
        }
      }

      return existsConnectorFamilies ;
    }
    
    private class FamilyOption : IFamilyLoadOptions
    {
      private readonly bool _forceUpdate ;

      public FamilyOption( bool forceUpdate ) => _forceUpdate = forceUpdate ;

      public bool OnFamilyFound( bool familyInUse, out bool overwriteParameterValues )
      {
        if ( familyInUse && ! _forceUpdate ) {
          overwriteParameterValues = false ;
          return false ;
        }

        overwriteParameterValues = true ;
        return true ;
      }

      public bool OnSharedFamilyFound( Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues )
      {
        source = FamilySource.Project ;
        return OnFamilyFound( familyInUse, out overwriteParameterValues ) ;
      }
    }
  }
}