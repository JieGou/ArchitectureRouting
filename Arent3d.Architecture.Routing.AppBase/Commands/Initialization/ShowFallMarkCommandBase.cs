using System ;
using System.Collections.Generic ;
using System.Linq ;
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
      var conduitWithZDirection = new List<XYZ>() ;
      foreach ( var conduit in conduits ) {
        var conduitPosition = ( conduit.Location as LocationCurve ) ! ;
        var conduitLine = ( conduitPosition.Curve as Line ) ! ;
        var conduitDirection = conduitLine.Direction ;
        if ( conduitDirection.Z is not (1.0 or -1.0) ) continue ;
        var conduitOrigin = conduitLine.Origin ;
        if ( conduitWithZDirection.All( item =>
              Math.Abs( item.X - conduitOrigin.X ) > GeometryUtil.Tolerance ||
              Math.Abs( item.Y - conduitOrigin.Y ) > GeometryUtil.Tolerance ||
              Math.Abs( item.Z - conduitOrigin.Z ) > GeometryUtil.Tolerance ) )
          conduitWithZDirection.Add( conduitOrigin ) ;
      }

      if ( ! conduitWithZDirection.Any() ) return ;
      var fallMarkSymbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.FallMark ).FirstOrDefault() ??
                           throw new InvalidOperationException() ;
      fallMarkSymbol.TryGetProperty( "Lenght", out double lenghtMark ) ;
      foreach ( var (x, y, z) in conduitWithZDirection )
        fallMarkSymbol.Instantiate( new XYZ( x, y - lenghtMark / 2, z ), StructuralType.NonStructural ) ;
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