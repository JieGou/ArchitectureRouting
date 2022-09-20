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

        private List<Curve> TransformCurves( IList<Curve> curves, XYZ? placePoint, XYZ? direction, XYZ? firstPoint )
        {
            var curveTransforms = new List<Curve>() ;
            if ( ! curves.Any() || placePoint is null)
                return curveTransforms ;

            var curveTranslations = new List<Curve>() ;
            var vector = new XYZ( placePoint.X, placePoint.Y, _locationPoint.Z ) - _locationPoint ;
            var transform = Transform.CreateTranslation( vector ) ;
            foreach ( var curve in curves ) 
                curveTranslations.Add(curve.CreateTransformed(transform));

            if ( direction != null && firstPoint != null ) {
                var curveRotations = new List<Curve>() ;
                transform = Transform.CreateRotationAtPoint(XYZ.BasisZ, GetAngle(direction, firstPoint, placePoint), placePoint);
                foreach ( var curveTranslation in curveTranslations ) 
                    curveRotations.Add(curveTranslation.CreateTransformed(transform));

                curveTransforms = curveRotations ;
            }
            else {
                curveTransforms = curveTranslations ;
            }

            return curveTransforms ;
        }

        public static double GetAngle( XYZ direction, XYZ firstPoint, XYZ placePoint )
        {
            const double tolerance = 0.0001 ;
            if ( direction.X < 0 )
                direction = direction.Negate() ;
            
            if ( IsAboveLine( direction, firstPoint, placePoint ) ) {
                var angle = direction.AngleTo( XYZ.BasisY ) ;
                if ( Math.Abs( angle - 0.5 * Math.PI ) < tolerance )
                    return 0 ;

                if ( angle < tolerance || Math.Abs( angle - Math.PI ) < tolerance ) {
                    if ( placePoint.X <= firstPoint.X )
                        return 0 ;
                    return Math.PI ;
                }

                if ( angle < 0.5 * Math.PI )
                    angle = 0.5 * Math.PI - angle ;
                else
                    angle = -( angle - Math.PI * 0.5 ) ;

                return angle ;
            }
            else {
                var angle = direction.AngleTo( XYZ.BasisX.Negate() ) ;
                if ( angle < tolerance )
                    return 0 ;

                if ( Math.Abs( angle - 0.5 * Math.PI ) < tolerance )
                {
                    if ( placePoint.X <= firstPoint.X )
                        return 0 ;
                    return Math.PI ;
                }

                if ( angle < 0.5 * Math.PI )
                    angle = Math.PI - angle ;

                if ( direction.Y >= 0 )
                    return -angle ;

                return angle ;
            }
        }

        private static bool IsAboveLine( XYZ direction, XYZ firstPoint, XYZ placePoint )
        {
            var vector = direction.Normalize() ;
            if ( vector.X < 0 )
                vector = vector.Negate() ;
            
            double equation( XYZ point ) => -vector.Y * (point.X - firstPoint.X) + vector.X * (point.Y - firstPoint.Y);
            return equation( placePoint ) >= 0 ;
        }

        public override void DrawExternal()
        {
            DrawingServer.CurveList.Clear() ;

            if ( DrawingServer.BasePoint is null || 
                 DrawingServer.NextPoint is null ||
                 PlacePoint is null)
                return ;

            if ( SecondPoint is null ) {
                var curves = TransformCurves( _curves, PlacePoint, null, null ) ;
                DrawingServer.CurveList = curves ;
            }
            else if ( FirstPoint != null ) {
                var vector = (new XYZ(SecondPoint.X, SecondPoint.Y, FirstPoint.Z) - FirstPoint).Normalize() ;
                var curves = TransformCurves( _curves, PlacePoint, vector, FirstPoint ) ;
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
                var direction = ( new XYZ(SecondPoint.X, SecondPoint.Y, FirstPoint.Z) - FirstPoint ).Normalize() ;
                    
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