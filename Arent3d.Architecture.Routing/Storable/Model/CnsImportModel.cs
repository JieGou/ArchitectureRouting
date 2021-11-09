using System ;
using System.ComponentModel;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CnsImportModel
  {
    private int _sequen;
    public int Sequen 
    { 
      get=>_sequen;
      set
      {
        _sequen = value;
      }
    }
  
    public string CategoryName { get ; set ; }

    public CnsImportModel(int sequen, string categoryName)
    {
      Sequen = sequen;
      CategoryName = categoryName;
    }
    
    public bool Equals( CnsImportModel other )
    {
      return other != null &&
             Sequen == other.Sequen &&
             CategoryName == other.CategoryName ;
    }
  }
}