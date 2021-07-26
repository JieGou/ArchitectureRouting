using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Revit.UI
{

  public class AppUIBase : IAppUIBase
  {
    public bool IsDisposed { get ; private set ; }

    ~AppUIBase()
    {
      if ( IsDisposed ) return ;
      IsDisposed = true ;

      ReleaseUnmanagedResources() ;
    }

    void IDisposable.Dispose()
    {
      if ( IsDisposed ) return ;
      IsDisposed = true ;

      GC.SuppressFinalize( this ) ;

      DisposeManagedResources() ;

      ReleaseUnmanagedResources() ;
    }

    public void UpdateUI( Document document, AppUIUpdateType updateType )
    {
      if ( document.IsFamilyDocument ) {
        UpdateUIForFamilyDocument( document, updateType ) ;
      }
      else {
        UpdateUIForNormalDocument( document, updateType ) ;
      }
    }

    protected virtual void UpdateUIForFamilyDocument( Document document, AppUIUpdateType updateType )
    {
    }

    protected virtual void UpdateUIForNormalDocument( Document document, AppUIUpdateType updateType )
    {
    }

    protected virtual void DisposeManagedResources()
    {
    }

    protected virtual void ReleaseUnmanagedResources()
    {
    }
  }
}