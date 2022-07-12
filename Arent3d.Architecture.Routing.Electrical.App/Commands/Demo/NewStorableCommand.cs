using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Demo
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Demo.NewStorableCommand", DefaultString = "New Storable" )]
  [Image( "resources/Initialize-16.bmp", ImageType = Revit.UI.ImageType.Normal )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class NewStorableCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      try {
        var document = commandData.Application.ActiveUIDocument.Document ;
        var selection = commandData.Application.ActiveUIDocument.Selection ;

        var firstPoint = selection.PickPoint() ;
        var secondPoint = selection.PickPoint() ;

        using var trans = new Transaction( document ) ;
        trans.Start( "New Curve" ) ;

        var detailCurve = document.Create.NewDetailCurve( document.ActiveView, Line.CreateBound( firstPoint, secondPoint ) ) ;

        var schemaId = new Guid("B333D4A3-4F67-4879-9308-935030691324") ;
        const string uniqueIdDetailCurveField = "UniqueIdDetailCurveField" ;
        
        var schema = FindOrCreateSchema( schemaId, uniqueIdDetailCurveField ) ;
        var entity = new Entity( schema ) ;
        var field = schema.GetField( uniqueIdDetailCurveField ) ;
        
        entity.Set(field, detailCurve.UniqueId);
        detailCurve.SetEntity(entity);

        trans.Commit() ;

        var uniqueIds = GetUniqueIds( document, schemaId, uniqueIdDetailCurveField ) ;
        TaskDialog.Show( "Arent", "Unique ID of the detail curve\n" + string.Join( "\n", uniqueIds ) ) ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        message = exception.Message ;
        return Result.Failed ;
      }
    }

    private List<string> GetUniqueIds(Document document, Guid schemaId, string fieldName)
    {
      var uniqueIds = new List<string>() ;
      var schema = Schema.Lookup( schemaId ) ;
      if ( null == schema )
        return uniqueIds ;

      var detailCurves = document.GetAllInstances<CurveElement>() ;
      foreach ( var detailCurve in detailCurves ) {
        var entity = detailCurve.GetEntity( schema ) ;
        if(!entity.IsValid())
          continue;
        
        var field = schema.GetField( fieldName ) ;
        if(null == field)
          continue;

        var value = entity.Get<string>( field ) ;
        if(string.IsNullOrEmpty(value))
          continue;
        
        uniqueIds.Add(value);
      }

      return uniqueIds ;
    }

    private static Schema FindOrCreateSchema(Guid schemaId, string fieldName)
    {
      var schema = Schema.Lookup( schemaId ) ;
      if ( null != schema )
        return schema ;

      var schemaBuilder = new SchemaBuilder( schemaId ) ;
      schemaBuilder.SetReadAccessLevel( AccessLevel.Public ) ;
      schemaBuilder.SetWriteAccessLevel( AccessLevel.Vendor ) ;
      schemaBuilder.SetVendorId( "com.arent3d" ) ;
      schemaBuilder.SetSchemaName( "DemoSchema" ) ;

      var fieldBuilder = schemaBuilder.AddSimpleField( fieldName, typeof( string ) ) ;
      fieldBuilder.SetDocumentation( "Save ID of the detail curve when you creating." ) ;

      return schemaBuilder.Finish() ;
    }
    
  }
}