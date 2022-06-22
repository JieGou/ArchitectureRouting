using System ;
using System.Collections.Generic;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using System.Linq;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
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
    private const string AllProductTypes = "All product types";

    private Dictionary<string, PickUpViewModel.ProductType?>? _productTypes ;
    public Dictionary<string, PickUpViewModel.ProductType?> ProductTypes
    {
      get => _productTypes ??= new Dictionary<string, PickUpViewModel.ProductType?>() ;
      set
      {
        _productTypes = value ;
        ProductTypes.TryAdd( AllProductTypes, null ) ;
        SelectedProductType = _productTypes[AllProductTypes] ;
        OnPropertyChanged();
      }
    }

    private PickUpViewModel.ProductType? _selectedProductType ;
    public PickUpViewModel.ProductType? SelectedProductType
    {
      get => _selectedProductType ??= ProductTypes[AllProductTypes] ;
      set { _selectedProductType = value ; OnPropertyChanged(); }
    }

    public bool IsSelected { get ; set ; }

    #endregion

    #region Commands

    public ICommand FilterCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          IsSelected = true ;
          wd.Close();
        } ) ;
      }
    }

    #endregion

  }
}