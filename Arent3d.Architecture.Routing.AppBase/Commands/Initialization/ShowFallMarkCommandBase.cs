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
    private const double VerticalOffset = 0.1 ;
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      try {
        return document.Transaction(
          "TransactionName.Commands.Routing.ShowFallMark".GetAppStringByKeyOrDefault( "Show Fall Mark" ), _ =>
          {
            var fallMarkInstanceIds = GetExistedFallMarkInstancesIds( document ) ;
            if ( fallMarkInstanceIds.Count > 0 )
              document.Delete( fallMarkInstanceIds ) ; // remove marks are displaying
            else
              CreateFallMarkForConduitWithVerticalDirection( document ) ;
            return Result.Succeeded ;
          } ) ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private static void CreateFallMarkForConduitWithVerticalDirection( Document document )
    {
      var conduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var conduitWithZDirection = new List<Conduit>() ;
      foreach ( var conduit in conduits ) {
        var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitPosition.Curve as Line ) ! ;
        var conduitDirection = conduitLine.Direction ;
        if ( conduitDirection.Z is not (1.0 or -1.0) ) continue ;
        if ( ! conduitWithZDirection.Any( item => IsAlmostEqual(
              ( ( item.Location as LocationCurve )!.Curve as Line )!.Origin, conduitLine.Origin ) ) )
          conduitWithZDirection.Add( conduit ) ;
      }

      if ( ! conduitWithZDirection.Any() ) return ;
      var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ).FirstOrDefault() ??
                   throw new InvalidOperationException() ;
      symbol.TryGetProperty( "Lenght", out double lenghtMark ) ;
      foreach ( var conduit in conduitWithZDirection ) {
        GenerateFallMarks( document, symbol, conduit ) ;
      }
    }

    private static void GenerateFallMarks( Document document, FamilySymbol symbol, Conduit conduit )
    {
      var level = conduit.ReferenceLevel ;
      var height = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() + VerticalOffset ;
      var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
      var conduitLine = ( conduitPosition.Curve as Line ) ! ;
      symbol.Instantiate( new XYZ( conduitLine.Origin.X, conduitLine.Origin.Y, height ), level,
        StructuralType.NonStructural ) ;
    }

    private static bool IsAlmostEqual( XYZ a, XYZ b )
    {
      return Math.Abs( a.X - b.X ) <= GeometryUtil.Tolerance && Math.Abs( a.Y - b.Y ) <= GeometryUtil.Tolerance &&
             Math.Abs( a.Z - b.Z ) <= GeometryUtil.Tolerance ;
    }

    private static List<ElementId> GetExistedFallMarkInstancesIds( Document document )
    {
      var fallMarkSymbols = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ) ??
                            throw new InvalidOperationException() ;
      return document.GetAllFamilyInstances( fallMarkSymbols ).Select( item => item.Id ).ToList() ;
    }
  }
}