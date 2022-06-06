using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using System.Windows.Input ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.AppBase.ViewModel.Models ;

namespace Arent3d.Architecture.Routing.AppBase.ViewModel
{
  public class FilterFieldViewModel : NotifyPropertyChanged
  {

    #region Properties

    private string? _fieldName ;
    public string FieldName
    {
      get => _fieldName ??= string.Empty ;
      set
      {
        _fieldName = value ;
        OnPropertyChanged();
      }
    }

    private List<OptionModel>? _fieldValues ;

    public List<OptionModel> FieldValues
    {
      get => _fieldValues ??= new List<OptionModel>() ;
      set
      {
        _fieldValues = value ;
        OnPropertyChanged();
      }
    }

    private bool? _isCheckAll;
    public bool IsCheckAll
    {
      get
      {
        if ( FieldValues.All( x => x.IsChecked ) )
          _isCheckAll = true ;
        else
          _isCheckAll = false ;

        return _isCheckAll.Value ;
      }
      set
      {
        _isCheckAll = value ;
        if ( _isCheckAll.Value ) {
          foreach ( var fieldValue in FieldValues ) {
            fieldValue.IsChecked = true ;
          }
        }
        else {
          foreach ( var fieldValue in FieldValues ) {
            fieldValue.IsChecked = false ;
          }
        }
        OnPropertyChanged();
      }
    }

    public bool IsOk { get ; set ; }
    #endregion

    #region Constructors

    public FilterFieldViewModel(){}

    public FilterFieldViewModel( string fieldName, List<OptionModel> optionModels )
    {
      FieldName = fieldName ;
      FieldValues = optionModels ;
    }

    #endregion

    #region Commands

    public ICommand CloseCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          IsOk = false ;
          wd.Close();
        } ) ;
      }
    }
    
    public ICommand OkCommand
    {
      get
      {
        return new RelayCommand<Window>( wd => null != wd, wd =>
        {
          IsOk = true ;
          wd.Close();
        } ) ;
      }
    }

    #endregion

  }
}