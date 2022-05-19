using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using System;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class ComponentHelper
  {
    public static readonly Dictionary<int, string> ComponentNames = new()
    {
      { 01, "漏水帯（布）" },
      { 02, "漏水帯（発色）" },
      { 03, "漏水帯（塩ビ）" }
    } ;

    public static void InitialComponent(Document document)
    {
      var elementTypes = document.GetAllTypes<ElementType>( x => x.FamilyName == "Repeating Detail" ) ;
      if(!elementTypes.Any())
        return;

      var defaultType = elementTypes.First() ;

      using var transaction = new Transaction( document ) ;
      transaction.Start( "Create Repeat Component" ) ;
      
      var repeatType01 = elementTypes.FirstOrDefault( x => x.Name == ComponentNames[ 01 ] ) ;
      if ( null == repeatType01 ) {
        repeatType01 = defaultType.Duplicate( ComponentNames[ 01 ] ) ;
        var familySymbol01 = document.GetFamilySymbols( ElectricalRoutingFamilyType.DetailComponent01 ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        SetupRepeatType( repeatType01, familySymbol01 ) ;
      }
      
      var repeatType02 = elementTypes.FirstOrDefault( x => x.Name == ComponentNames[ 02 ] ) ;
      if ( null == repeatType02 ) {
        repeatType02 = defaultType.Duplicate( ComponentNames[ 02 ] ) ;
        var familySymbol02 = document.GetFamilySymbols( ElectricalRoutingFamilyType.DetailComponent02 ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        SetupRepeatType( repeatType02, familySymbol02 ) ;
      }
      
      var repeatType03 = elementTypes.FirstOrDefault( x => x.Name == ComponentNames[ 03 ] ) ;
      if ( null == repeatType03 ) {
        repeatType03 = defaultType.Duplicate( ComponentNames[ 03 ] ) ;
        var familySymbol03 = document.GetFamilySymbols( ElectricalRoutingFamilyType.DetailComponent03 ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        SetupRepeatType( repeatType03, familySymbol03 ) ;
      }

      transaction.Commit() ;
    }

    private static void SetupRepeatType( ElementType elementType, FamilySymbol familySymbol )
    {
      if ( elementType.get_Parameter( BuiltInParameter.REPEATING_DETAIL_ELEMENT ) is { } detail ) {
        detail.Set( familySymbol.Id ) ;
      }
      
      if ( elementType.get_Parameter( BuiltInParameter.REPEATING_DETAIL_ROTATION ) is { } rotation ) {
        rotation.Set( 1 ) ;
      }
      
      if ( elementType.get_Parameter( BuiltInParameter.REPEATING_DETAIL_INSIDE ) is { } inside ) {
        inside.Set( 1 ) ;
      }
      
      if ( elementType.get_Parameter( BuiltInParameter.REPEATING_DETAIL_LAYOUT ) is { } layout ) {
        layout.Set( 3 ) ;
      }
      
      if ( elementType.get_Parameter( BuiltInParameter.REPEATING_DETAIL_SPACING ) is { } spacing ) {
        spacing.Set( 300d.MillimetersToRevitUnits() ) ;
      }
    }
  }
}