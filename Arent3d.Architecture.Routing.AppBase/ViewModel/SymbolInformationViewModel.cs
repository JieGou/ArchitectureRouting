using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public enum SymbolKindEnum
  {
    Start,
    Rectangle,
    Triangle,
  }

  public enum SymbolCoordinateEnum
  {
    Center,
    Left,
    Right,
  }

  public class SymbolInformationViewModel : NotifyPropertyChanged
  {
    private Document? _document ;

    public SymbolInformationModel? SymbolInformation { get ; set ; }
    public readonly Array SymbolKinds = Enum.GetValues( typeof( SymbolKindEnum ) ) ;
    public readonly Array SymbolCoordinates = Enum.GetValues( typeof( SymbolCoordinateEnum ) ) ;

    private CsvStorable? _csvStorable ;
    public CsvStorable CsvStorable => _csvStorable ??= _document!.GetCsvStorable() ;

    private IEnumerable<CeedModel>? _ceedModels ;
    public IEnumerable<CeedModel> CeedModels => _ceedModels ??= _document!.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>() ;

    private List<HiroiSetMasterModel>? _hiroiSetMasterNormalModels ;
    public List<HiroiSetMasterModel> HiroiSetMasterNormalModels => _hiroiSetMasterNormalModels ??= CsvStorable.HiroiSetMasterNormalModelData ;

    private IEnumerable<HiroiMasterModel>? _hiroiMasterModels ;
    public IEnumerable<HiroiMasterModel> HiroiMasterModels => _hiroiMasterModels ??= CsvStorable.HiroiMasterModelData ;

    private List<HiroiSetCdMasterModel>? _hiroiSetCdMasterModels ;
    public List<HiroiSetCdMasterModel> HiroiSetCdMasterModels => _hiroiSetCdMasterModels ??= CsvStorable.HiroiSetCdMasterNormalModelData ;

    private CeedDetailInformationModel? _ceedDetailInformationModel ;

    public CeedDetailInformationModel CeedDetailInformationModel
    {
      get { return _ceedDetailInformationModel ??= new CeedDetailInformationModel( new ObservableCollection<QueryData>(), string.Empty ) ; }
      set
      {
        _ceedDetailInformationModel = value ;
        OnPropertyChanged() ;
      }
    }

    private string? _setCode ; 
    public string SetCode
    {
      get => _setCode ??= string.Empty ;
      set
      {
        _setCode = value.Trim() ?? string.Empty ;
        ConstructionClassificationSelected = HiroiSetCdMasterModels.Find( x => x.SetCode == _setCode )?.ConstructionClassification ?? string.Empty ;
        //LoadData() ;
        OnPropertyChanged() ;
      }
    }

    private ObservableCollection<string>? _constructionClassifications ;

    public ObservableCollection<string> ConstructionClassifications
    {
      get
      {
        if ( null != _constructionClassifications )
          return _constructionClassifications ;

        var values = HiroiSetCdMasterModels.Select( x => x.ConstructionClassification ).Distinct() ;
        _constructionClassifications = new ObservableCollection<string>( values ) ;

        return _constructionClassifications ;
      }
      set
      {
        _constructionClassifications = value ;
        OnPropertyChanged() ;
      }
    }

    private string? _constructionClassificationSelected ;

    public string ConstructionClassificationSelected
    {
      get { return _constructionClassificationSelected ??= HiroiSetCdMasterModels.Find( x => x.SetCode == SetCode )?.ConstructionClassification ?? string.Empty ; }
      set
      {
        var result = HiroiSetCdMasterModels.Find( x => x.SetCode == SetCode.ToUpper() ) ;
        if ( null == result )
          return ;

        if ( result.ConstructionClassification.Equals( value ) )
          return ;

        result.ConstructionClassification = value ;
        CsvStorable.HiroiSetCdMasterNormalModelData = HiroiSetCdMasterModels ;

        _constructionClassificationSelected = result.ConstructionClassification ;
        OnPropertyChanged() ;

        try {
          using var transaction = new Transaction( _document, "Update Construction Classification" ) ;
          transaction.Start() ;
          CsvStorable.Save() ;
          transaction.Commit() ;
        }
        catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
          MessageBox.Show( "Failed to update the construction classification.", "Error Message" ) ;
        }
      }
    }

    public SymbolInformationViewModel()
    {
    }

    public SymbolInformationViewModel( Document? document, string? setCode = null )
    {
      _document = document ;
      SetCode = setCode ?? string.Empty ;
    }
    
    public SymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel )
    {
      _document = document ;
      SymbolInformation = symbolInformationModel ?? new SymbolInformationModel(null, null, null  ) ;
    }
  }
}