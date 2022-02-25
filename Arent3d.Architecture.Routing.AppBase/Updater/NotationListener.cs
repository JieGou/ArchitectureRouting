using System ;
using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
  // public class NotationListener : IDocumentUpdateListener
  // {
  //   public string Name { get; }
  //   public Guid Guid { get; }
  //   public string Description { get; }
  //   public bool IsDocumentSpan { get; }
  //   bool IsOptional { get; }
  //   ChangePriority ChangePriority { get; }
  //   DocumentUpdateListenType ListenType { get; }
  //   
  //   public NotationListener()
  //   {
  //     
  //   }
  //   
  //   ElementFilter GetElementFilter(Document? document);
  //   bool CanListen(Document document);
  //   IEnumerable<ParameterProxy> GetListeningParameters(Document? document);
  //
  //   public void Execute( UpdaterData data )
  //   {
  //     try
  //     {
  //       Document doc = data.GetDocument();
  //       
  //     }
  //     catch (Exception exception)
  //     {
  //       CommandUtils.DebugAlertException( exception ) ;
  //     }
  //   }
  // }
}