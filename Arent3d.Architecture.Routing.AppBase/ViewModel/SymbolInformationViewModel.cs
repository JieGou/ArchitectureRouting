using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.ComponentModel ;
using System.Globalization ;
using System.Linq ;
using System.Windows.Data ;
using System.Windows.Forms ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class SymbolInformationViewModel : NotifyPropertyChanged
  {
    private const double QuantityDefault = 100 ;
    private const string UnitDefault = "m" ;
    private const string TrajectoryDefault = "100" ;
     
    private Document? _document ; 

    public ICommand AddCeedDetailCommand => new RelayCommand( AddCeedDetail ) ;
    public ICommand DeleteCeedDetailCommand => new RelayCommand( DeleteCeedDetail ) ;

    public SymbolInformationModel SymbolInformation { get ; set ; }

    #region SymbolSetting

    public readonly Array SymbolKinds = Enum.GetValues( typeof( SymbolKindEnum ) ) ;
    public readonly Array SymbolCoordinates = Enum.GetValues( typeof( SymbolCoordinateEnum ) ) ; 
    public readonly Array SymbolColors = SymbolColor.DictSymbolColor.Keys.ToArray() ;
     
    public SymbolKindEnum SelectedSymbolKind
    {
      get => (SymbolKindEnum)Enum.Parse( typeof( SymbolKindEnum ), SymbolInformation.SymbolKind! ) ;
      set => SymbolInformation.SymbolKind = value.GetFieldName() ;
    }

    public SymbolCoordinateEnum SelectedSymbolCoordinate
    {
      get => (SymbolCoordinateEnum)Enum.Parse( typeof( SymbolCoordinateEnum ), SymbolInformation.SymbolCoordinate! ) ;
      set => SymbolInformation.SymbolCoordinate = value.GetFieldName() ;
    }

    #endregion

    #region CeedDetail Setting

    private CsvStorable? _csvStorable ;
    public CsvStorable CsvStorable => _csvStorable ??= _document!.GetCsvStorable() ;

    private List<HiroiMasterModel>? _hiroiMasterModels ;
    public List<HiroiMasterModel> HiroiMasterModels => _hiroiMasterModels ??= CsvStorable.HiroiMasterModelData ;

    private ObservableCollection<CeedDetailModel> _ceedDetailList = new() ;
    public CeedDetailModel? CeedDetailSelected { get ; set ; }

    public ObservableCollection<CeedDetailModel> CeedDetailList
    {
      get => _ceedDetailList ;
      set
      {
        _ceedDetailList = value ;
        OnPropertyChanged( "CeedDetailList" ) ;
      }
    }

    public ObservableCollection<string> ConstructionClassificationTypeList { get ; set ; }

    #endregion
 
    #region Command

    private void AddCeedDetail()
    {
      var hiroiMasterViewModel = new HiroiMasterViewModel( _document, HiroiMasterModels ) ;
      var hiroiMasterDialog = new HiroiMasterDialog( hiroiMasterViewModel ) ;
      if ( true == hiroiMasterDialog.ShowDialog() ) {
        var ceedDetailModel = new CeedDetailModel( hiroiMasterViewModel.HiroiMasterSelected?.Buzaicd, hiroiMasterViewModel.HiroiMasterSelected?.Hinmei, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, "", QuantityDefault, UnitDefault, this.SymbolInformation.Id, TrajectoryDefault ) ;
        CeedDetailList.Add( ceedDetailModel ) ;
        CollectionViewSource.GetDefaultView( CeedDetailList ).Refresh() ;
      }
    }

    private void DeleteCeedDetail()
    {
      if ( null != CeedDetailSelected )
        CeedDetailList.Remove( CeedDetailSelected ) ;
    }

    #endregion
 
    public SymbolInformationViewModel( Document? document, SymbolInformationModel? symbolInformationModel )
    {
      _document = document ;
      SymbolInformation = symbolInformationModel ?? new SymbolInformationModel() ;

      if ( ! string.IsNullOrEmpty( SymbolInformation.Id ) && SymbolInformation.Id != "-1" ) {
        CeedDetailList = new ObservableCollection<CeedDetailModel>( _document!.GetCeedDetailStorable().AllCeedDetailModelData.FindAll( x => x.ParentId == SymbolInformation.Id ) ) ;
      }
      else {
        CeedDetailList = new ObservableCollection<CeedDetailModel>() ;
      }

      ConstructionClassificationTypeList = new ObservableCollection<string>( Enum.GetNames( typeof( CreateDetailTableCommandBase.ConstructionClassificationType ) ).ToList() ) ;
    }
  }
}