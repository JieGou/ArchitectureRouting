using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Controls ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using MessageBox = System.Windows.Forms.MessageBox ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class CeedDetailInformationViewModel : NotifyPropertyChanged
  {
    
    private readonly Document _document ;

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

    private IEnumerable<CeedModel>? _ceedModels ;
    public IEnumerable<CeedModel> CeedModels => _ceedModels ??= _document.GetAllStorables<CeedStorable>().FirstOrDefault()?.CeedModelData ?? new List<CeedModel>() ;

    private CsvStorable? _csvStorable ;
    public CsvStorable CsvStorable => _csvStorable ??= _document.GetCsvStorable() ;

    private List<HiroiSetMasterModel>? _hiroiSetMasterNormalModels ;
    public List<HiroiSetMasterModel> HiroiSetMasterNormalModels => _hiroiSetMasterNormalModels ??= CsvStorable.HiroiSetMasterNormalModelData ;

    private IEnumerable<HiroiMasterModel>? _hiroiMasterModels ;
    public IEnumerable<HiroiMasterModel> HiroiMasterModels => _hiroiMasterModels ??= CsvStorable.HiroiMasterModelData ;

    private List<HiroiSetCdMasterModel>? _hiroiSetCdMasterModels ;
    public List<HiroiSetCdMasterModel> HiroiSetCdMasterModels => _hiroiSetCdMasterModels ??= CsvStorable.HiroiSetCdMasterNormalModelData ;

    private string? _setCode ;
    public string SetCode
    {
      get => _setCode ??= string.Empty ;
      set
      {
        _setCode = value.Trim() ?? string.Empty ;
        ConstructionClassificationSelected = HiroiSetCdMasterModels.Find( x => x.SetCode == _setCode )?.ConstructionClassification ?? string.Empty ;
        ModelNumber = string.Empty ;
        DeviceSymbol = string.Empty ;
        if ( ! string.IsNullOrEmpty( _setCode ) ) {
          var ceedModel = CeedModels.FirstOrDefault( model => string.Equals( model.CeedSetCode, SetCode, StringComparison.InvariantCultureIgnoreCase ) ) ;
          if ( ceedModel != null ) {
            ModelNumber = ceedModel.ModelNumber ;
            DeviceSymbol = ceedModel.GeneralDisplayDeviceSymbol ;
          }
        }
        
        LoadData() ;
        OnPropertyChanged() ;
      }
    }
    
    private string? _modelNumber ;
    public string ModelNumber
    {
      get => _modelNumber ??= string.Empty ;
      set
      {
        _modelNumber = value.Trim() ;
        OnPropertyChanged() ;
      }
    }
    
    private string? _deviceSymbol ;
    public string DeviceSymbol
    {
      get => _deviceSymbol ??= string.Empty ;
      set
      {
        _deviceSymbol = value.Trim() ;
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

        if ( result.ConstructionClassification.Equals(value) )
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

    public bool DialogResult { get ; set ; }

    public CeedDetailInformationViewModel( Document document, string ceedSetCode, string deviceSymbol, string modelNumber )
    {
      _document = document ;
      SetCode = ceedSetCode ;
      DeviceSymbol = deviceSymbol ;
      ModelNumber = modelNumber ;
    }

    #region Commands

    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          DialogResult = true ;
          wd?.Close() ;
        } ) ;
      }
    }

    public ICommand ResetCommand
    {
      get { return new RelayCommand<Window>( wd => null != wd, wd => { wd?.Close() ; } ) ; }
    }

    public ICommand DeleteRowCommand
    {
      get
      {
        return new RelayCommand<DataGrid>( dg => null != dg, dg =>
        {
          if ( dg.SelectedItem is not QueryData queryData )
            return ;

          var hiroiSetMasterModel = HiroiSetMasterNormalModels.Find( x => x.ParentPartModelNumber.Equals( queryData.ParentPartModelNumber ) && x.ParentPartModelNumber.Equals( queryData.ParentPartModelNumber ) ) ;
          switch ( queryData.MaterialIndex ) {
            case 1 :
              hiroiSetMasterModel.MaterialCode1 = string.Empty ;
              hiroiSetMasterModel.Quantity1 = string.Empty ;
              hiroiSetMasterModel.Name1 = string.Empty ;
              break ;
            case 2 :
              hiroiSetMasterModel.MaterialCode2 = string.Empty ;
              hiroiSetMasterModel.Quantity2 = string.Empty ;
              hiroiSetMasterModel.Name2 = string.Empty ;
              break ;
            case 3 :
              hiroiSetMasterModel.MaterialCode3 = string.Empty ;
              hiroiSetMasterModel.Quantity3 = string.Empty ;
              hiroiSetMasterModel.Name3 = string.Empty ;
              break ;
            case 4 :
              hiroiSetMasterModel.MaterialCode4 = string.Empty ;
              hiroiSetMasterModel.Quantity4 = string.Empty ;
              hiroiSetMasterModel.Name4 = string.Empty ;
              break ;
            case 5 :
              hiroiSetMasterModel.MaterialCode5 = string.Empty ;
              hiroiSetMasterModel.Quantity5 = string.Empty ;
              hiroiSetMasterModel.Name5 = string.Empty ;
              break ;
            case 6 :
              hiroiSetMasterModel.MaterialCode6 = string.Empty ;
              hiroiSetMasterModel.Quantity6 = string.Empty ;
              hiroiSetMasterModel.Name6 = string.Empty ;
              break ;
            case 7 :
              hiroiSetMasterModel.MaterialCode7 = string.Empty ;
              hiroiSetMasterModel.Quantity7 = string.Empty ;
              hiroiSetMasterModel.Name7 = string.Empty ;
              break ;
            case 8 :
              hiroiSetMasterModel.MaterialCode8 = string.Empty ;
              hiroiSetMasterModel.Quantity8 = string.Empty ;
              hiroiSetMasterModel.Name8 = string.Empty ;
              break ;
          }

          if ( string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode1 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode2 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode3 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode4 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode5 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode6 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode7 ) && string.IsNullOrWhiteSpace( hiroiSetMasterModel.MaterialCode8 ) ) {
            HiroiSetMasterNormalModels.Remove( hiroiSetMasterModel ) ;
          }

          CsvStorable.HiroiSetMasterNormalModelData = HiroiSetMasterNormalModels ;

          try {
            using var transaction = new Transaction( _document, "Delete Data" ) ;
            transaction.Start() ;
            CsvStorable.Save() ;
            transaction.Commit() ;

            LoadData() ;
          }
          catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
            MessageBox.Show( "Delete Data Failed.", "Error Message" ) ;
          }
        } ) ;
      }
    }

    #endregion

    #region Methods

    private void BuildQueryData( string materialCode, string quantity, string parentPartModelNumber, int materialIndex, ref ObservableCollection<QueryData> queryData )
    {
      if ( string.IsNullOrWhiteSpace( materialCode ) )
        return ;

      if ( ! int.TryParse( materialCode, out var value ) )
        return ;

      materialCode = value.ToString( "D6" ) ;
      var hiroiMasterItem = HiroiMasterModels.FirstOrDefault( x => x.Buzaicd == materialCode ) ;
      var name = hiroiMasterItem != null ? hiroiMasterItem.Hinmei.Trim() : string.Empty ;
      var standard = hiroiMasterItem != null ? hiroiMasterItem.Kikaku.Trim() : string.Empty ;

      queryData.Add( new QueryData( materialCode, name, standard, quantity, parentPartModelNumber, materialIndex ) ) ;
    }

    private void LoadData()
    {
      var queryData = new ObservableCollection<QueryData>() ;
      if ( ! string.IsNullOrWhiteSpace( SetCode ) ) {
        var ceedModel = CeedModels.FirstOrDefault( model => string.Equals( model.CeedSetCode, SetCode, StringComparison.InvariantCultureIgnoreCase ) ) ;
        if ( ceedModel != null ) {
          var hiroiSetMasterNormals = HiroiSetMasterNormalModels.Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeedModelNumber ) ) ;
          foreach ( var hiroiSetMasterNormal in hiroiSetMasterNormals ) {
            BuildQueryData( hiroiSetMasterNormal.MaterialCode1, hiroiSetMasterNormal.Quantity1, hiroiSetMasterNormal.ParentPartModelNumber, 1, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode2, hiroiSetMasterNormal.Quantity2, hiroiSetMasterNormal.ParentPartModelNumber, 2, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode3, hiroiSetMasterNormal.Quantity3, hiroiSetMasterNormal.ParentPartModelNumber, 3, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode4, hiroiSetMasterNormal.Quantity4, hiroiSetMasterNormal.ParentPartModelNumber, 4, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode5, hiroiSetMasterNormal.Quantity5, hiroiSetMasterNormal.ParentPartModelNumber, 5, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode6, hiroiSetMasterNormal.Quantity6, hiroiSetMasterNormal.ParentPartModelNumber, 6, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode7, hiroiSetMasterNormal.Quantity7, hiroiSetMasterNormal.ParentPartModelNumber, 7, ref queryData ) ;
            BuildQueryData( hiroiSetMasterNormal.MaterialCode8, hiroiSetMasterNormal.Quantity8, hiroiSetMasterNormal.ParentPartModelNumber, 8, ref queryData ) ;
          }
        }
      }

      CeedDetailInformationModel = new CeedDetailInformationModel( queryData, string.Empty ) ;
    }

    #endregion
    
  }
}