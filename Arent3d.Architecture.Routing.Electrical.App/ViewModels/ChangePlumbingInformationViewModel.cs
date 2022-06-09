using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Utility ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class ChangePlumbingInformationViewModel : NotifyPropertyChanged
  {
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "なし" ;
    private readonly List<ConduitsModel> _conduitsModelData ;
    
    private string _conduitId ;

    public string ConduitId
    {
      get => _conduitId ;
      set
      {
        _conduitId = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _plumbingType ;

    public string PlumbingType
    {
      get => _plumbingType ;
      set
      {
        _plumbingType = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _plumbingSize ;

    public string PlumbingSize
    {
      get => _plumbingSize ;
      set
      {
        _plumbingSize = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _numberOfPlumbing ;

    public string NumberOfPlumbing
    {
      get => _numberOfPlumbing ;
      set
      {
        _numberOfPlumbing = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _constructionClassification ;

    public string ConstructionClassification
    {
      get => _constructionClassification ;
      set
      {
        _constructionClassification = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _constructionItem ;

    public string ConstructionItem
    {
      get => _constructionItem ;
      set
      {
        _constructionItem = value ;
        OnPropertyChanged() ;
      }
    }
    
    private bool _isExposure ;

    public bool IsExposure
    {
      get => _isExposure ;
      set
      {
        _isExposure = value ;
        OnPropertyChanged() ;
      }
    }
    
    private bool _isEnabled;

    public bool IsEnabled
    {
      get => _isEnabled ;
      set
      {
        _isEnabled = value ;
        OnPropertyChanged() ;
      }
    }
    
    private bool _isInDoor ;

    public bool IsInDoor
    {
      get => _isInDoor ;
      set
      {
        _isInDoor = value ;
        OnPropertyChanged() ;
      }
    }

    public List<ChangePlumbingInformationModel> ChangePlumbingInformationModels { get ; set ; }
    public List<DetailTableModel.ComboboxItemType> PlumbingTypes { get ; }
    public List<DetailTableModel.ComboboxItemType> ConstructionClassifications { get ; }
    public List<DetailTableModel.ComboboxItemType> ConcealmentOrExposure { get ; }
    public List<DetailTableModel.ComboboxItemType> InOrOutDoor { get ; }
    public List<DetailTableModel.ComboboxItemType> ConduitIds { get ; }

    public ICommand SelectionChangedConnectorCommand => new RelayCommand( SelectionChangedConnector ) ;
    public ICommand SelectionChangedPlumbingTypeCommand => new RelayCommand( SetPlumbingSizes ) ;
    public ICommand SelectionChangedConstructionClassificationCommand => new RelayCommand( SelectionChangedConstructionClassification ) ;
    public ICommand SelectionChangedConcealmentOrExposureCommand => new RelayCommand( SelectionChangedConcealmentOrExposure ) ;
    public ICommand SelectionChangedInOrOutDoorCommand => new RelayCommand( SelectionChangedInOrOutDoor ) ;
    public RelayCommand<Window> ApplyCommand => new(Apply) ;
    
    public ChangePlumbingInformationViewModel( List<ConduitsModel> conduitsModelData, List<ChangePlumbingInformationModel> changePlumbingInformationModels, List<DetailTableModel.ComboboxItemType> plumbingTypes, List<DetailTableModel.ComboboxItemType> constructionClassifications, List<DetailTableModel.ComboboxItemType> concealmentOrExposure, List<DetailTableModel.ComboboxItemType> inOrOutDoor, List<DetailTableModel.ComboboxItemType> conduitIds )
    {
      _conduitsModelData = conduitsModelData ;
      var changePlumbingInformationModel = changePlumbingInformationModels.First() ;
      _conduitId = changePlumbingInformationModel.ConduitId ;
      _plumbingType = changePlumbingInformationModel.PlumbingType ;
      _plumbingSize = changePlumbingInformationModel.PlumbingSize ;
      _numberOfPlumbing = changePlumbingInformationModel.NumberOfPlumbing ;
      _constructionClassification = changePlumbingInformationModel.ConstructionClassification ;
      _constructionItem = changePlumbingInformationModel.ConstructionItems ;
      _isExposure = changePlumbingInformationModel.IsExposure ;
      _isEnabled = GetIsEnabled() ;
      PlumbingTypes = plumbingTypes ;
      ConstructionClassifications = constructionClassifications ;
      ConcealmentOrExposure = concealmentOrExposure ;
      InOrOutDoor = inOrOutDoor ;
      ConduitIds = conduitIds ;
      ChangePlumbingInformationModels = changePlumbingInformationModels ;
    }
    
    private void SetPlumbingSizes()
    {
      const double percentage = 0.32 ;
      var changePlumbingInformationModel = ChangePlumbingInformationModels.SingleOrDefault( c => c.ConduitId == _conduitId ) ;
      if ( changePlumbingInformationModel != null ) {
        var wireCrossSectionalArea = changePlumbingInformationModel.WireCrossSectionalArea ;
        if ( _plumbingType != NoPlumping ) {
          var conduitsModels = _conduitsModelData.Where( c => c.PipingType == _plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= wireCrossSectionalArea / percentage ) ?? conduitsModels.Last() ;
          PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          NumberOfPlumbing = plumbing == conduitsModels.Last() ? ( (int) Math.Ceiling( ( wireCrossSectionalArea / percentage ) / double.Parse( plumbing.InnerCrossSectionalArea ) ) ).ToString() : "1" ;
        }
        else {
          PlumbingSize = NoPlumbingSize ;
          NumberOfPlumbing = string.Empty ;
        }
        changePlumbingInformationModel.PlumbingType = PlumbingType ;
        changePlumbingInformationModel.PlumbingSize = PlumbingSize ;
        changePlumbingInformationModel.NumberOfPlumbing = NumberOfPlumbing ;
      }
    }
    
    private void SelectionChangedConnector()
    {
      var changePlumbingInformationModel = ChangePlumbingInformationModels.SingleOrDefault( c => c.ConduitId == _conduitId ) ;
      if ( changePlumbingInformationModel == null ) return ;
      PlumbingType = changePlumbingInformationModel.PlumbingType ;
      PlumbingSize = changePlumbingInformationModel.PlumbingSize ;
      NumberOfPlumbing = changePlumbingInformationModel.NumberOfPlumbing ;
      ConstructionClassification = changePlumbingInformationModel.ConstructionClassification ;
      ConstructionItem = changePlumbingInformationModel.ConstructionItems ;
      IsExposure = changePlumbingInformationModel.IsExposure ;
      IsInDoor = changePlumbingInformationModel.IsInDoor ;
    }
    
    private void SelectionChangedConcealmentOrExposure()
    {
      var changePlumbingInformationModel = ChangePlumbingInformationModels.SingleOrDefault( c => c.ConduitId == _conduitId ) ;
      if ( changePlumbingInformationModel != null ) {
        changePlumbingInformationModel.IsExposure = IsExposure ;
      }
    }
    
    private void SelectionChangedConstructionClassification()
    {
      var changePlumbingInformationModel = ChangePlumbingInformationModels.SingleOrDefault( c => c.ConduitId == _conduitId ) ;
      if ( changePlumbingInformationModel == null ) return ;
      changePlumbingInformationModel.ConstructionClassification = ConstructionClassification ;
      IsEnabled = GetIsEnabled() ;
      IsExposure = IsEnabled && changePlumbingInformationModel.IsExposure || ConstructionClassification == CreateDetailTableCommandBase.ConstructionClassificationType.露出.GetFieldName() ;
    }
    
    private void SelectionChangedInOrOutDoor()
    {
      var changePlumbingInformationModel = ChangePlumbingInformationModels.SingleOrDefault( c => c.ConduitId == _conduitId ) ;
      if ( changePlumbingInformationModel != null ) {
        changePlumbingInformationModel.IsInDoor = IsInDoor ;
      }
    }

    private bool GetIsEnabled()
    {
      return _constructionClassification == CreateDetailTableCommandBase.ConstructionClassificationType.天井コロガシ.GetFieldName() 
             || _constructionClassification == CreateDetailTableCommandBase.ConstructionClassificationType.ケーブルラック配線.GetFieldName() 
             || _constructionClassification == CreateDetailTableCommandBase.ConstructionClassificationType.フリーアクセス.GetFieldName() ;
    }
    
    private void Apply( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }
  }
}