using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Reflection ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing
{
  public static class ViewExtensions
  {
    private const string RoutingFamilyName = "Arent-Generic Models-Box" ;

    private const string RoutingViewPostFix = "Routing Assist" ;

    public static void CreateRoutingView( this Document document, IReadOnlyCollection<(ElementId Id, string Name)> levels )
    {
      var floorPlanFamily = document.GetAllElements<ViewFamilyType>().FirstOrDefault( viewFamilyType => viewFamilyType.ViewFamily == ViewFamily.FloorPlan ) ?? throw new InvalidOperationException() ;
      var views = document.GetAllElements<View>() ;

      foreach ( var (id, name) in levels ) {
        var view = ViewPlan.Create( document, floorPlanFamily.Id, id ) ;
        foreach ( Category cat in view.Document.Settings.Categories ) {
          if ( cat.get_AllowsVisibilityControl( view ) ) {
            cat.set_Visible( view, IsViewable( cat ) ) ;
          }
        }

        view.Name = $"{name} - {RoutingViewPostFix}" ;
        view.ViewTemplateId = ElementId.InvalidElementId ;
        view.get_Parameter( BuiltInParameter.VIEW_DISCIPLINE ).Set( 4095 ) ;

        var pvr = view.GetViewRange() ;

        // pvr.SetLevelId(PlanViewPlane.TopClipPlane, vp.LevelId);
        pvr.SetOffset( PlanViewPlane.TopClipPlane, 4000.0 / 304.8 ) ;

        pvr.SetOffset( PlanViewPlane.CutPlane, 3000.0 / 304.8 ) ;

        // pvr.SetLevelId(PlanViewPlane.BottomClipPlane, vp.LevelId);
        pvr.SetOffset( PlanViewPlane.BottomClipPlane, 0.0 ) ;
        view.SetViewRange( pvr ) ;
      }

      var filter = CreateElementFilter<RoutingFamilyType>( document ) ;

      foreach ( View v in views ) {
        if ( NotContain( v.Name, RoutingViewPostFix ) ) {
          try {
            v.AddFilter( filter.Id ) ;
            v.SetFilterVisibility( filter.Id, false ) ;
          }
          catch {
            // ignored
          }
        }
      }
    }

    private static FilterElement CreateElementFilter<TFamilyTypeEnum>( Document document ) where TFamilyTypeEnum : Enum
    {
      var familyNameParamId = new ElementId( BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM ) ;

      var categoryFilters = new List<ElementId>() ;
      var elementFilters = new List<ElementFilter>() ;

      foreach ( var field in typeof( TFamilyTypeEnum ).GetFields() ) {
        var nameOnRevit = field.GetCustomAttribute<NameOnRevitAttribute>() ;
        if ( null == nameOnRevit ) continue ;

        var familyCategory = field.GetCustomAttribute<FamilyCategoryAttribute>() ;
        if ( null == familyCategory ) continue ;

        var filterRule = ParameterFilterRuleFactory.CreateEqualsRule( familyNameParamId, RoutingFamilyName, true ) ;
        elementFilters.Add( new ElementParameterFilter( filterRule ) ) ;

        categoryFilters.Add( new ElementId( familyCategory.Category ) ) ;
      }


      var filter = ParameterFilterElement.Create( document, "RoutingModels", categoryFilters ) ;
      filter.SetElementFilter( new LogicalOrFilter( elementFilters ) ) ;

      return filter ;
    }

    private static bool NotContain( string s, string key )
    {
      if ( s.Contains( key ) ) {
        return false ;
      }

      return true ;
    }

    private static readonly BuiltInCategory[] ViewableElementCategory =
    {
      BuiltInCategory.OST_DuctAccessory,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctTerminal,
      BuiltInCategory.OST_FlexDuctCurves,
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_PipeFitting,
      BuiltInCategory.OST_PipeSegments,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_PlumbingFixtures,
      BuiltInCategory.OST_Columns,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_Grids,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_Sections,
      BuiltInCategory.OST_SectionBox
    } ;

    private static bool IsViewable( Category cat )
    {
      return ViewableElementCategory.Contains( (BuiltInCategory) cat.Id.IntegerValue ) ;
    }
  }
}