using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class ChangePlumbingInformationViewModel : NotifyPropertyChanged
  {
    private const string NoPlumping = "配管なし" ;
    private const string NoPlumbingSize = "（なし）" ;
    private Document _document ;
    private List<ConduitsModel> _conduitsModelData ;
    
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
    
    private int _numberOfPlumbing ;

    public int NumberOfPlumbing
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
    
    public List<DetailTableModel.ComboboxItemType> PlumbingTypes { get ; }

    public List<DetailTableModel.ComboboxItemType> PlumbingSizes { get ; set ; }
    
    public List<DetailTableModel.ComboboxItemType> NumbersOfPlumbing { get ; }

    public List<DetailTableModel.ComboboxItemType> ConstructionClassifications { get ; }

    public List<DetailTableModel.ComboboxItemType> ConstructionItems { get ; }
    
    public ICommand SetPlumbingSizesCommand => new RelayCommand( SetPlumbingSizes ) ;
    public RelayCommand<Window> ApplyCommand => new(Apply) ;
    
    public ChangePlumbingInformationViewModel( Document document, List<ConduitsModel> conduitsModelData, string plumbingType, string plumbingSize, int numberOfPlumbing, string constructionClassification, string constructionItem, List<DetailTableModel.ComboboxItemType> plumbingTypes, List<DetailTableModel.ComboboxItemType> plumbingSizes, List<DetailTableModel.ComboboxItemType> numbersOfPlumbing, List<DetailTableModel.ComboboxItemType> constructionClassifications, List<DetailTableModel.ComboboxItemType> constructionItems )
    {
      _document = document ;
      _conduitsModelData = conduitsModelData ;
      _plumbingType = plumbingType ;
      _plumbingSize = plumbingSize ;
      _numberOfPlumbing = numberOfPlumbing ;
      _constructionClassification = constructionClassification ;
      _constructionItem = constructionItem ;
      PlumbingTypes = plumbingTypes ;
      PlumbingSizes = plumbingSizes ;
      NumbersOfPlumbing = numbersOfPlumbing ;
      ConstructionClassifications = constructionClassifications ;
      ConstructionItems = constructionItems ;
    }
    
    private void SetPlumbingSizes()
    {
      if ( _plumbingType != NoPlumping ) {
        var plumbingSizesOfPlumbingType = _conduitsModelData.Where( c => c.PipingType == _plumbingType ).Select( c => c.Size.Replace( "mm", "" ) ).Distinct().ToList() ;
        PlumbingSizes = ( from plumbingSizeName in plumbingSizesOfPlumbingType select new DetailTableModel.ComboboxItemType( plumbingSizeName, plumbingSizeName ) ).ToList() ;
      }
      else {
        PlumbingSizes = new List<DetailTableModel.ComboboxItemType>() { new( NoPlumbingSize, NoPlumbingSize ) } ;
      }
    }
    
    private void Apply( Window window )
    {
      window.DialogResult = true ;
      window.Close() ;
    }
  }
}