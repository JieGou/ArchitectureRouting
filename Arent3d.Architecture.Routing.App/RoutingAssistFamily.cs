// using System ;
// using System.Linq ;
// using Autodesk.Revit.DB ;
// using Autodesk.Revit.DB.Plumbing ;
// using Autodesk.Revit.UI ;
//
// namespace Arent3d.Architecture.Routing.App
// {
//   public static class RoutingAssistFamily
//   {
//     private const string RoutingAssistPointFamilyName = "Arent-Mechanical Equipment-Pointer" ;
//
//     public static void SetAssistFamily( this Document document, UIDocument uidoc )
//     {
//       Element e = GetSelectedElement( document, uidoc ) ;
//
//       LocationCurve? elCurve = e.Location as LocationCurve ;
//       Line? l = elCurve.Curve as Line ;
//       XYZ direction = l.Direction ;
//
//       XYZ p1 = l.GetEndPoint( 0 ) ;
//       XYZ p2 = l.GetEndPoint( 1 ) ;
//       XYZ mid = ( p1 + p2 ) / 2 ;
//       XYZ pZAxis = new XYZ( mid.X, mid.Y, mid.Z + 5 ) ;
//       XYZ pXAxis = new XYZ( mid.X + 5, mid.Y, mid.Z ) ;
//
//       XYZ vertical_p1 = new XYZ( p1.Y, p1.X * -1, p1.Z ) ;
//       XYZ vertical_p2 = new XYZ( p2.Y, p2.X * -1, p2.Z ) ;
//
//       double angle_xy = GetAngle( e, direction, p1 ) ;
//       double angle_z = Math.Acos( direction.Z ) ;
//
//       Element pointer = new FilteredElementCollector( document ).OfCategory( BuiltInCategory.OST_MechanicalEquipment ).OfClass( typeof( FamilySymbol ) ).First( x => ( (FamilySymbol) x ).FamilyName == RoutingAssistPointFamilyName ) ;
//       Element? elf = null ;
//       using ( TransactionGroup tg = new TransactionGroup( document ) ) {
//         tg.Start( "Set Route Assist Family" ) ;
//
//         using ( Transaction tx = new Transaction( document ) ) {
//           tx.Start( "Set FamilyInstance" ) ;
//           FamilySymbol? p = pointer as FamilySymbol ;
//           if ( p.IsActive != true ) {
//             p.Activate() ;
//           }
//
//           FamilyInstance f = document.Create.NewFamilyInstance( mid, p, Autodesk.Revit.DB.Structure.StructuralType.NonStructural ) ;
//           elf = f as Element ;
//           elf.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;
//           Pipe? pipe_e = e as Pipe ;
//           elf.LookupParameter( "Arent-RoundDuct-Diameter" ).Set( pipe_e.Diameter ) ;
//           tx.Commit() ;
//         }
//
//         using ( Transaction tx = new Transaction( document ) ) {
//           tx.Start( "Rotate FamilyInstance" ) ;
//           Line axis_z = Line.CreateBound( mid, pZAxis ) ;
//           Line axis_x = Line.CreateBound( mid, pXAxis ) ;
//           Line axis_l = Line.CreateBound( vertical_p1, vertical_p2 ) ;
//           ElementTransformUtils.RotateElement( document, elf.Id, axis_z, angle_xy ) ;
//           ElementTransformUtils.RotateElement( document, elf.Id, axis_l, angle_z ) ;
//           tx.Commit() ;
//         }
//
//         tg.Assimilate() ;
//       }
//     }
//
//     public static Element GetSelectedElement( Document document, UIDocument uidoc )
//     {
//       ICollection<ElementId> els = uidoc.Selection.GetElementIds() ;
//
//       if ( els.Count != 1 ) {
//         Environment.Exit( 1 ) ;
//       }
//
//       Element e = document.GetElement( els.First() ) ;
//       return e ;
//     }
//
//     public static double GetAngle( Element e, XYZ direction, XYZ p1 )
//     {
//       double angle_xy = Math.Acos( direction.X ) ;
//
//       XYZ c1 = new XYZ() ;
//       Connector? cn = null ;
//
//       MEPCurve? mep_e = e as MEPCurve ;
//       ConnectorSet cs = mep_e.ConnectorManager.Connectors ;
//       foreach ( Connector c in cs ) {
//         if ( c.Id == 0 ) {
//           c1 = c.Origin ;
//           cn = c ;
//         }
//       }
//
//       if ( c1.IsAlmostEqualTo( p1 ) == true ) {
//         if ( cn.Direction == FlowDirectionType.Out ) {
//           // TaskDialog.Show("message", "p1 Out: " + angle_xy.ToString());
//           angle_xy += Math.PI ;
//         }
//
//         // else { TaskDialog.Show("message", "p1 In: " + angle_xy.ToString()); }
//       }
//       else {
//         if ( cn.Direction == FlowDirectionType.In ) {
//           // TaskDialog.Show("message", "p2 In: " + angle_xy.ToString());
//           angle_xy += Math.PI ;
//         }
//
//         // else { TaskDialog.Show("message", "p2 Out: " + angle_xy.ToString()); }
//       }
//
//       return angle_xy ;
//     }
//   }
// }