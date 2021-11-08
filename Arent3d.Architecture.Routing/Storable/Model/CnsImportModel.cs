using System ;
using Arent3d.Architecture.Routing.Utils ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storable.Model
{
  public class CnsImportModel
  {
    public int Sequen { get ; set ; }
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