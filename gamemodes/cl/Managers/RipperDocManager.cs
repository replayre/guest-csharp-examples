public static class RipperDocManager
{
    public static void Register()
    {
        re.Hooks.Place(
            "RipperDocGameController",
            "OnInitialize",
            (RipperDocGameController_OnInitialize x) =>
            {
                var instance = ScriptGameInstance.Get();
                var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;
                var player = playerSys.GetLocalPlayerControlledGameObject()!;

                x.self.vendorUserData = new VendorUserData
                {
                    menu = "CYBERWARE",
                    vendorData = new quest.VendorPanelData
                    {
                        data = new game.VendorData
                        {
                            vendorId = "",
                            entityID = player.GetEntityID(),
                            isActive = true,
                        },
                    },
                };
                x.self.isActivePanel = true;

                var retval = re.Hooks.CallOriginal<re.msgpack.Bool>();
                x.self.PreparePlayerItems();
                return retval;
            }
        );

        re.Hooks.PlaceVoid(
            "RipperDocGameController",
            "OpenPerkTree;",
            (RipperDocGameController_OpenPerkTree x) =>
            {
                x.self.screen = CyberwareScreenType.Inventory;

                re.Hooks.CallOriginalVoid();

                x.self.screen = CyberwareScreenType.Ripperdoc;
            }
        );

        re.Hooks.Place(
            "RipperDocGameController",
            "OnBack",
            (RipperDocGameController_OnBack x) =>
            {
                x.self.screen = CyberwareScreenType.Inventory;

                var retval = re.Hooks.CallOriginal<re.msgpack.Bool>();

                x.self.screen = CyberwareScreenType.Ripperdoc;

                return retval;
            }
        );
    }
}

[Msgpack.Gen]
public readonly partial record struct RipperDocGameController_OnInitialize(
    RipperDocGameController self
);

[Msgpack.Gen]
public readonly partial record struct RipperDocGameController_OpenPerkTree(
    RipperDocGameController self
);

[Msgpack.Gen]
public readonly partial record struct RipperDocGameController_OnBack(
    RipperDocGameController self,
    IScriptable? userData
);
