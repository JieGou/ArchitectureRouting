using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Arent3d.Revit.Csv ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.FittingSizeCalculators.MEPCurveGenerators;
using Autodesk.Revit.DB.Electrical;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class NewRackCommandBase : IExternalCommand
  {
    //private static readonly double DefaultThickness = 200.0 ;
    //private static readonly double DefaultWidth = 100.0 ;
    private static readonly double DefaultHeight = 4000.0 ;
    protected abstract AddInType GetAddInType() ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var pickFrom = PointOnRoutePicker.PickRoute(uiDocument, false, "Pick a point on a route to delete.", GetAddInType());
        var pickTo = PointOnRoutePicker.PickRoute(uiDocument, false, "Pick a point on a route to delete.", GetAddInType());

        if ( null == pickFrom.Position || null == pickTo.Position || null == pickFrom.RouteDirection ||
             null == pickTo.RouteDirection) {
          return Result.Failed ;
        }

        //var sv = new SetRackProperty() ;
        //sv.UpdateParameters( DefaultHeight, DefaultThickness ) ;
        //sv.ShowDialog() ;
        //if ( true != sv.DialogResult ) {
        //  return Result.Failed ;
        //}

        var result = document.Transaction(
          "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Rack" ), _ =>
          {
              var routeName = RoutingElementExtensions.GetRouteName(pickFrom.Element);
              if (routeName != null)
              {
                  var elements = document.GetAllElementsOfRouteName<Element>(routeName);
                  var elements2 = document.GetAllElementsOfRouteName<MEPCurve>(routeName);
                  foreach (var e1 in elements)
                  {
                      foreach (var par in e1.ParametersMap)
                      {
                          var v = ((Autodesk.Revit.DB.Parameter)par).Definition.Name;
                          var v2 = e1.ParametersMap.get_Item(((Autodesk.Revit.DB.Parameter)par).Definition.Name).AsValueString();

                      }
                      FilteredElementCollector collector1 = new FilteredElementCollector(document);
                      ICollection<Element> collection = collector1.WhereElementIsNotElementType().ToElements();

                      var fsym = new FilteredElementCollector(document)
                                   .OfClass(typeof(FamilySymbol))
                                   .Cast<FamilySymbol>()
                                   .Where(x=>x.GetBuiltInCategory() == BuiltInCategory.OST_CableTrayFitting)
                                   .ToList();

                      var names = fsym.Select(x => x.FamilyName).ToList();
                      foreach (FamilySymbol symbq in fsym)
                      {

                      }

                      // 
                      if (e1 is Conduit)
                      {
                          var conduit = (e1 as Conduit)!;
                          var location = (e1.Location as LocationCurve)!;
                          var line = location.Curve as Autodesk.Revit.DB.Line;

                          var length = conduit.ParametersMap.get_Item("Length").AsDouble();
                          var bounding = conduit.get_BoundingBox(uiDocument.ActiveView)!;
                          var endPos = line!.Origin.Multiply(length);
                          // CableTray.Create(document, e1.Id, endPos, endPos, uiDocument.ActiveView.GenLevel.Id);

                          var symbol = fsym.FirstOrDefault()!;

                          var instance = symbol.Instantiate(
                                            new XYZ(line!.Origin.X, line!.Origin.Y, DefaultHeight.MillimetersToRevitUnits()),
                                            uiDocument.ActiveView.GenLevel, StructuralType.NonStructural);

                          if (line.Direction.X == 1.0)
                          {
                              //SetParameter(instance, "幅", length);
                              //SetParameter(instance, "高さ", DefaultThickness.MillimetersToRevitUnits());
                              //SetParameter(instance, "奥行き", DefaultWidth.MillimetersToRevitUnits());
                          } else
                          {
                              //SetParameter(instance, "幅", DefaultWidth.MillimetersToRevitUnits());
                              //SetParameter(instance, "高さ", DefaultThickness.MillimetersToRevitUnits());
                              //SetParameter(instance, "奥行き", length);
                          }
                          //SetParameter(instance, "Length", length);

                      } else
                      {
                          //var conduit = (e1 as FamilyInstance)!;
                          //var location = (e1.Location as LocationCurve)!;
                          //var line = location.Curve as Autodesk.Revit.DB.Line;

                          //var length = conduit.ParametersMap.get_Item("Length").AsDouble();
                          //var bounding = conduit.get_BoundingBox(uiDocument.ActiveView)!;
                          //var endPos = line!.Origin.Multiply(length);
                          //// CableTray.Create(document, e1.Id, endPos, endPos, uiDocument.ActiveView.GenLevel.Id);

                          //var symbol = uiDocument.Document.GetFamilySymbol(RoutingFamilyType.RackGuide)!;

                          //var instance = symbol.Instantiate(
                          //                  line!.Origin,
                          //                  uiDocument.ActiveView.GenLevel, StructuralType.NonStructural);

                          //SetParameter(instance, "幅", DefaultWidth.MillimetersToRevitUnits());
                          //SetParameter(instance, "高さ", DefaultThickness.MillimetersToRevitUnits());
                          //SetParameter(instance, "奥行き", length);
                      }
                  }
              }

            return Result.Succeeded ;
          } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }
    private static (XYZ? Position, XYZ? Direction) GetPositionAndDirection( Element elm, XYZ position )
    {
      return elm switch
      {
        MEPCurve curve => GetNearestPointAndDirection( curve, position ),
        FamilyInstance fi => ToPositionAndDirection( fi.GetTotalTransform() ),
        _ => ( null, null ),
      } ;
        }
        private static (XYZ? Position, XYZ? Direction) GetNearestPointAndDirection(MEPCurve curve, XYZ position)
        {
            var from = curve.GetRoutingConnectors(true).FirstOrDefault();
            if (null == from) return (null, null);
            var to = curve.GetRoutingConnectors(false).FirstOrDefault();
            if (null == to) return (null, null);

            var o = from.Origin.To3dRaw();
            var dir = to.Origin.To3dRaw() - o;
            var tole = curve.Document.Application.VertexTolerance;
            if (dir.sqrMagnitude < tole * tole) return (null, null);

            var line = new MathLib.Line(o, dir);
            var dist = line.DistanceTo(position.To3dRaw(), 0);
            return (Position: dist.PointOnSelf.ToXYZRaw(), Direction: dir.normalized.ToXYZRaw());
        }

        private static (XYZ Position, XYZ Direction) ToPositionAndDirection(Transform transform)
        {
            return (transform.Origin, transform.BasisX);
        }
        private static void GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ,
      double sizeX, double sizeY, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbol( RoutingFamilyType.RackGuide )! ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }

    private static Level? GetUpperLevel( Level refRevel )
    {
      var minElevation = refRevel.Elevation + refRevel.Document.Application.ShortCurveTolerance ;
      return refRevel.Document.GetAllElements<Level>().Where( level => minElevation < level.Elevation )
        .MinItemOrDefault( level => level.Elevation ) ;
    }

    private static void SetParameter( FamilyInstance instance, string parameterName, double value )
    {
      instance.ParametersMap.get_Item( parameterName )?.Set( value ) ;
    }

    private static void SetParameter( FamilyInstance instance, BuiltInParameter parameter, double value )
    {
      instance.get_Parameter( parameter )?.Set( value ) ;
    }
  }
}