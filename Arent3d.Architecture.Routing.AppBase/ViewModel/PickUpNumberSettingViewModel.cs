using System.Linq ;
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
      var textNotePickUpModelStorable = _document.GetTextNotePickUpStorable() ;
      var level = _document.ActiveView.GenLevel.Name ;
      IsPickUpNumberSetting = textNotePickUpModelStorable.PickUpNumberSettingOfLevels.FirstOrDefault(pn => pn.Level == level)?.IsPickUpNumberSetting ?? false ;
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
      var textNotePickUpModelStorable = _document.GetTextNotePickUpStorable() ;
      var level = _document.ActiveView.GenLevel.Name ;
      
      var pickUpNumberSettingModel = textNotePickUpModelStorable.PickUpNumberSettingOfLevels.FirstOrDefault( pn => pn.Level == level ) ;
      if ( pickUpNumberSettingModel == null )
        textNotePickUpModelStorable.PickUpNumberSettingOfLevels.Add( new PickUpNumberSettingModel( level, IsPickUpNumberSetting ) );
      else pickUpNumberSettingModel.IsPickUpNumberSetting = IsPickUpNumberSetting ;
      
      using Transaction transaction = new(_document, "Save Pick Up Number Setting") ;
      transaction.Start() ;
      textNotePickUpModelStorable.Save() ;
      transaction.Commit() ;

      window.DialogResult = true ;
      window.Close() ;
    }
  }
}