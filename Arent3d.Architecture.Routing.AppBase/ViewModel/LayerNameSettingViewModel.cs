using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class LayerNameSettingViewModel : NotifyPropertyChanged
  {
    //private readonly Document _document ;
    private List<Layer> _layers ;
    
    public ObservableCollection<Layer> Layers { get ; }
    
    public RelayCommand<Window> UpdateCommand => new ( Update ) ;

    public LayerNameSettingViewModel(List<Layer> layers )
    {
      Layers = new ObservableCollection<Layer>() ;
      _layers = new List<Layer>() ;
      if ( layers.Any() ) {
        _layers = layers ;
        SetDataSource( _layers ) ;
      }
    }
    
    private void Update( Window window )
    {
      window.DialogResult = true ;
      window.Close();
    }

    private void SetDataSource( List<Layer> layers )
    {
      Layers.Clear();
      foreach ( var layer in layers ) {
        Layers.Add( layer );
      }
    }
  }

  public class Layer
  {
    public string Name { get ; set ; } 

    public Layer( string name)
    {
      Name = name ;
    }
  }
}