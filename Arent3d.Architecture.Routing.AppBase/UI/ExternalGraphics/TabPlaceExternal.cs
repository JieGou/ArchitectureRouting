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
        private readonly double _radius = 300d.MillimetersToRevitUnits() ;
        private readonly UIDocument _uiDocument ;

        public XYZ? Origin { get ; set ; }
        
        public TabPlaceExternal( UIApplication uiApplication ) : base( uiApplication )
        {
            _uiDocument = uiApplication.ActiveUIDocument ;
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
                _numberOfTabs++ ;
                
                var nextPoint = new XYZ( DrawingServer.NextPoint!.X, DrawingServer.NextPoint.Y, DrawingServer.BasePoint!.Z ) ;
                var direction = ( nextPoint - DrawingServer.BasePoint ).Normalize() ;
                    
                switch ( _numberOfTabs % 3 ) {
                    case 0 :
                        DrawingServer.BasePoint = Origin ;
                        break ;
                    case 1 :
                    {
                        var transform = Transform.CreateTranslation( direction.CrossProduct( _uiDocument.ActiveView.ViewDirection ) * _radius ) ;
                        DrawingServer.BasePoint = transform.OfPoint( Origin ) ;
                        break ;
                    }
                    default :
                    {
                        var transform = Transform.CreateTranslation( direction.CrossProduct( _uiDocument.ActiveView.ViewDirection ).Negate() * _radius ) ;
                        DrawingServer.BasePoint = transform.OfPoint( Origin ) ;
                        break ;
                    }
                }

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
    }
}