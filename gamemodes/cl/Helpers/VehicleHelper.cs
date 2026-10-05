using game.mounting;

public static class VehicleHelper
{
    public static void ForcePlayerExitVehicle()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player != null)
        {
            var mountingFacility = ScriptGameInstance.GetMountingFacility(instance);
            var mountingInfo = mountingFacility?.GetMountingInfoSingleWithIds(player.GetEntityID());
            if (mountingInfo != null && mountingInfo.parentId.id != 0)
            {
                mountingFacility!.Unmount(
                    new UnmountingRequest()
                    {
                        lowLevelMountingInfo = mountingInfo,
                        mountData = new() { isInstant = true },
                    }
                );
            }
        }
    }

    public static void EnterVehicle(ent.EntityID vehicleEntityId)
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
            return;

        var mountingFacility = ScriptGameInstance.GetMountingFacility(instance);
        if (mountingFacility == null)
            return;

        mountingFacility.Mount(
            new MountingRequest()
            {
                lowLevelMountingInfo = new MountingInfo()
                {
                    parentId = vehicleEntityId,
                    childId = player.GetEntityID(),
                    slotId = new MountingSlotId() { id = new CName("seat_front_left") },
                },
                mountData = new() { isInstant = true },
            }
        );
    }
}
