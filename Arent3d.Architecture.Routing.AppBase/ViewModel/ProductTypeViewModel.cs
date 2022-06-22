using System ;
using System.Collections.Generic;
using System.Linq;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class ProductTypeViewModel : NotifyPropertyChanged
  {

    #region Constructors

    public ProductTypeViewModel()
    {
      ProductTypes = Enum.GetValues(typeof(PickUpViewModel.ProductType)).Cast<PickUpViewModel.ProductType?>().ToDictionary(g => g.ToString(), g => g) ;
    }

    #endregion

    #region Properties
    private const string AllProductTypes = "All";

    private Dictionary<string, PickUpViewModel.ProductType?>? _productTypes ;
    public Dictionary<string, PickUpViewModel.ProductType?> ProductTypes
    {
      get => _productTypes ??= new Dictionary<string, PickUpViewModel.ProductType?>() ;
      set
      {
        _productTypes = new() ;
        _productTypes.TryAdd( AllProductTypes, null ) ;
        _productTypes.AddRange( value ) ;
        OnPropertyChanged();
      }
    }

    private PickUpViewModel.ProductType? _selectedProductType ;
    public PickUpViewModel.ProductType? SelectedProductType
    {
      get => _selectedProductType ??= ProductTypes[AllProductTypes] ;
      set { _selectedProductType = value ; OnPropertyChanged(); }
    }
    #endregion
  }
}