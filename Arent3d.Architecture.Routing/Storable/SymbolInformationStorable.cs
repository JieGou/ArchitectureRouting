using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "1c92fa8b-4a0d-4070-82b2-5862de4a656d" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class SymbolInformationStorable : StorableBase
  {
    public const string StorableName = "Symbol Information Model" ;
    private const string AllSymbolInformationModelField = "AllSymbolInformationModel" ;
    public List<SymbolInformationModel> AllSymbolInformationModelData { get ; set ; }

    public SymbolInformationStorable( DataStorage owner ) : base( owner, false )
    {
      AllSymbolInformationModelData = new List<SymbolInformationModel>() ;
    }

    public SymbolInformationStorable( Document document ) : base( document, false )
    {
      AllSymbolInformationModelData = new List<SymbolInformationModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      AllSymbolInformationModelData = reader.GetArray<SymbolInformationModel>( AllSymbolInformationModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( AllSymbolInformationModelField, AllSymbolInformationModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<SymbolInformationModel>( AllSymbolInformationModelField ) ;
    }

    public override string Name => StorableName ;
  }
}