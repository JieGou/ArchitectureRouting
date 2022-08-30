using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Extensions ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Updater
{
    public class ConnectorUpdater : IUpdater
    {
        private readonly UpdaterId _updaterId ;
        
        public ConnectorUpdater( AddInId addInId )
        {
            _updaterId = new UpdaterId( addInId, new Guid( "1C2E3C4C-CD5B-4DBD-976C-85D7B3174EEE" ) ) ;
        }
        
        public void Execute( UpdaterData updaterData )
        {
            try {
                var document = updaterData.GetDocument() ;

                var uiDocument = new UIDocument( document ) ;
                if(uiDocument.Selection.GetElementIds().Count == 0)
                    return;
                
                var tagTypes = document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolContentTag ).ToList() ;
                if(!tagTypes.Any())
                    return;
                
                var hideQuantityType = tagTypes.FirstOrDefault( x => x.LookupParameter( "Is Hide Quantity" ).AsInteger() == 1 ) ;
                if(null == hideQuantityType)
                    return;
                
                var showQuantityType = tagTypes.FirstOrDefault( x => x.LookupParameter( "Is Show Quantity" ).AsInteger() == 1 ) ;
                if(null == showQuantityType)
                    return;

                var elementIdSelecteds = uiDocument.Selection.GetElementIds() ;
                if ( updaterData.GetModifiedElementIds().Count == 0 ) 
                    return;
                
                var elements = updaterData.GetModifiedElementIds()
                    .Where(x => elementIdSelecteds.Any(y => y == x))
                    .Select( x => document.GetElement( x ) )
                    .Where( x => BuiltInCategorySets.OtherElectricalElements.Any( y => y == x.GetBuiltInCategory() ) )
                    .ToList();
                if(!elements.Any())
                    return;
                    
                var familyName = ElectricalRoutingFamilyType.SymbolContentTag.GetFamilyName() ;
                var removeElements = new List<ElementId>() ;
                foreach ( var element in elements.Where( element => element.HasParameter(ElectricalRoutingElementParameter.Quantity) ) ) {
                    var tag = element.GetTagsFromElement().FirstOrDefault( x =>
                    {
                        if ( document.GetElement( x.GetTypeId() ) is not FamilySymbol type )
                            return false ;

                        return type.FamilyName == familyName ;
                    }) ;
                    if(null == tag)
                        continue;
                    
                    var quantity = element.GetPropertyString( ElectricalRoutingElementParameter.Quantity ) ;
                    if ( int.TryParse( quantity, out var value ) && value >= 0) {
                        switch ( value ) {
                            case 0 :
                                tag.ChangeTypeId( hideQuantityType.Id ) ;
                                break ;
                            default :
                                tag.ChangeTypeId( showQuantityType.Id ) ;
                                break ;
                        }
                    }
                    else {
                        element.SetProperty( ElectricalRoutingElementParameter.Quantity, string.Empty );
                        tag.ChangeTypeId( hideQuantityType.Id ) ;
                    }
                }

                if ( removeElements.Any() )
                    document.Delete( removeElements ) ;

                uiDocument.Selection.SetElementIds(new List<ElementId>());
            }
            catch ( Exception exception ) {
                TaskDialog.Show( "Arent Inc", exception.Message ) ;
            }
        }

        public UpdaterId GetUpdaterId()
        {
            return _updaterId ;
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.MEPFixtures ;
        }

        public string GetUpdaterName()
        {
            return "Connector Arent Updater" ;
        }

        public string GetAdditionalInformation()
        {
            return "Arent, " + "https://arent3d.com" ;
        }
    }
}