using System ;
using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Model
{
  public class ImportDwgMappingModel : NotifyPropertyChanged, IDataErrorInfo
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
    
    private bool _isEnabled ;
    public bool IsEnabled 
    { 
      get => _isEnabled ;
      set
      {
        _isEnabled = value ;
        OnPropertyChanged() ;
      } 
    }

    private bool _isDeleted ;
    public bool IsDeleted
    { 
      get => _isDeleted ;
      set
      {
        _isDeleted = value ;
        OnPropertyChanged() ;
      } 
    }
    
    private string _floorHeightDisplay ;

    public string FloorHeightDisplay
    {
      get => _floorHeightDisplay ;
      set
      {
        _floorHeightDisplay = value ;
        OnPropertyChanged() ;
      }
    }
    
    private bool _isEnabledFloorHeight ;
    public bool IsEnabledFloorHeight
    { 
      get => _isEnabledFloorHeight ;
      set
      {
        _isEnabledFloorHeight = value ;
        OnPropertyChanged() ;
      } 
    }

    public string this[ string columnName ]
    {
      get
      {
        var errorMessage = _scale is <= 0 or > 9999 ? "Invalid scale for the view plan!" : string.Empty ;
        return errorMessage ;
      }
    }

    public string Error => string.Empty ;

    public ImportDwgMappingModel( string fileName, string floorName, double floorHeight )
    {
      Id = Guid.NewGuid().ToString() ;
      _fullFilePath = fileName ;
      _fileName = ! string.IsNullOrEmpty( fileName ) ? Path.GetFileName( fileName ) : "" ;
      _floorName = floorName ;
      _floorHeight = floorHeight ;
      _isEnabled = true ;
      _isDeleted = true ;
      _isEnabledFloorHeight = true ;
      _floorHeightDisplay = "0" ;
    }

    public ImportDwgMappingModel( string fileName, string floorName, double floorHeight, int scale, double floorHeightDisplay = 0 )
    {
      Id = Guid.NewGuid().ToString() ;
      _fullFilePath = fileName ;
      _fileName = ! string.IsNullOrEmpty( fileName ) ? Path.GetFileName( fileName ) : "" ;
      _floorName = floorName ;
      _floorHeight = floorHeight ;
      _scale = scale ;
      _isEnabled = true ;
      _isDeleted = true ;
      _floorHeightDisplay = floorHeightDisplay.ToString( CultureInfo.InvariantCulture );
      _isEnabledFloorHeight = true ;
    }
    
    public ImportDwgMappingModel( Storable.Model.ImportDwgMappingModel item, bool isNotDeleted )
    {
      Id = item.Id ;
      _fullFilePath = item.FullFilePath ;
      _fileName = item.FileName ;
      _floorName = item.FloorName ;
      _floorHeight = item.FloorHeight ;
      _scale = item.Scale ;
      _isEnabled = false ;
      _isDeleted = isNotDeleted ;
      _floorHeightDisplay = item.FloorHeightDisplay.ToString( CultureInfo.InvariantCulture ) ;
      _isEnabledFloorHeight = true ;
    }
    
    public ImportDwgMappingModel(string id,string fileName, string floorName, double floorHeight, int scale, double floorHeightDisplay = 0 )
    {
      Id = id ;
      _fullFilePath = fileName ;
      _fileName = ! string.IsNullOrEmpty( fileName ) ? Path.GetFileName( fileName ) : "" ;
      _floorName = floorName ;
      _floorHeight = floorHeight ;
      _scale = scale ;
      _isEnabled = true ;
      _isDeleted = true ;
      _floorHeightDisplay = floorHeightDisplay.ToString( CultureInfo.InvariantCulture );
      _isEnabledFloorHeight = true ;
    }

    public static double GetDefaultSymbolMagnification( Document document )
    {
      var activeViewScale = document.ActiveView.Scale ;
      var defaultSymbolMagnification = activeViewScale * GetDefaultSymbolRatio( activeViewScale ) ;
      return defaultSymbolMagnification ;
    }
    
    public static double GetMagnificationOfView( int viewScale )
    {
      var defaultSymbolMagnification = viewScale * GetDefaultSymbolRatio( viewScale ) ;
      return defaultSymbolMagnification ;
    }
    
    private static double GetDefaultSymbolRatio( int scale )
    {
      return scale switch
      {
        > 0 and <= 20 => 200 / 100d,
        > 20 and <= 30 => 166.7 / 100d,
        > 30 and <= 60 => 133.3 / 100d,
        > 60 and <= 150 => 100 / 100d,
        > 150 and <= 500 => 76.7 / 100d,
        > 500 and <= 9999 => 50 / 100d,
        _ => 1
      } ;
    }
  }
}