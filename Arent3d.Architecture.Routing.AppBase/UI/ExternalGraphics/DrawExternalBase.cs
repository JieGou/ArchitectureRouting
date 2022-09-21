using System ;
using System.Collections.Generic ;
using System.Runtime.InteropServices ;
using System.Windows.Forms ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.UI.ExternalGraphics
{
  public abstract class DrawExternalBase : IDisposable
  {
    protected UIApplication UIApplication { get ; set ; }
    public ExternalDrawingServer DrawingServer { get ; set ; }
    public UserActivityHook UserActivityHook { get ; set ; }
    public List<XYZ> PickedPoints { get ; set ; }
    public string UserInput { get ; set ; }

    protected IntPtr RevitWindow = IntPtr.Zero ;

    protected DrawExternalBase( UIApplication uiApplication )
    {
      this.UserInput = "" ;
      this.PickedPoints = new List<XYZ>() ;
      this.UIApplication = uiApplication ;
      this.UserActivityHook = new UserActivityHook( true, true ) ;
      this.UserActivityHook.OnMouseActivity += OnMouseActivity ;
      this.UserActivityHook.KeyPress += OnKeyPressActivity ;

      this.DrawingServer = new ExternalDrawingServer( this.UIApplication.ActiveUIDocument.Document ) ;
      var externalGraphics = new DrawingServerHost() ;
      externalGraphics.RegisterServer( this.DrawingServer ) ;

      this.RevitWindow = GetActiveWindow() ;
    }

    public abstract void DrawExternal() ;

    public virtual void OnKeyPressActivity( object sender, KeyPressEventArgs e )
    {
      var topWindow = GetActiveWindow() ;
      if ( this.RevitWindow != topWindow ) {
        e.Handled = false ;
        return ;
      }

      if ( this.DrawingServer != null && e.KeyChar == 27 ) {
        this.UserInput = "" ;
        StopExternal() ;
      }

      if ( this.DrawingServer != null && e.KeyChar == 8 && this.UserInput.Length > 0 ) {
        this.UserInput = this.UserInput.Substring( 0, this.UserInput.Length - 1 ) ;
      }
      else {
        if ( char.IsLetterOrDigit( e.KeyChar ) ) {
          this.UserInput += e.KeyChar.ToString() ;
        }
      }

      e.Handled = false ;
    }

    public void StopExternal()
    {
      if ( this.UserActivityHook != null ) {
        this.UserActivityHook.OnMouseActivity -= OnMouseActivity ;
        this.UserActivityHook.KeyPress -= OnKeyPressActivity ;
        this.UserActivityHook.Stop() ;
      }

      if ( this.DrawingServer != null ) {
        this.DrawingServer.BasePoint = null ;
        this.DrawingServer.NextPoint = null ;
        this.DrawingServer.CurveList.Clear() ;

        var externalGraphics = new DrawingServerHost() ;
        externalGraphics.UnRegisterServer( this.DrawingServer.Document ) ;

        this.DrawingServer = null! ;
      }

      UIApplication.ActiveUIDocument.RefreshActiveView() ;
    }

    public virtual void OnMouseActivity( object sender, MouseEventArgs e )
    {
      var topWindow = GetActiveWindow() ;
      if ( this.RevitWindow != topWindow ) {
        return ;
      }

      try {
        var currPoint = GetMousePoint() ;
        //add points to the list:
        if ( this.PickedPoints != null && e.Clicks > 0 && e.Button == MouseButtons.Left ) {
          this.PickedPoints.Add( currPoint ) ;
        }

        if ( this.DrawingServer != null && this.DrawingServer.BasePoint == null && e.Clicks > 0 && e.Button == MouseButtons.Left ) {
          //start server
          this.DrawingServer.BasePoint = currPoint ;
        }
        else if ( this.DrawingServer != null && e.Clicks > 0 && e.Button == MouseButtons.Left ) {
          this.DrawingServer.BasePoint = currPoint ;
          this.DrawingServer.NextPoint = null ;
        }
        else if ( this.DrawingServer != null ) {
          //mouse is moving
          if ( this.DrawingServer.NextPoint != null ) {
            if ( currPoint.DistanceTo( this.DrawingServer.NextPoint ) > 0.01 ) {
              this.DrawingServer.NextPoint = currPoint ;
              this.DrawExternal() ;
              UIApplication.ActiveUIDocument.RefreshActiveView() ;
            }
          }
          else {
            this.DrawingServer.NextPoint = currPoint ;
            this.DrawExternal() ;
            UIApplication.ActiveUIDocument.RefreshActiveView() ;
          }
        }
      }
      catch ( Exception ) {
      }
    }

    private UIView GetActiveUiView( UIDocument uidoc )
    {
      var doc = uidoc.Document ;
      var view = doc.ActiveView ;
      var uiviews = uidoc.GetOpenUIViews() ;
      UIView uiview = null! ;

      foreach ( var uv in uiviews ) {
        if ( uv.ViewId.Equals( view.Id ) ) {
          uiview = uv ;
          break ;
        }
      }

      return uiview ;
    }

    protected XYZ GetMousePoint()
    {
      var uiView = GetActiveUiView( UIApplication.ActiveUIDocument ) ;
      var corners = uiView.GetZoomCorners() ;
      var rect = uiView.GetWindowRectangle() ;
      var p = Cursor.Position ;
      var dx = (double) ( p.X - rect.Left ) / ( rect.Right - rect.Left ) ;
      var dy = (double) ( p.Y - rect.Bottom ) / ( rect.Top - rect.Bottom ) ;
      var a = corners[ 0 ] ;
      var b = corners[ 1 ] ;
      var v = b - a ;
      var q = a + dx * v.X * XYZ.BasisX + dy * v.Y * XYZ.BasisY ;

      return q ;
    }

    public void Dispose()
    {
      this.StopExternal() ;
    }

    [DllImport( "user32.dll" )]
    protected static extern IntPtr GetActiveWindow() ;
  }
}