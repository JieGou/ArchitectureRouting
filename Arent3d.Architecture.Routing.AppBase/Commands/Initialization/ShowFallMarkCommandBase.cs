using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowFallMarkCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction(
          "TransactionName.Commands.Routing.ConfirmUnset".GetAppStringByKeyOrDefault( "Confirm Not Connect" ), _ =>
          {
            if ( ! HideFallMarks( document ) )
              CreateFallMarkForConduitWithZDirection( document ) ;
            return Result.Succeeded ;
          } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void CreateFallMarkForConduitWithZDirection( Document document )
    {
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var conduitWithZDirection = new List<Conduit>() ;
      foreach ( var conduit in conduits ) {
        var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitPosition.Curve as Line ) ! ;
        var conduitDirection = conduitLine.Direction ;
        if ( conduitDirection.Z is not (1.0 or -1.0) ) continue ;
        if ( ! conduitWithZDirection.Any( item => Equal(
              ( ( item.Location as LocationCurve )!.Curve as Line )!.Origin, conduitLine.Origin ) ) )
          conduitWithZDirection.Add( conduit ) ;
      }

      if ( ! conduitWithZDirection.Any() ) return ;
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ).FirstOrDefault() ??
                   throw new InvalidOperationException() ;
      symbol.TryGetProperty( "Lenght", out double lenghtMark ) ;
      foreach ( var conduit in conduitWithZDirection ) {
        GenerateMark( document, symbol, conduit ) ;
      }
    }

    private static void GenerateMark( Document document, FamilySymbol symbol, Conduit conduit )
    {
      var level = conduit.ReferenceLevel ;
      var height = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() + 0.1 ;
      var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
      var conduitLine = ( conduitPosition.Curve as Line ) ! ;
      symbol.Instantiate( new XYZ( conduitLine.Origin.X, conduitLine.Origin.Y, height ), level,
        StructuralType.NonStructural ) ;
    }

    private static bool Equal( XYZ a, XYZ b )
    {
      return Math.Abs( a.X - b.X ) <= GeometryUtil.Tolerance && Math.Abs( a.Y - b.Y ) <= GeometryUtil.Tolerance &&
             Math.Abs( a.Z - b.Z ) <= GeometryUtil.Tolerance ;
    }


    private static bool HideFallMarks( Document document )
    {
      var fallMarkSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ) ??
                            throw new InvalidOperationException() ;
      var fallMarkIds = document.GetAllFamilyInstances( fallMarkSymbols ).Select( item => item.Id ).ToList() ;
      if ( fallMarkIds.Count == 0 ) return false ;
      document.Delete( fallMarkIds ) ;
      return true ;
    }
  }
}