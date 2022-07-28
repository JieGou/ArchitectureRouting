using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class PickUpNumberSettingViewModel : NotifyPropertyChanged
  {
    private readonly Document _document ;

    private bool _isPickUpNumberSetting ;

    public PickUpNumberSettingViewModel( Document document )
    {
      _document = document ;
      var wireLengthNotationStorable = _document.GetWireLengthNotationStorable() ;
      var levelId = _document.ActiveView.GenLevel.Id.IntegerValue ;
      IsPickUpNumberSetting = wireLengthNotationStorable.PickUpNumberSettingData[ levelId ]?.IsPickUpNumberSetting ?? false ;
    }

    public bool IsPickUpNumberSetting
    {
      get => _isPickUpNumberSetting ;
      set
      {
        _isPickUpNumberSetting = value ;
        OnPropertyChanged() ;
      }
    }

    public RelayCommand<Window> CancelCommand => new(Cancel) ;
    public RelayCommand<Window> ExecuteCommand => new(Execute) ;

    private void Cancel( Window window )
    {
      window.DialogResult = false ;
      window.Close() ;
    }

    private void Execute( Window window )
    {
      var wireLengthNotationStorable = _document.GetWireLengthNotationStorable() ;
      var level = _document.ActiveView.GenLevel ;
      var levelId = level.Id.IntegerValue ;

      var pickUpNumberSettingModel = wireLengthNotationStorable.PickUpNumberSettingData[ levelId ] ;
      if ( pickUpNumberSettingModel == null )
        wireLengthNotationStorable.PickUpNumberSettingData.Add( levelId, new PickUpNumberSettingModel( level, IsPickUpNumberSetting ) ) ;
      else 
        pickUpNumberSettingModel.IsPickUpNumberSetting = IsPickUpNumberSetting ;

      using Transaction transaction = new(_document, "Save Pick Up Number Setting") ;
      transaction.Start() ;
      wireLengthNotationStorable.Save() ;
      transaction.Commit() ;

      window.DialogResult = true ;
      window.Close() ;
    }
  }
}