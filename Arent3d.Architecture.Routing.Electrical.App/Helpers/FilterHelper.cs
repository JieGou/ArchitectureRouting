using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.Electrical.App.Helpers
{
  public static class FilterHelper
  {
    public static void InitialFilters(Document document)
    {
      const string parameterName = "Location Type" ;
      
      using var transactionGroup = new TransactionGroup( document ) ;
      transactionGroup.Start("Add Filter") ;
        
      var categorySet = CreateCategorySet( document ) ;
      var (notify, parameterGuid) = ShareParameterHelper.FindOrCreateShareParameter( document, parameterName, categorySet ) ;
      if ( ! string.IsNullOrEmpty( notify ) ) {
        transactionGroup.RollBack() ;
        return ;
      }
        
      var viewPlans = document.GetAllElements<ViewPlan>().Where( x => ! x.IsTemplate ).ToList() ;
      if ( ! viewPlans.Any() ) {
        transactionGroup.RollBack() ;
        return ;
      }
        
      var shareParameterElement = SharedParameterElement.Lookup( document, parameterGuid ) ;
      var linePatternElements = PatternElementHelper.GetLinePatterns( document ) ;
      ApplyFilter( document, viewPlans, shareParameterElement, categorySet, linePatternElements ) ;

      transactionGroup.Assimilate() ;
    }
    private static void ApplyFilter( Document document, IEnumerable<ViewPlan> viewPlans, SharedParameterElement sharedParameterElement, CategorySet categorySet, List<(string PatternName, ElementId PatternId)> linePatternElements)
    {
      using var transaction = new Transaction( document ) ;
      transaction.Start( "Apply Filter" ) ;
      
      var filters = CreateFilters( document, sharedParameterElement, categorySet, linePatternElements ).ToList() ;
      foreach ( var viewPlan in viewPlans ) {
        if ( null != viewPlan.ViewTemplateId && document.GetElement(viewPlan.ViewTemplateId) is View view) {
          ApplyFilter( view, filters ) ;
        }
        else {
          ApplyFilter( viewPlan, filters ) ;
        }
      }
      
      transaction.Commit() ;
    }
    
    private static void ApplyFilter( View view, IEnumerable<(ParameterFilterElement Filter, ElementId PatternId)> filters )
    {
      foreach ( var (filter, patternId) in filters ) {
        if(view.IsFilterApplied(filter.Id))
          continue;
        
        var overrideGraphic = new OverrideGraphicSettings() ;
        overrideGraphic.SetProjectionLinePatternId( patternId ) ;
        view.SetFilterOverrides(filter.Id, overrideGraphic);
      }
    }

    private static IEnumerable<(ParameterFilterElement Filter, ElementId PatternId)> CreateFilters(Document document, SharedParameterElement sharedParameterElement,  CategorySet categorySet, List<(string PatternName, ElementId PatternId)> linePatternElements)
    {
      var parameterFilterElements = document.GetAllElements<ParameterFilterElement>() ;
      var categoryIds = categorySet.OfType<Category>().Select( x => x.Id ).ToList() ;

      var filters = new List<(ParameterFilterElement Filter, ElementId PatternId)>() ;
      foreach ( var (patternName, patternId) in linePatternElements ) {
        var parameterFilterElement = parameterFilterElements.SingleOrDefault( x => x.Name == patternName ) ;
        if ( null != parameterFilterElement ) {
          filters.Add((parameterFilterElement, patternId));
        }
        else {
          var elementParameterFilter = new ElementParameterFilter( ParameterFilterRuleFactory.CreateEqualsRule( sharedParameterElement.Id, patternName, true ) ) ;
          if ( ! ParameterFilterElement.ElementFilterIsAcceptableForParameterFilterElement( document, new HashSet<ElementId>( categoryIds ), elementParameterFilter ) ) 
            continue ;
          
          parameterFilterElement = ParameterFilterElement.Create( document, patternName, categoryIds, elementParameterFilter ) ;
          filters.Add((parameterFilterElement, patternId));
        }
      }

      return filters ;
    }
    
    private static CategorySet CreateCategorySet( Document document )
    {
      var categorySet = new CategorySet() ;
      categorySet.Insert( Category.GetCategory( document, BuiltInCategory.OST_Conduit ) ) ;
      categorySet.Insert( Category.GetCategory( document, BuiltInCategory.OST_ConduitFitting ) ) ;
      return categorySet ;
    }
  }
}