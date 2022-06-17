using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Model ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ElectricalCategoryViewModel : NotifyPropertyChanged
  {
    private readonly Document? _document ;
    private List<HiroiMasterModel> HiroiMasterModels ;
    private List<HiroiSetMasterModel>? HiroiSetMasterNormalModels ;
    private List<HiroiSetMasterModel>? HiroiSetMasterEcoModels ;
    private double? _quantityDefault ;
    private string? _unitDefault ;
    private string? _trajectoryDefault ;
    private string? _symbolInformationId ;
    public List<ElectricalCategoryModel> ElectricalCategoriesEco { get ; set ; }
    public List<ElectricalCategoryModel> ElectricalCategoriesNormal { get ; set ; }

    public Dictionary<string, string> DictElectricalCategoriesEcoKey { get ; set ; }
    public Dictionary<string, string> DictElectricalCategoriesNormalKey { get ; set ; }
    public CeedDetailModel? CeedDetailSelected { get ; set ; }

    public ElectricalCategoryViewModel( Document? document, List<ElectricalCategoryModel> electricalCategoriesEco, List<ElectricalCategoryModel> electricalCategoriesNormal, Dictionary<string, string> dictElectricalCategoriesEcoKey, Dictionary<string, string> dictElectricalCategoriesNormalKey, List<HiroiMasterModel> hiroiMasterModels, List<HiroiSetMasterModel>? hiroiSetMasterNormalModels, List<HiroiSetMasterModel>? hiroiSetMasterEcoModels, double? quantityDefault, string? unitDefault, string? trajectoryDefault, string? symbolInformationId )
    {
      _document = document ;
      ElectricalCategoriesEco = electricalCategoriesEco ;
      ElectricalCategoriesNormal = electricalCategoriesNormal ;
      DictElectricalCategoriesEcoKey = dictElectricalCategoriesEcoKey ;
      DictElectricalCategoriesNormalKey = dictElectricalCategoriesNormalKey ;

      HiroiMasterModels = hiroiMasterModels ;
      HiroiSetMasterEcoModels = hiroiSetMasterEcoModels ;
      HiroiSetMasterNormalModels = hiroiSetMasterNormalModels ;
      _quantityDefault = quantityDefault ;
      _trajectoryDefault = trajectoryDefault ;
      _unitDefault = unitDefault ;
      _symbolInformationId = symbolInformationId ;
    }

    public CeedDetailModel? LoadData( bool isEcoModel, string searchValue )
    {
      searchValue = isEcoModel ? DictElectricalCategoriesEcoKey.ContainsKey( searchValue ) ? DictElectricalCategoriesEcoKey[ searchValue ] : searchValue : DictElectricalCategoriesNormalKey.ContainsKey( searchValue ) ? DictElectricalCategoriesNormalKey[ searchValue ] : searchValue ;
      var hiroiMasterViewModel = new HiroiMasterViewModel( _document, HiroiMasterModels, HiroiSetMasterEcoModels, HiroiSetMasterNormalModels, isEcoModel ) { SearchText = searchValue } ;
      var hiroiMasterDialog = new HiroiMasterDialog( hiroiMasterViewModel ) ;
      CeedDetailSelected = true == hiroiMasterDialog.ShowDialog() ? new CeedDetailModel( hiroiMasterViewModel.HiroiMasterSelected?.Buzaicd, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, "", _quantityDefault, _unitDefault, _symbolInformationId, _trajectoryDefault, hiroiMasterViewModel.HiroiMasterSelected?.Size1, hiroiMasterViewModel.HiroiMasterSelected?.Size2, hiroiMasterViewModel.HiroiMasterSelected?.Kikaku, 1, string.Empty, string.Empty, string.Empty, 0, 1, _quantityDefault, string.Empty, true ) : null ;

      if ( _document == null ) return CeedDetailSelected ;
      var csvStorable = _document!.GetCsvStorable() ;
      var conduit = csvStorable.ConduitsModelData.FirstOrDefault( x => x.PipingType + x.Size == CeedDetailSelected?.ProductName ) ;
      if ( CeedDetailSelected == null ) return CeedDetailSelected ;
      CeedDetailSelected.IsConduit = conduit != null ;

      return CeedDetailSelected ;
    }
  }
}