using System ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Storages.Extensions
{
  public static class DocumentExtension
  {
    public static void OpenTransactionIfNeed( this Document document, string transactionName, Action action )
    {
      if ( ! document.IsModifiable ) {
        if ( string.IsNullOrEmpty( transactionName ) )
          throw new ArgumentNullException( nameof( transactionName ) ) ;

        using var trans = new Transaction( document ) ;
        trans.Start( transactionName ) ;
        action() ;
        trans.Commit() ;
      }
      else {
        action() ;
      }
    }
  }
}