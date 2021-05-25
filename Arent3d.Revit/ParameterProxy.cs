using Autodesk.Revit.DB ;

namespace Arent3d.Revit
{
  public abstract class ParameterProxy
  {
    public static ParameterProxy From( Parameter parameter ) => new ParameterProxyFromParameter( parameter ) ;
    public static ParameterProxy From( ElementId parameterElementId ) => new ParameterProxyFromElement( parameterElementId ) ;

    public abstract ChangeType GetChangeTypeParameter() ;


    private class ParameterProxyFromParameter : ParameterProxy
    {
      public Parameter Parameter { get ; }

      public ParameterProxyFromParameter( Parameter parameter )
      {
        Parameter = parameter ;
      }

      public override ChangeType GetChangeTypeParameter() => Element.GetChangeTypeParameter( Parameter ) ;
    }

    private class ParameterProxyFromElement : ParameterProxy
    {
      public ElementId ParameterElementId { get ; }

      public ParameterProxyFromElement( ElementId parameterElementId )
      {
        ParameterElementId = parameterElementId ;
      }

      public override ChangeType GetChangeTypeParameter() => Element.GetChangeTypeParameter( ParameterElementId ) ;
    }
  }
}