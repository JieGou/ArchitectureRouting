﻿using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing
{
    [Transaction(TransactionMode.Manual)]
    [DisplayNameKey("Electrical.App.Commands.Routing.NewIndoorHumiditySensorWithLogoCommand", DefaultString = "室内用湿度ｾﾝｻｰ\n(ロゴあり)" )]
    [Image("resources/Initialize-16.bmp", ImageType = ImageType.Normal)]
    [Image("resources/Initialize-32.bmp", ImageType = ImageType.Large)]
    public class NewIndoorHumiditySensorWithLogoCommand : NewConnectorCommandBase
    {
        protected override RoutingFamilyType RoutingFamilyType => RoutingFamilyType.IndoorHumiditySensorWithLogo;
    }
}