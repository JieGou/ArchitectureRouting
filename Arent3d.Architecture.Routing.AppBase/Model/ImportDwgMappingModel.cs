using System ;
using System.IO ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ImportDwgMappingModel : NotifyPropertyChanged
  {
    public string Id { get ; }

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
    
    private int _scale ;

    public int Scale
    {
      get => _scale ;
      set
      {
        _scale = value ;
        OnPropertyChanged() ;
      }
    }

    public ImportDwgMappingModel( string fileName, string floorName, double floorHeight )
    {
      Id = Guid.NewGuid().ToString() ;
      _fullFilePath = fileName ;
      _fileName = ! string.IsNullOrEmpty( fileName ) ? Path.GetFileName( fileName ) : "" ;
      _floorName = floorName ;
      _floorHeight = floorHeight ;
    }
    
    public ImportDwgMappingModel( string fileName, string floorName, double floorHeight, int scale )
    {
      Id = Guid.NewGuid().ToString() ;
      _fullFilePath = fileName ;
      _fileName = ! string.IsNullOrEmpty( fileName ) ? Path.GetFileName( fileName ) : "" ;
      _floorName = floorName ;
      _floorHeight = floorHeight ;
      _scale = scale ;
    }
  }
}