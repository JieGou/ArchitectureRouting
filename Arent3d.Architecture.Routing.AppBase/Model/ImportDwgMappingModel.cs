﻿using System ;
using System.ComponentModel ;
using System.IO ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
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

    public static double GetDefaultSymbolMagnification( Document document )
    {
      var activeViewScale = document.ActiveView.Scale ;
      var defaultSymbolMagnification = activeViewScale * SetRatio( activeViewScale ) ;
      return defaultSymbolMagnification ;
    }
    
    private static double SetRatio( int scale )
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