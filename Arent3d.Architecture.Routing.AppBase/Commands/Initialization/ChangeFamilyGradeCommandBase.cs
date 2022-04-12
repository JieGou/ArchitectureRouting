using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ChangeFamilyGradeCommandBase : IExternalCommand
  {
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var ceedStorable = document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable == null ) return Result.Failed ;

      var floorPlans = ceedStorable.CeedModelData.Select( item => item.FloorPlanType ).GroupBy( item => item )
        .Select( item => item.Key ).Where( item => ! string.IsNullOrEmpty( item ) ).ToList() ;

      var connectorOneSideFamilyTypeNames =
        ( (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) )
        .Select( f => f.GetFieldName() ).ToHashSet() ;

      var instances = new List<FamilyInstance>() ;

      foreach ( var item in floorPlans ) {
        // get symbols
        var symbol = new List<FamilySymbol>() ;
        if ( connectorOneSideFamilyTypeNames.Contains( item ) ) {
          var connectorOneSideFamilyType = GetConnectorFamilyType( item ) ;
          symbol.Add( document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ??
                      ( document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ??
                        throw new InvalidOperationException() ) ) ;
        }
        else {
          if ( new FilteredElementCollector( document ).OfClass( typeof( Family ) )
                .FirstOrDefault( f => f.Name == item ) is Family family ) {
            symbol.AddRange( from familySymbolId in family.GetFamilySymbolIds()
              select document.GetElementById<FamilySymbol>( familySymbolId ) ??
                     throw new InvalidOperationException() ) ;
          }
        }
        
        // find instances
        instances.AddRange( document.GetAllFamilyInstances( symbol )
          .Where( instance => instance.HasParameter( "グレード3" ) ).ToList() ) ;
      }

      // update property グレード3 of instances
      using Transaction t = new(document, "Update grade") ;
      t.Start() ;
      var valueToUpdate = ! instances.Any( item => item.GetPropertyBool( "グレード3" ) ) ;
      foreach ( var instance in instances )
        instance.SetProperty( "グレード3", valueToUpdate ) ;
      t.Commit() ;

      return Result.Succeeded ;
    }

    private static ConnectorOneSideFamilyType GetConnectorFamilyType( string floorPlanType )
    {
      var connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      if ( string.IsNullOrEmpty( floorPlanType ) ) return connectorOneSideFamilyType ;
      foreach ( var item in (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ) {
        if ( floorPlanType == item.GetFieldName() ) connectorOneSideFamilyType = item ;
      }

      return connectorOneSideFamilyType ;
    }
  }
}