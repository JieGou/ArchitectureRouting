using System ;
using System.ComponentModel ;
using System.IO ;
using System.Runtime.CompilerServices ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ImportDwgMappingModel: NotifyPropertyChanged
  {
    private string _id ;

    public string Id
    {
      get => _id ;
      set
      {
        _id = value ;
        OnPropertyChanged() ;
      }
    }
    
    private string _fullFilePath ;

    public string FullFilePath
    {
      get => _fullFilePath ;
      set
      {
        _fullFilePath = value ;
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
    
    private double _floorHeight ;

    public double FloorHeight
    {
      get => _floorHeight ;
      set
      {
        _floorHeight = value ;
        OnPropertyChanged() ;
      }
    }

    public ImportDwgMappingModel( string fileName, string floorName, double floorHeight )
    {
      _id = Guid.NewGuid().ToString() ;
      _fullFilePath = fileName ;
      _fileName = !string.IsNullOrEmpty(fileName) ? Path.GetFileName(fileName) : "" ;
      _floorName = floorName ;
      _floorHeight = floorHeight ;
    }
  }
}