using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
    public class TabPlaceExternal : DrawExternalBase
    {
        private int _numberOfTabs ;
        private readonly double _width ;
        private readonly UIDocument _uiDocument ;
        private readonly IList<Curve> _curves ;
        private readonly XYZ _locationPoint ;
        private readonly ModelessOkCancelDialog _dialog ;
        
        private XYZ? _firstPoint ;
        public XYZ? FirstPoint
        {
            get => _firstPoint ;
            set
            {
                _firstPoint = value ;
                if ( null == _firstPoint ) 
                    return ;
                
                PlacePoint = new XYZ(_firstPoint.X, _firstPoint.Y, _firstPoint.Z) ;
                DrawingServer.BasePoint = new XYZ( _firstPoint.X, _firstPoint.Y, _firstPoint.Z ) ;
            }
        }
        
        public XYZ? SecondPoint { get ; set ; }
        
        public XYZ? PlacePoint { get ; set ; }
        
        public TabPlaceExternal( UIApplication uiApplication, List<Curve> curves, XYZ locationPoint, ModelessOkCancelDialog dialog ) : base( uiApplication )
        {
            _uiDocument = uiApplication.ActiveUIDocument ;
            _curves = curves ;
            _locationPoint = locationPoint ;
            _dialog = dialog ;

            _width = 0.5 * GetWidth( curves ) ;
        }

        private static double GetWidth( IList<Curve> curves )
        {
            if ( ! curves.Any() )
                throw new ArgumentException( nameof( curves ) ) ;

            var xs = curves.SelectMany( x => x.Tessellate().Select( y => y.X ) ).EnumerateAll() ;
            return xs.Max() - xs.Min() ;
        }

        private List<Curve> TransformCurves( IList<Curve> curves, XYZ? placePoint, XYZ? direction )
        {
            var curveTransforms = new List<Curve>() ;
            if ( ! curves.Any() || placePoint is null)
                return curveTransforms ;

            var curveTranslations = new List<Curve>() ;
            var vector = new XYZ( placePoint.X, placePoint.Y, _locationPoint.Z ) - _locationPoint ;
            var transform = Transform.CreateTranslation( vector ) ;
            foreach ( var curve in curves ) 
                curveTranslations.Add(curve.CreateTransformed(transform));

            if ( direction != null ) {
                var curveRotations = new List<Curve>() ;
                transform = Transform.CreateRotationAtPoint(XYZ.BasisZ, GetAngle(direction), placePoint);
                foreach ( var curveTranslation in curveTranslations ) 
                    curveRotations.Add(curveTranslation.CreateTransformed(transform));

                curveTransforms = curveRotations ;
            }
            else {
                curveTransforms = curveTranslations ;
            }

            return curveTransforms ;
        }

        public static double GetAngle(XYZ direction )
        {
            var angle = direction.AngleTo( XYZ.BasisY ) ;
            
            if ( direction.X <= 0 )
                return angle ;
            
            return -angle ;
        }

        public override void DrawExternal()
        {
            DrawingServer.CurveList.Clear() ;

            if ( DrawingServer.BasePoint is null || 
                 DrawingServer.NextPoint is null ||
                 PlacePoint is null)
                return ;

            if ( SecondPoint is null ) {
                var curves = TransformCurves( _curves, PlacePoint, null ) ;
                DrawingServer.CurveList = curves ;
            }
            else if ( FirstPoint != null ) {
                var vector = (SecondPoint - FirstPoint).Normalize() ;
                var curves = TransformCurves( _curves, PlacePoint, vector ) ;
                DrawingServer.CurveList = curves ;
            }
        }

        public override void OnKeyPressActivity( object sender, KeyPressEventArgs e )
        {
            
            var topWindow = GetActiveWindow() ;
            if ( RevitWindow != topWindow ) {
                e.Handled = false ;
                return ;
            }

            if ( e.KeyChar == 32 ) {
                if(null == FirstPoint || null == SecondPoint)
                    return;
                
                _numberOfTabs++ ;
                var direction = ( SecondPoint - FirstPoint ).Normalize() ;
                    
                switch ( _numberOfTabs % 3 ) {
                    case 0 :
                        DrawingServer.BasePoint = FirstPoint ;
                        break ;
                    case 1 :
                    {
                        var transform = Transform.CreateTranslation( direction.CrossProduct( _uiDocument.ActiveView.ViewDirection ) * _width ) ;
                        DrawingServer.BasePoint = transform.OfPoint( FirstPoint ) ;
                        break ;
                    }
                    default :
                    {
                        var transform = Transform.CreateTranslation( direction.CrossProduct( _uiDocument.ActiveView.ViewDirection ).Negate() * _width ) ;
                        DrawingServer.BasePoint = transform.OfPoint( FirstPoint ) ;
                        break ;
                    }
                }

                PlacePoint = new XYZ( DrawingServer.BasePoint.X, DrawingServer.BasePoint.Y, DrawingServer.BasePoint.Z ) ;
                DrawExternal() ;
                _uiDocument.RefreshActiveView();
            }
            else if ( e.KeyChar == 27 ) {
                DrawingServer.BasePoint = null ;
                StopExternal() ;
            }
            else if ( e.KeyChar == 13 ) {
                StopExternal() ;
            }

            e.Handled = false ;

            if ( e.KeyChar != 13 )
                return ;

            _dialog.Focus() ;
        }

        public override void OnMouseActivity( object sender, MouseEventArgs e )
        {
            var topWindow = GetActiveWindow() ;
            if ( RevitWindow != topWindow )
                return;

            var currPoint = GetMousePoint();
            DrawingServer.NextPoint = currPoint ;
            DrawExternal();
            _uiDocument.RefreshActiveView();
        }
    }
}