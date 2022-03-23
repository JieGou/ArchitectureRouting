using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.Haseko.App.Commands.Routing.NewEnvelopeCommand", DefaultString = "New Envelope\nPS" )]
  [Image( "resources/new_envelope.png" )]
  public class NewEnvelopeCommand : NewEnvelopeCommandBase
  {
  }
}