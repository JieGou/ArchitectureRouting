using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Initialization ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Architecture.Routing.Storages.Models ;
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

    private string _classificationOfPlumbing ;

    public string ClassificationOfPlumbing
    {
      get => _classificationOfPlumbing ;
      set
      {
        _classificationOfPlumbing = value ;
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
    
    private int _selectedIndex ;

    public int SelectedIndex
    {
      get => _selectedIndex ;
      set
      {
        _selectedIndex = value ;
        ClassificationOfPlumbing = ChangePlumbingInformationModels.ElementAt( _selectedIndex ).ClassificationOfPlumbing ;
        PlumbingSize = ChangePlumbingInformationModels.ElementAt( _selectedIndex ).PlumbingSize ;
        NumberOfPlumbing = ChangePlumbingInformationModels.ElementAt( _selectedIndex ).NumberOfPlumbing ;
        IsInDoor = ChangePlumbingInformationModels.ElementAt( _selectedIndex ).IsInDoor ;
        IsEnabled = GetIsEnabled() ;
        OnPropertyChanged() ;
      }
    }

    public List<ChangePlumbingInformationModel> ChangePlumbingInformationModels { get ; set ; }
    public List<DetailTableItemModel.ComboboxItemType> PlumbingTypes { get ; }
    public List<DetailTableItemModel.ComboboxItemType> ClassificationsOfPlumbing { get ; }
    public List<DetailTableItemModel.ComboboxItemType> ConcealmentOrExposure { get ; }
    public List<DetailTableItemModel.ComboboxItemType> InOrOutDoor { get ; }
    public List<ConnectorInfo> ConnectorInfos { get ; }

    public ICommand SelectionChangedPlumbingTypeCommand => new RelayCommand( SetPlumbingSizes ) ;
    public ICommand SelectionChangedConcealmentOrExposureCommand => new RelayCommand( SelectionChangedConcealmentOrExposure ) ;
    public RelayCommand<Window> ApplyCommand => new(Apply) ;
    
    public ChangePlumbingInformationViewModel( List<ConduitsModel> conduitsModelData, List<ChangePlumbingInformationModel> changePlumbingInformationModels, List<DetailTableItemModel.ComboboxItemType> plumbingTypes, List<DetailTableItemModel.ComboboxItemType> classificationsOfPlumbing, List<DetailTableItemModel.ComboboxItemType> concealmentOrExposure, List<DetailTableItemModel.ComboboxItemType> inOrOutDoor, List<ConnectorInfo> connectorInfos )
    {
      _conduitsModelData = conduitsModelData ;
      var changePlumbingInformationModel = changePlumbingInformationModels.First() ;
      _conduitId = changePlumbingInformationModel.ConduitId ;
      _plumbingType = changePlumbingInformationModel.PlumbingType ;
      _plumbingSize = changePlumbingInformationModel.PlumbingSize ;
      _numberOfPlumbing = changePlumbingInformationModel.NumberOfPlumbing ;
      _classificationOfPlumbing = changePlumbingInformationModel.ClassificationOfPlumbing ;
      _constructionItem = changePlumbingInformationModel.ConstructionItems ;
      _isExposure = changePlumbingInformationModel.IsExposure ;
      _isInDoor = changePlumbingInformationModel.IsInDoor ;
      _isEnabled = GetIsEnabled() ;
      _selectedIndex = -1 ;
      PlumbingTypes = plumbingTypes ;
      ClassificationsOfPlumbing = classificationsOfPlumbing ;
      ConcealmentOrExposure = concealmentOrExposure ;
      InOrOutDoor = inOrOutDoor ;
      ConnectorInfos = connectorInfos ;
      ChangePlumbingInformationModels = changePlumbingInformationModels ;
    }
    
    private void SetPlumbingSizes()
    {
      const double percentage = 0.32 ;
      foreach ( var changePlumbingInformationModel in ChangePlumbingInformationModels ) {
        var wireCrossSectionalArea = changePlumbingInformationModel.WireCrossSectionalArea ;
        if ( _plumbingType != NoPlumping ) {
          var conduitsModels = _conduitsModelData.Where( c => c.PipingType == _plumbingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
          var plumbing = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= wireCrossSectionalArea / percentage ) ?? conduitsModels.Last() ;
          PlumbingSize = plumbing.Size.Replace( "mm", "" ) ;
          changePlumbingInformationModel.PlumbingName = plumbing.Name ;
          NumberOfPlumbing = plumbing == conduitsModels.Last() ? ( (int) Math.Ceiling( ( wireCrossSectionalArea / percentage ) / double.Parse( plumbing.InnerCrossSectionalArea ) ) ).ToString() : "1" ;
        }
        else {
          PlumbingSize = NoPlumbingSize ;
          NumberOfPlumbing = string.Empty ;
          changePlumbingInformationModel.PlumbingName = string.Empty ;
        }
        changePlumbingInformationModel.PlumbingType = PlumbingType ;
        changePlumbingInformationModel.PlumbingSize = PlumbingSize ;
        changePlumbingInformationModel.NumberOfPlumbing = NumberOfPlumbing ;
      }
    }

    private void SelectionChangedConcealmentOrExposure()
    {
      foreach ( var changePlumbingInformationModel in ChangePlumbingInformationModels ) {
        if ( changePlumbingInformationModel.ClassificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.地中埋設.GetFieldName()
             || changePlumbingInformationModel.ClassificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.打ち込み.GetFieldName()
             || changePlumbingInformationModel.ClassificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.冷媒管共巻配線.GetFieldName() )
        {
          changePlumbingInformationModel.IsExposure = false ; // 施工区分が地中埋設、打ち込み、冷媒管共巻配線となっている場合、区分が隠蔽となる
        }
        else if ( changePlumbingInformationModel.ClassificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.露出.GetFieldName() ) {
          changePlumbingInformationModel.IsExposure = true ; // 施工区分が露出となっている場合、区分が露出となる
        }
        else {
          changePlumbingInformationModel.IsExposure = IsExposure ; // 施工区分がケーブルラック配線、天井コロガシ、二重床となっている場合、区分は隠蔽/露出を選択できる
        }
      }
    }

    private bool GetIsEnabled()
    {
      return _classificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.天井コロガシ.GetFieldName() 
             || _classificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.ケーブルラック配線.GetFieldName() 
             || _classificationOfPlumbing == CreateDetailTableCommandBase.ConstructionClassificationType.フリーアクセス.GetFieldName() ;
    }
    
    private void Apply( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }
    
    public class ConnectorInfo
    {
      public string Connector { get ; }
      public string ConstructionItems { get ; }
      public double ConduitDirectionZ { get ; }

      public ConnectorInfo( string connector, string constructionItems, double? conduitDirectionZ )
      {
        Connector = connector ;
        ConstructionItems = constructionItems ;
        ConduitDirectionZ = conduitDirectionZ ?? 1 ;
      }
    }
  }
}