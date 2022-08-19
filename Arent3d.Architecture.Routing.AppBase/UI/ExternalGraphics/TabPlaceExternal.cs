using System ;
using System.Windows.Forms ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
    public class TabPlaceExternal : DrawExternalBase
    {
        private int _numberOfTabs ;
        private readonly double _radius ;
        private readonly UIDocument _uiDocument ;

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
        
        public TabPlaceExternal( UIApplication uiApplication, double radius ) : base( uiApplication )
        {
            _uiDocument = uiApplication.ActiveUIDocument ;
            _radius = radius ;
        }

        public override void DrawExternal()
        {
            DrawingServer.ArcList.Clear() ;

            if ( DrawingServer.BasePoint == null || DrawingServer.NextPoint == null || DrawingServer.BasePoint.DistanceTo( DrawingServer.NextPoint ) <= _uiDocument.Document.Application.ShortCurveTolerance )
                return ;

            var circle = Arc.Create( DrawingServer.BasePoint, _radius, 0, 2 * Math.PI, _uiDocument.ActiveView.RightDirection, _uiDocument.ActiveView.UpDirection ) ;
            DrawingServer.ArcList.Add( circle ) ;
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
                        var transform = Transform.CreateTranslation( direction.CrossProduct( _uiDocument.ActiveView.ViewDirection ) * _radius ) ;
                        DrawingServer.BasePoint = transform.OfPoint( FirstPoint ) ;
                        break ;
                    }
                    default :
                    {
                        var transform = Transform.CreateTranslation( direction.CrossProduct( _uiDocument.ActiveView.ViewDirection ).Negate() * _radius ) ;
                        DrawingServer.BasePoint = transform.OfPoint( FirstPoint ) ;
                        break ;
                    }
                }

                PlacePoint = new XYZ( DrawingServer.BasePoint.X, DrawingServer.BasePoint.Y, DrawingServer.BasePoint.Z ) ;
                DrawExternal() ;
                UIApplication.ActiveUIDocument.RefreshActiveView();
            }
            else if ( e.KeyChar == 27 ) {
                DrawingServer.BasePoint = null ;
                StopExternal() ;
            }
            else if ( e.KeyChar == 13 ) {
                StopExternal() ;
            }

            e.Handled = false ;
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