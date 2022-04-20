﻿using Arent3d.Architecture.Routing.AppBase.Commands.Initialization;
using Arent3d.Revit.UI;
using Autodesk.Revit.Attributes;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
    [Transaction( TransactionMode.Manual )]
    [DisplayNameKey( "Electrical.App.Commands.Initialization.RegistrationOfBoardDataCommand", DefaultString = "Registration of\nboard data" )]
    [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
    public class ShowRegistrationOfBoardDataCommand : ShowRegistrationOfBoardDataCommandBase
    {
       
    }
}