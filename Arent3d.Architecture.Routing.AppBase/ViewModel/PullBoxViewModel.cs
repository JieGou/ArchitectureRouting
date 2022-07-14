using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;


namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PullBoxViewModel : NotifyPropertyChanged
  {
    private const string PullBoxName = "プルボックス" ;
    private const string DefaultBuzaicdForGradeModeThanThree = "032025" ;
    private const double DefaultDistanceHeight = 150 ;
    private bool _isCreatePullBoxWithoutSettingHeight = true ;

    public bool IsCreatePullBoxWithoutSettingHeight
    {
      get => _isCreatePullBoxWithoutSettingHeight ;
      set
      {
        _isCreatePullBoxWithoutSettingHeight = value ;
        OnPropertyChanged() ;
      }
    }

    public List<PullBoxModel> PullBoxModels { get ; }

    private PullBoxModel? _selectedPullBox ;

    public PullBoxModel? SelectedPullBox
    {
      get => _selectedPullBox ;
      set
      {
        if ( _selectedPullBox == value )
          return ;
        _selectedPullBox = value ;
        OnPropertyChanged() ;
      }
    }

    private bool _isAutoCalculatePullBoxSize ;

    public bool IsAutoCalculatePullBoxSize
    {
      get => _isAutoCalculatePullBoxSize ;
      set
      {
        if ( _isAutoCalculatePullBoxSize == value )
          return ;
        _isAutoCalculatePullBoxSize = value ;
        OnPropertyChanged() ;
      }
    }

    private double _heightConnector ;

    public double HeightConnector
    {
      get => _heightConnector ;
      set
      {
        _heightConnector = value ;
        OnPropertyChanged() ;
      }
    }

    private double _heightWire ;

    public double HeightWire
    {
      get => _heightWire ;
      set
      {
        _heightWire = value ;
        OnPropertyChanged() ;
      }
    }

    public PullBoxViewModel( Document document )
    {
      GetPullBoxModels( document ) ;

      var gradeMode = GetGradeMode( document ) ;
      PullBoxModels = GetPullBoxModels( document ) ;
      HeightConnector = 3000 ;
      HeightWire = 1000 ;
      IsAutoCalculatePullBoxSize = true ;
      if ( gradeMode > 3 ) {
        SelectedPullBox = PullBoxModels.FirstOrDefault( x => x.Buzaicd == DefaultBuzaicdForGradeModeThanThree ) ;
      }
    }

    private int? GetGradeMode( Document document )
    {
      var defaultSettingInfo = document.GetDefaultSettingStorable() ;
      return defaultSettingInfo.GradeSettingData.GradeMode ;
    }

    private List<PullBoxModel> GetPullBoxModels( Document document )
    {
      var csvStorable = document.GetCsvStorable() ;
      var allPullBoxHiroiMasterModel = csvStorable.HiroiMasterModelData.Where( hr => hr.Hinmei == PullBoxName ) ;
      var pullBoxModels =  from hiroiMasterModel in allPullBoxHiroiMasterModel
        select new PullBoxModel( hiroiMasterModel ) ;

      return pullBoxModels.OrderBy( pb => pb.Width ).ThenBy( pb => pb.Height )
        .ToList() ;

    }

    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          if ( HeightConnector - HeightWire < DefaultDistanceHeight ) {
            HeightWire = HeightConnector - DefaultDistanceHeight ;
            MessageBox.Show( $"Height wire must be smaller than height wire at least {DefaultDistanceHeight}mm ",
              "Alert Message" ) ;
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