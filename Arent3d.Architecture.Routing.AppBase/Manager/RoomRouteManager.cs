using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Manager
{
  public static class RoomRouteManager
  {
    public static Reference? PickRoom( UIDocument uiDocument )
    {
      RoomPickFilter roomPickFilter = new() ;
      Reference? element = null ;
      FamilyInstance? room = null ;
      while ( room == null ) {
        MessageBox.Show( "Please select door position", "Message" ) ;
        element = uiDocument.Selection.PickObject( ObjectType.Element, roomPickFilter, "Select room." ) ;
        room = uiDocument.Document.GetAllFamilyInstances( ElectricalRoutingFamilyType.Room ).FirstOrDefault( r => r.Id == element.ElementId ) ;
      }

      return element ;
    }

    public enum RoomEdge
    {
      Left,
      Right,
      Front,
      Back,
      LeftFrontCorner,
      RightFrontCorner,
      LeftBackCorner,
      RightBackCorner,
      Other
    }

    public static ( List<FamilyInstance>, XYZ ) InsertPassPointElement( Document document, string routeName, ElementId? levelId, double radius, Reference room, FixedHeight? fromFixedHeight, bool isOut, string fromConnectorId, string toConnectorId )
    {
      const string thicknessParam = "Thickness" ;
      const string leftDoorDistanceParam = "Left Door Distance" ;
      const string leftDoorWidthParam = "Left Door Width" ;
      const string rightDoorDistanceParam = "Right Door Distance" ;
      const string rightDoorWidthParam = "Right Door Width" ;
      const string frontDoorDistanceParam = "Front Door Distance" ;
      const string frontDoorWidthParam = "Front Door Width" ;
      const string backDoorDistanceParam = "Back Door Distance" ;
      const string backDoorWidthParam = "Back Door Width" ;
      var maxThickness = ( 400.0 ).MillimetersToRevitUnits() ;
      var thicknessDefault = ( 200.0 ).MillimetersToRevitUnits() ;
      var widthDoor = ( 300.0 ).MillimetersToRevitUnits() ;
      // get room properties
      var element = document.GetElement( room.ElementId ) ;
      var lenght = element.ParametersMap.get_Item( "Lenght" ).AsDouble() ;
      var width = element.ParametersMap.get_Item( "Width" ).AsDouble() ;
      var thickness = element.ParametersMap.get_Item( thicknessParam ).AsDouble() ;
      var leftDoorDistance = element.ParametersMap.get_Item( leftDoorDistanceParam ).AsDouble() ;
      var rightDoorDistance = element.ParametersMap.get_Item( rightDoorDistanceParam ).AsDouble() ;
      var frontDoorDistance = element.ParametersMap.get_Item( frontDoorDistanceParam ).AsDouble() ;
      var backDoorDistance = element.ParametersMap.get_Item( backDoorDistanceParam ).AsDouble() ;
      var locationPoint = ( element.Location as LocationPoint ) ! ;
      var p1 = locationPoint.Point ;
      var p2 = new XYZ( p1.X + lenght, p1.Y, p1.Z ) ;
      var p3 = new XYZ( p2.X, p2.Y - width, p2.Z ) ;
      var p4 = new XYZ( p1.X, p1.Y - width, p1.Z ) ;
      
      // get pass point height
      IList<Element> levels = new FilteredElementCollector( document ).OfClass( typeof( Level ) ).ToElements() ;
      if ( levels.FirstOrDefault( l => l.Id == levelId ) == null ) throw new InvalidOperationException() ;
      var level = levels.FirstOrDefault( l => l.Id == levelId ) as Level ;
      var height = fromFixedHeight?.Height ?? 0 ;
      height += level!.Elevation ;
      
      XYZ position = new( room.GlobalPoint.X, room.GlobalPoint.Y, height ) ;
      XYZ position2 = new( room.GlobalPoint.X, room.GlobalPoint.Y, height ) ;
      Vector3d direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
      var (edgeRoom, (x, y, _)) = GetRoomEdgeInsertPassPoint( room.GlobalPoint, thickness, p1, p2, p3, p4 ) ;
      if ( thickness > maxThickness ) {
        element.ParametersMap.get_Item( thicknessParam ).Set( maxThickness ) ;
        thickness = maxThickness ;
      }
      
      switch ( edgeRoom ) {
        case RoomEdge.Left :
          direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          if ( leftDoorDistance == 0 ) {
            position = new XYZ( x, room.GlobalPoint.Y, height ) ;
            position2 = new XYZ( x + thickness, room.GlobalPoint.Y, height ) ;
            element.ParametersMap.get_Item( leftDoorDistanceParam ).Set( Math.Abs( room.GlobalPoint.Y - y ) - widthDoor / 2 ) ;
            element.ParametersMap.get_Item( leftDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = y < 0 ? y + leftDoorDistance + widthDoor / 2 : y - leftDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x + thickness, yPoint, height ) ;
          }

          break ;
        case RoomEdge.Right :
          direction = isOut ? new Vector3d( -1, 0, 0 ) : new Vector3d( 1, 0, 0 ) ;
          if ( rightDoorDistance == 0 ) {
            position = new XYZ( x, room.GlobalPoint.Y, height ) ;
            position2 = new XYZ( x - thickness, room.GlobalPoint.Y, height ) ;
            element.ParametersMap.get_Item( rightDoorDistanceParam ).Set( Math.Abs( room.GlobalPoint.Y - y ) - widthDoor / 2 ) ;
            element.ParametersMap.get_Item( rightDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = y < 0 ? y + rightDoorDistance + widthDoor / 2 : y - rightDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x - thickness, yPoint, height ) ;
          }

          break ;
        case RoomEdge.Front :
          direction = isOut ? new Vector3d( 0, 1, 0 ) : new Vector3d( 0, -1, 0 ) ;
          if ( frontDoorDistance == 0 ) {
            position = new XYZ( room.GlobalPoint.X, y, height ) ;
            position2 = new XYZ( room.GlobalPoint.X, y + thickness, height ) ;
            element.ParametersMap.get_Item( frontDoorDistanceParam ).Set( Math.Abs( room.GlobalPoint.X - x ) - widthDoor / 2 ) ;
            element.ParametersMap.get_Item( frontDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var xPoint = x > 0 ? x + frontDoorDistance + widthDoor / 2 : x - frontDoorDistance - widthDoor / 2 ;
            position = new XYZ( xPoint, y, height ) ;
            position2 = new XYZ( xPoint, y + thickness, height ) ;
          }

          break ;
        case RoomEdge.Back :
          direction = isOut ? new Vector3d( 0, -1, 0 ) : new Vector3d( 0, 1, 0 ) ;
          if ( backDoorDistance == 0 ) {
            position = new XYZ( room.GlobalPoint.X, y, height ) ;
            position2 = new XYZ( room.GlobalPoint.X, y - thickness, height ) ;
            element.ParametersMap.get_Item( backDoorDistanceParam ).Set( Math.Abs( room.GlobalPoint.X - x ) - widthDoor / 2 ) ;
            element.ParametersMap.get_Item( backDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var xPoint = x > 0 ? x + backDoorDistance + widthDoor / 2 : x - backDoorDistance - widthDoor / 2 ;
            position = new XYZ( xPoint, y, height ) ;
            position2 = new XYZ( xPoint, y - thickness, height ) ;
          }

          break ;
        case RoomEdge.LeftFrontCorner :
          direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          if ( leftDoorDistance == 0 ) {
            position = new XYZ( x, y + widthDoor / 2, height ) ;
            position2 = new XYZ( x + thickness, y + widthDoor / 2, height ) ;
            element.ParametersMap.get_Item( leftDoorDistanceParam ).Set( width - thickness - widthDoor ) ;
            element.ParametersMap.get_Item( leftDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = p1.Y < 0 ? p1.Y +leftDoorDistance + widthDoor / 2 : p1.Y - leftDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x + thickness, yPoint, height ) ;
          }

          break ;
        case RoomEdge.RightFrontCorner :
          direction = isOut ? new Vector3d( -1, 0, 0 ) : new Vector3d( 1, 0, 0 ) ;
          if ( rightDoorDistance == 0 ) {
            position = new XYZ( x, y + widthDoor / 2, height ) ;
            position2 = new XYZ( x - thickness, y + widthDoor / 2, height ) ;
            element.ParametersMap.get_Item( rightDoorDistanceParam ).Set( width - thickness - widthDoor ) ;
            element.ParametersMap.get_Item( rightDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = p2.Y < 0 ? p2.Y + rightDoorDistance + widthDoor / 2 : p2.Y - rightDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x - thickness, yPoint, height ) ;
          }

          break ;
        case RoomEdge.LeftBackCorner :
          direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          if ( leftDoorDistance == 0 ) {
            position = new XYZ( x, y - widthDoor / 2, height ) ;
            position2 = new XYZ( x + thickness, y - widthDoor / 2, height ) ;
            element.ParametersMap.get_Item( leftDoorDistanceParam ).Set( thickness ) ;
            element.ParametersMap.get_Item( leftDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = p1.Y < 0 ? p1.Y + leftDoorDistance + widthDoor / 2 : p1.Y - leftDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x + thickness, yPoint, height ) ;
          }

          break ;
        case RoomEdge.RightBackCorner :
          direction = isOut ? new Vector3d( -1, 0, 0 ) : new Vector3d( 1, 0, 0 ) ;
          if ( rightDoorDistance == 0 ) {
            position = new XYZ( x, y - widthDoor / 2, height ) ;
            position2 = new XYZ( x - thickness, y - widthDoor / 2, height ) ;
            element.ParametersMap.get_Item( rightDoorDistanceParam ).Set( thickness ) ;
            element.ParametersMap.get_Item( rightDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = p2.Y < 0 ? p2.Y + rightDoorDistance + widthDoor / 2 : p2.Y - rightDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x - thickness, yPoint, height ) ;
          }

          break ;
        case RoomEdge.Other :
          direction = isOut ? new Vector3d( 1, 0, 0 ) : new Vector3d( -1, 0, 0 ) ;
          if ( leftDoorDistance == 0 ) {
            position = new XYZ( x, y - widthDoor / 2, height ) ;
            position2 = new XYZ( x + thickness, y - widthDoor / 2, height ) ;
            element.ParametersMap.get_Item( leftDoorDistanceParam ).Set( thickness ) ;
            element.ParametersMap.get_Item( leftDoorWidthParam ).Set( widthDoor ) ;
          }
          else {
            var yPoint = p1.Y < 0 ? p1.Y + leftDoorDistance + widthDoor / 2 : p1.Y - leftDoorDistance - widthDoor / 2 ;
            position = new XYZ( x, yPoint, height ) ;
            position2 = new XYZ( x + thickness, yPoint, height ) ;
          }

          break ;
      }

      var passPoints = new List<FamilyInstance>() ;
      var passPoint = document.AddPassPoint( routeName, isOut ? position : position2, direction.normalized.ToXYZRaw(), radius, levelId! ) ;
      passPoint.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toConnectorId ) ;
      passPoint.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromConnectorId ) ;
      passPoints.Add( passPoint ) ;

      if ( ! ( thickness > thicknessDefault ) ) return ( passPoints, position ) ;
      var passPoint2 = document.AddPassPoint( routeName, isOut ? position2 : position, direction.normalized.ToXYZRaw(), radius, levelId! ) ;
      passPoint2.SetProperty( PassPointParameter.RelatedConnectorUniqueId, toConnectorId ) ;
      passPoint2.SetProperty( PassPointParameter.RelatedFromConnectorUniqueId, fromConnectorId ) ;
      passPoints.Add( passPoint2 ) ;

      return ( passPoints, isOut ? position2 : position ) ;
    }

    private static ( RoomEdge, XYZ ) GetRoomEdgeInsertPassPoint( XYZ passPoint, double thickness, XYZ p1, XYZ p2, XYZ p3, XYZ p4 )
    {
      const double errorRange = 0.001 ;
      if ( passPoint.X >= p1.X - errorRange && passPoint.X <= p1.X + thickness + errorRange && p4.Y + thickness <= passPoint.Y && passPoint.Y <= p1.Y - thickness )
        return ( RoomEdge.Left, p1 ) ;
      if ( passPoint.X >= p2.X - thickness - errorRange && passPoint.X <= p2.X + errorRange && p3.Y + thickness <= passPoint.Y && passPoint.Y <= p2.Y - thickness )
        return ( RoomEdge.Right, p2 ) ;
      if ( passPoint.Y >= p4.Y - errorRange && passPoint.Y <= p4.Y + thickness + errorRange && p4.X + thickness <= passPoint.X && passPoint.X <= p3.X - thickness )
        return ( RoomEdge.Front, p4 ) ;
      if ( passPoint.Y >= p1.Y - thickness - errorRange && passPoint.Y <= p1.Y + errorRange && p1.X + thickness <= passPoint.X && passPoint.X <= p2.X - thickness )
        return ( RoomEdge.Back, p1 ) ;
      if ( passPoint.X >= p1.X - errorRange && passPoint.X < p1.X + thickness && p1.Y - thickness < passPoint.Y && passPoint.Y <= p1.Y + errorRange )
        return ( RoomEdge.LeftBackCorner, new XYZ( p1.X, p1.Y - thickness, p1.Z ) ) ;
      if ( passPoint.X > p2.X - thickness && passPoint.X <= p2.X + errorRange && p2.Y - thickness < passPoint.Y && passPoint.Y <= p2.Y + errorRange )
        return ( RoomEdge.RightBackCorner, new XYZ( p2.X, p2.Y - thickness, p2.Z ) ) ;
      if ( passPoint.Y >= p4.Y - errorRange && passPoint.Y < p4.Y + thickness && p4.X - errorRange <= passPoint.X && passPoint.X < p4.X + thickness + errorRange )
        return ( RoomEdge.LeftFrontCorner, new XYZ( p4.X, p4.Y + thickness, p4.Z ) ) ;
      if ( passPoint.Y >= p3.Y - errorRange && passPoint.Y < p3.Y + thickness && p3.X - thickness < passPoint.X && passPoint.X <= p3.X + errorRange )
        return ( RoomEdge.RightFrontCorner, new XYZ( p3.X, p3.Y + thickness, p3.Z ) ) ;

      return ( RoomEdge.Other, new XYZ( p1.X, p1.Y - thickness, p1.Z ) ) ;
    }

    public static bool CheckPickElementIsInOrOutRoom( Document document, Reference element, XYZ elementEndPoint )
    {
      var room = document.GetElement( element.ElementId ) ;
      if ( room == null ) return true ;
      var locationPoint = ( room.Location as LocationPoint ) ! ;
      var lenght = room.ParametersMap.get_Item( "Lenght" ).AsDouble() ;
      var width = room.ParametersMap.get_Item( "Width" ).AsDouble() ;
      var p1 = locationPoint.Point ;
      var p2 = new XYZ( p1.X + lenght, p1.Y, p1.Z ) ;
      var p3 = new XYZ( p2.X, p2.Y - width, p2.Z ) ;
      return ! ( elementEndPoint.X > p1.X ) || ! ( elementEndPoint.X < p2.X ) || ! ( elementEndPoint.Y < p1.Y ) || ! ( elementEndPoint.Y > p3.Y ) ;
    }
    
    public static RoomEdge GetPassPointPositionOutRoom( XYZ passPoint, XYZ p1, XYZ p2, XYZ p4 )
    {
      if ( Math.Abs( passPoint.X - p1.X ) < Math.Abs( passPoint.X - p2.X ) 
           && Math.Abs( passPoint.X - p1.X ) < Math.Abs( passPoint.Y - p1.Y ) 
           && Math.Abs( passPoint.X - p1.X ) < Math.Abs( passPoint.Y - p4.Y ) )
        return RoomEdge.Left ;
      if ( Math.Abs( passPoint.X - p2.X ) < Math.Abs( passPoint.X - p1.X ) 
           && Math.Abs( passPoint.X - p2.X ) < Math.Abs( passPoint.Y - p1.Y ) 
           && Math.Abs( passPoint.X - p2.X ) < Math.Abs( passPoint.Y - p4.Y ) )
        return RoomEdge.Right ;
      if ( Math.Abs( passPoint.Y - p1.Y ) < Math.Abs( passPoint.Y - p4.Y ) 
           && Math.Abs( passPoint.Y - p1.Y ) < Math.Abs( passPoint.X - p1.X ) 
           && Math.Abs( passPoint.Y - p1.Y ) < Math.Abs( passPoint.X - p2.X ) )
        return RoomEdge.Back ;
      return RoomEdge.Front ;
    }

    private class RoomPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return ( e.GetBuiltInCategory() == BuiltInCategory.OST_GenericModel ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return false ;
      }
    }
  }
}