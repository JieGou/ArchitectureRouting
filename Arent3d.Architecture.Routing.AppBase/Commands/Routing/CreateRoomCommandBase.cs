﻿using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public class CreateRoomCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      UIApplication uiApp = commandData.Application ;
      UIDocument uiDocument = uiApp.ActiveUIDocument ;
      Document document = uiDocument.Document ;
      var selection = uiDocument.Selection ;

      if ( !IsExistParameter( document, ElectricalRoutingElementParameter.RoomCondition ) ) {
        message = "Please initial the plugin!" ;
        return Result.Cancelled ;
      }

      if ( !TryGetConditions( document, out var conditions ) ) {
        message = "Please load CSV data!" ;
        return Result.Cancelled ;
      }

      if ( ! conditions.Any() ) {
        message = "Not found the condition!" ;
        return Result.Cancelled ;
      }

      var viewModel = new ArentRoomViewModel()
      {
        Conditions = conditions
      } ;
      var view = new ArentRoomView { DataContext = viewModel } ;
      view.ShowDialog() ;
      if ( ! viewModel.IsCreate )
        return Result.Cancelled ;
      
      bool checkEx = false ;
      const double minDistance = 0.2 ;
      try {
        // Pick first point 
        XYZ firstPoint = selection.PickPoint( "Pick first point" ) ;
        XYZ? lastPoint = null ;
        // This is the object to render the guide line
        RectangleExternal rectangleExternal = new RectangleExternal( uiApp ) ;
        try {
          // Add first point to list picked points
          rectangleExternal.PickedPoints.Add( firstPoint ) ;
          // Assign first point
          rectangleExternal.DrawingServer.BasePoint = firstPoint ;
          // Render the guide line
          rectangleExternal.DrawExternal() ;
          // Pick next point 
          lastPoint = selection.PickPoint( "Pick next point" ) ;
          if ( firstPoint.DistanceTo( lastPoint ) < minDistance ) {
            message = "Lenght too small to create room" ;
            return Result.Failed ;
          }
        }
        catch ( Exception ) {
          checkEx = true ;
        }
        finally {
          // End to render guide line
          rectangleExternal.Dispose() ;
        }

        // If last point is null. Return failed to end command
        if ( lastPoint == null || checkEx ) return Result.Failed ;

        using var trans = new Transaction( document, "Create Arent Room" ) ;
        
        trans.Start() ;
        var mpt = ( firstPoint + lastPoint ) * 0.5 ;
        var currView = document.ActiveView ;
        var plane = Plane.CreateByNormalAndOrigin( currView.RightDirection, mpt ) ;
        var mirrorMat = Transform.CreateReflection( plane ) ;
        var secondPoint = mirrorMat.OfPoint( firstPoint ) ;
        var thirdPoint = mirrorMat.OfPoint( lastPoint ) ;

        var levelId = currView.GenLevel.Id ;
        HeightSettingStorable heightSetting = document.GetHeightSettingStorable() ;
        var levels = heightSetting.Levels.OrderBy( x => x.Elevation ).ToList() ;
        var level = levels.FirstOrDefault( x => x.Id == levelId ) ;
        //Find above level
        var aboveLevel = levels.Last() ;
        for ( var i = 0 ; i < levels.Count - 1 ; i++ ) {
          if ( levels[ i ].Id != level!.Id ) continue ;
          aboveLevel = levels[ i + 1 ] ;
          break ;
        }

        var wallHeightDefault = ( 4000.0 ).MillimetersToRevitUnits() ;
        var thickness = ( 1.0 ).MillimetersToRevitUnits() ;
        var heightOfLevel = aboveLevel == level ? wallHeightDefault : aboveLevel.Elevation - level!.Elevation - thickness ;
        var lenght = firstPoint.DistanceTo( secondPoint ) ;
        var width = secondPoint.DistanceTo( lastPoint ) ;
        var originPoint = GetOriginPoint( firstPoint, secondPoint, thirdPoint, lastPoint ) ;

        var symbol = document.GetFamilySymbols( ElectricalRoutingFamilyType.Room ).FirstOrDefault() ?? throw new InvalidOperationException() ;
        var instance = symbol.Instantiate( originPoint, level!, StructuralType.NonStructural ) ;
        // Set room's parameters
        instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;
        instance.LookupParameter( "Lenght" ).Set( lenght ) ;
        instance.LookupParameter( "Width" ).Set( width ) ;
        instance.LookupParameter( "Height" ).Set( heightOfLevel ) ;
        instance.LookupParameter( "Thickness" ).Set( thickness ) ;
        instance.SetProperty( ElectricalRoutingElementParameter.RoomCondition, viewModel.SelectedCondition! ) ;

        var overrideGraphicSettings = new OverrideGraphicSettings() ;
        overrideGraphicSettings.SetSurfaceTransparency( 100 ) ;
        document.ActiveView.SetElementOverrides(instance.Id, overrideGraphicSettings);
        trans.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        message = e.Message ;
        return Result.Failed ;
      }
    }

    private bool IsExistParameter(Document document, ElectricalRoutingElementParameter roomCondition)
    {
      var guiId = roomCondition.GetParameterGuid() ;
      var shareParameter = SharedParameterElement.Lookup(document, guiId ) ;
      return null != shareParameter ;
    }

    public static bool TryGetConditions( Document document, out List<string> conditions )
    {
      conditions = new List<string>() ;
      var store = document.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( null == store )
        return false ;

      conditions = store.CeedModelData.Select( x => x.Condition ).Distinct().Where(x => !string.IsNullOrEmpty(x)).OrderBy( x => x ).ToList() ;
      return true ;
    }

    private XYZ GetOriginPoint( XYZ firstPoint, XYZ secondPoint, XYZ thirdPoint, XYZ lastPoint )
    {
      XYZ originPoint = XYZ.Zero ;
      if ( firstPoint.X < lastPoint.X && firstPoint.Y > lastPoint.Y )
        originPoint = firstPoint ;
      if ( firstPoint.X < lastPoint.X && firstPoint.Y < lastPoint.Y )
        originPoint = thirdPoint ;
      if ( firstPoint.X > lastPoint.X && firstPoint.Y > lastPoint.Y )
        originPoint = secondPoint ;
      if ( firstPoint.X > lastPoint.X && firstPoint.Y < lastPoint.Y )
        originPoint = lastPoint ;
      return originPoint ;
    }
  }
}