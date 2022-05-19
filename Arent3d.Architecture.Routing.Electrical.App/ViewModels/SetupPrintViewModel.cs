using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Electrical.App.ViewModels.Models ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.ViewModels
{
  public class SetupPrintViewModel : NotifyPropertyChanged
  {
    private readonly UIDocument _uiDocument ;
    private readonly SetupPrintStorable _setupPrintStorable ;

    private ObservableCollection<TitleBlockModel>? _titleBlocks ;

    public ObservableCollection<TitleBlockModel> TitleBlocks
    {
      get
      {
        if ( null != _titleBlocks ) return _titleBlocks ;

        var filter = new FilteredElementCollector( _uiDocument.Document ) ;
        var titleBlockModels = new List<TitleBlockModel> { new() { TitleBlockName = "None", TitleBlockId = ElementId.InvalidElementId.IntegerValue } } ;
        titleBlockModels.AddRange( filter.OfCategory( BuiltInCategory.OST_TitleBlocks ).OfType<FamilySymbol>().Select( x => new TitleBlockModel { TitleBlockName = x.Name, TitleBlockId = x.Id.IntegerValue } ).OrderBy( x => x.TitleBlockName ).ToList() ) ;
        _titleBlocks = new ObservableCollection<TitleBlockModel>( titleBlockModels ) ;

        return _titleBlocks ;
      }
      set
      {
        _titleBlocks = value ;
        OnPropertyChanged() ;
      }
    }

    private TitleBlockModel? _titleBlock ;

    public TitleBlockModel? TitleBlock
    {
      get
      {
        if ( null != _titleBlock ) return _titleBlock ;
        
        var titleBlock = TitleBlocks.FirstOrDefault( x => x.TitleBlockId == _setupPrintStorable.TitleBlockTypeId ) ;
        if ( null != titleBlock )
          return _titleBlock = titleBlock ;
        _titleBlock = TitleBlocks.FirstOrDefault() ;

        return _titleBlock ;
      }
      set
      {
        _titleBlock = value ;
        OnPropertyChanged() ;
      }
    }

    private string? _scale ;

    public string Scale
    {
      get => _scale ??= $"{_setupPrintStorable.Scale}";
      set
      {
        _scale = value ;
        OnPropertyChanged() ;
      }
    }

    public ExternalEventHandler? ExternalEventHandler { get ; set ; }

    private Dictionary<Func<int, bool>, double>? _mapping ;
    public Dictionary<Func<int, bool>, double> Mapping
    {
      get
      {
        return _mapping ??= _mapping = new Dictionary<Func<int, bool>, double>
        {
          { sc => 0 < sc && sc <= 20, 200 / 100d },
          { sc => 20 < sc && sc <= 30, 166.7 / 100d },
          { sc => 30 < sc && sc <= 50, 133.3 / 100d },
          { sc => 50 < sc && sc <= 60, 133.3 / 100d },
          { sc => 60 < sc && sc <= 150, 100 / 100d },
          { sc => 150 < sc && sc <= 250, 76.7 / 100d },
          { sc => 250 < sc && sc <= 500, 76.7 / 100d },
          { sc => 500 < sc && sc <= 1000, 50 / 100d },
          { sc => 1000 < sc && sc <= 9999, 50 / 100d }
        } ;
      }
    }

    public SetupPrintViewModel( UIDocument uiDocument )
    {
      _uiDocument = uiDocument ;
      _setupPrintStorable = _uiDocument.Document.GetSetupPrintStorable() ;
    }
    
    public ICommand SaveCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          if ( ! int.TryParse( Scale, out var result ) && result is <= 0 or > 9999)
            TaskDialog.Show( "Arent Inc", "Invalid scale for the view plan!") ;

          Scale = $"{result}" ;
          ExternalEventHandler?.AddAction( Save )?.Raise() ;
          wd.Close();
        } ) ;
      }
    }

    private void Save()
    {
      try {
        using var transaction = new Transaction( _uiDocument.Document ) ;
        transaction.Start( "Save Setup Print" ) ;

        _setupPrintStorable.TitleBlockTypeId = TitleBlock!.TitleBlockId ;
        _setupPrintStorable.Scale = int.Parse( Scale ) ;
        _setupPrintStorable.Ratio = Mapping.SingleOrDefault( x => x.Key( _setupPrintStorable.Scale ) ).Value ;
        _setupPrintStorable.Save() ;

        var textNoteType = TextNoteHelper.FindOrCreateTextNoteType( _uiDocument.Document ) ;
        var viewPlanFilter = new FilteredElementCollector( _uiDocument.Document ) ;
        var viewPlans = viewPlanFilter.OfClass( typeof( ViewPlan ) ).OfType<ViewPlan>().Where( x => ! x.IsTemplate ) ;
        foreach ( var viewPlan in viewPlans ) {
          if ( null != viewPlan.ViewTemplateId && _uiDocument.Document.GetElement( viewPlan.ViewTemplateId ) is View viewTemplate && viewTemplate.Scale != _setupPrintStorable.Scale ) {
            viewTemplate.Scale = _setupPrintStorable.Scale ;
          }
          else if (viewPlan.Scale != _setupPrintStorable.Scale) {
            viewPlan.Scale = _setupPrintStorable.Scale ;
          }

          if ( null == textNoteType ) continue ;
          var textNoteFilter = new FilteredElementCollector( _uiDocument.Document, viewPlan.Id ) ;
          textNoteFilter.OfClass( typeof( TextNote ) ).OfType<TextNote>().ForEach(x => x.TextNoteType = textNoteType);
        }

        transaction.Commit() ;
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Arent Inc", exception.Message ) ;
      }
    }
    
  }
}