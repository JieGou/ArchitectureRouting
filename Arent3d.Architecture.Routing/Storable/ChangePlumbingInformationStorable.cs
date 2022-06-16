using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "e292ef5e-2a11-4fc9-aa2e-367f7048e723" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class ChangePlumbingInformationStorable : StorableBase
  {
    public const string StorableName = "Change Plumbing Information Model" ;
    private const string ChangePlumbingInformationModelField = "ChangePlumbingInformationModel" ;
    
    public List<ChangePlumbingInformationModel> ChangePlumbingInformationModelData { get ; set ; }
    
    public ChangePlumbingInformationStorable( DataStorage owner ) : base( owner, false )
    {
      ChangePlumbingInformationModelData = new List<ChangePlumbingInformationModel>() ;
    }

    public ChangePlumbingInformationStorable( Document document ) : base( document, false )
    {
      ChangePlumbingInformationModelData = new List<ChangePlumbingInformationModel>() ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      ChangePlumbingInformationModelData = reader.GetArray<ChangePlumbingInformationModel>( ChangePlumbingInformationModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( ChangePlumbingInformationModelField, ChangePlumbingInformationModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<ChangePlumbingInformationModel>( ChangePlumbingInformationModelField ) ;
    }

    public override string Name => StorableName ;
  }
}