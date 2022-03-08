using System ;
using System.ComponentModel ;
using System.IO ;
using System.Runtime.CompilerServices ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ImportDwgMappingModel: INotifyPropertyChanged
  {
    private string _fullFilePath ;

    public string FullFilePath
    {
      get => _fullFilePath ;
      set
      {
        _fileName = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _fileName ;

    public string FileName
    {
      get => _fileName ;
      set
      {
        _fileName = value ;
        OnPropertyChanged() ;
      }
    }

    private string _floorName ;

    public string FloorName
    {
      get => _floorName ;
      set
      {
        _floorName = value ;
        OnPropertyChanged() ;
      }
    }

    public ImportDwgMappingModel( string fileName, string floorName )
    {
      _fullFilePath = fileName ;
      _fileName = Path.GetFileName(fileName) ;
      _floorName = floorName ;
    }

    public event PropertyChangedEventHandler? PropertyChanged ;

    protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = "" )
    {
      PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) ) ;
    }
  }
}