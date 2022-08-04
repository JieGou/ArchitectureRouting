using Autodesk.Revit.DB ;
using System;

namespace Arent3d.Architecture.Routing.Storages.Extensions
{
    public static class TransactionExtension
    {
        public static void OpenTransactionIfNeed(this Transaction transaction, Document document, string transactionName, Action action)
        {
            if (!document.IsModifiable)
            {
                if (string.IsNullOrEmpty(transactionName))
                    throw new ArgumentNullException(nameof(transactionName));

                transaction.Start(transactionName);
                action();
                transaction.Commit();
            }
            else
            {
                action();
            }
        }
    }
}