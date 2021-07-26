using System ;
using System.Reflection ;
using Autodesk.Revit.UI ;

namespace Arent3d.Revit.UI.Attributes
{
  [AttributeUsage( AttributeTargets.Class )]
  public class ButtonAttribute : Attribute
  {
    public Type CommandType { get ; }
    public Type? AvailabilityType { get ; set ; } = null ;

    public Type? TypeInResourceAssembly { get ; set ; } = null ;

    public bool InitializeButton { get ; set ; } = false ;
    public bool OnlyInitialized { get ; set ; } = false ;

    public ButtonAttribute( Type commandType ) : this( commandType, GetAvailabilityType( commandType ) )
    {
    }

    public ButtonAttribute( Type commandType, Type? availabilityType )
    {
      CommandType = commandType ;
      AvailabilityType = availabilityType ;
    }

    private static Type? GetAvailabilityType( Type commandType )
    {
      if ( commandType.HasInterface<IExternalCommandAvailability>() ) return commandType ;

      return null ;
    }
  }
}