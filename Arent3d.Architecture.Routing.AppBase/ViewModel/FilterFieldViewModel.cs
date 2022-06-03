using System.Collections.Generic ;
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

    #endregion

  }
}