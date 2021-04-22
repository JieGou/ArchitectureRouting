using System ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI
{
  public static class TransactionExtensions
  {
    public static Result TransactionGroup( this Document document, string transGroupName, Func<TransactionGroup, Result> action )
    {
      using var transactionGroup = new TransactionGroup( document ) ;
      try {
        transactionGroup.Start( transGroupName ) ;

        var result = action( transactionGroup ) ;
        if ( Result.Succeeded != result ) {
          transactionGroup.RollBack() ;
          return result ;
        }

        transactionGroup.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        transactionGroup.RollBack() ;
        return Result.Cancelled ;
      }
      catch ( OperationCanceledException ) {
        transactionGroup.RollBack() ;
        return Result.Cancelled ;
      }
      catch {
        transactionGroup.RollBack() ;
        throw ;
      }
    }

    public static T TransactionGroup<T>( this Document document, string transGroupName, Func<TransactionGroup, (Result, T)> action, T onCancel )
    {
      using var transactionGroup = new TransactionGroup( document ) ;
      try {
        transactionGroup.Start( transGroupName ) ;

        var result = action( transactionGroup ) ;
        if ( Result.Succeeded != result.Item1 ) {
          transactionGroup.RollBack() ;
          return result.Item2 ;
        }

        transactionGroup.Commit() ;
        return result.Item2 ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        transactionGroup.RollBack() ;
        return onCancel ;
      }
      catch ( OperationCanceledException ) {
        transactionGroup.RollBack() ;
        return onCancel ;
      }
      catch {
        transactionGroup.RollBack() ;
        throw ;
      }
    }

    public static Result Transaction( this Document document, string name, Func<Transaction, Result> action )
    {
      using var transaction = new Transaction( document ) ;
      try {
        transaction.Start( name ) ;

        var result = action( transaction ) ;
        if ( Result.Succeeded != result ) {
          transaction.RollBack() ;
          return result ;
        }

        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        transaction.RollBack() ;
        return Result.Cancelled ;
      }
      catch ( OperationCanceledException ) {
        transaction.RollBack() ;
        return Result.Cancelled ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }

    public static T Transaction<T>( this Document document, string name, Func<Transaction, (Result, T)> action, T onCancel )
    {
      using var transaction = new Transaction( document ) ;
      try {
        transaction.Start( name ) ;

        var result = action( transaction ) ;
        if ( Result.Succeeded != result.Item1 ) {
          transaction.RollBack() ;
          return result.Item2 ;
        }

        transaction.Commit() ;
        return result.Item2 ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        transaction.RollBack() ;
        return onCancel ;
      }
      catch ( OperationCanceledException ) {
        transaction.RollBack() ;
        return onCancel ;
      }
      catch {
        transaction.RollBack() ;
        throw ;
      }
    }
  }
}