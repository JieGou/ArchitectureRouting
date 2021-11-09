using System ;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CnsImportModel : INotifyPropertyChanged
  {
    private int _sequence;
    public int Sequence 
    { 
      get => _sequence;
      set
      {
        _sequence = value;
        OnPropertyChanged();
      }
    }

    public string CategoryName { get; set; }

    public CnsImportModel(int sequence, string categoryName)
    {
      _sequence = sequence;
      CategoryName = categoryName;
    }
    
    public bool Equals( CnsImportModel other )
    {
      return other != null &&
             Sequence == other.Sequence &&
             CategoryName == other.CategoryName ;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}