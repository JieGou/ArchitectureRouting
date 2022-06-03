using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PullBoxViewModel : NotifyPropertyChanged
  {
    private const double DefaultDistanceHeight = 150 ;
    private bool _isCreatePullBoxWithoutSettingHeight = true ;

    public bool IsCreatePullBoxWithoutSettingHeight
    {
      get => _isCreatePullBoxWithoutSettingHeight ;
      set
      {
        _isCreatePullBoxWithoutSettingHeight = value ; 
        OnPropertyChanged();
      }
    }
    
    private double _heightConnector ;

    public double HeightConnector
    {
      get => _heightConnector ;
      set
      {
        _heightConnector = value ; 
        OnPropertyChanged();
      }
    }
    
    private double _heightWire ;

    public double HeightWire
    {
      get => _heightWire ;
      set
      {
        _heightWire = value ; 
        OnPropertyChanged();
      }
    }
    
    public PullBoxViewModel( )
    {
      HeightConnector = 3000 ;
      HeightWire = 1000 ;
    }
    
    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          if ( HeightConnector - HeightWire < DefaultDistanceHeight ) {
            HeightWire = HeightConnector - DefaultDistanceHeight ;
            MessageBox.Show( $"Height wire must be smaller than height wire at least {DefaultDistanceHeight}mm ", "Alert Message" ) ;
          }
          else {
            wd.DialogResult = true ;
            wd.Close() ;
          }
        } ) ;
      }
    }
  }
}