using game.damage;
using game.stateMachine.eventt;

public static class EntityHelper
{
    public static void Teleport(Vector4 position, Quaternion orientation)
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        var teleportFacility = ScriptGameInstance.GetTeleportationFacility(instance);
        if (teleportFacility == null)
        {
            return;
        }

        teleportFacility.Teleport(player, position, orientation.ToEuler());
    }

    public static void KillCurrentlyControlledObject()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;
        var damageSys = ScriptGameInstance.GetDamageSystem(instance)!;
        var tweakDBInterface = new game.data.TweakDBInterface()!;

        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        var playerWeak = player.Downgrade()!;

        var attackContext = new game.AttackInitContext()!;
        attackContext.record = tweakDBInterface.GetAttackRecord(new TDBID("Attacks.VehicleImpact"));
        attackContext.instigator = playerWeak;
        attackContext.source = playerWeak;

        var attack = game.IAttack.Create(attackContext);
        if (attack == null)
        {
            return;
        }

        var evt = new game.events.HitEvent()!;
        evt.target = playerWeak;

        evt.attackData = new AttackData()!;
        evt.attackData.AddFlag(hitFlag.Kill, new CName("ForceKill"));
        evt.attackData.AddFlag(hitFlag.CanDamageSelf, new CName("ForceKill"));
        evt.attackData.SetSource(playerWeak);
        evt.attackData.SetInstigator(playerWeak);
        evt.attackData.SetAttackDefinition(attack);

        damageSys.QueueHitEvent(evt, player.Downgrade());
    }

    public static void ClearMenuGodmodesOnControlledObject()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;
        var godModeSys = ScriptGameInstance.GetGodModeSystem(instance)!;

        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        godModeSys.ClearGodMode(player.GetEntityID(), new CName("HubMenu"));
        godModeSys.ClearGodMode(player.GetEntityID(), new CName("WorldMap"));
    }

    public static void ClearStaleStatemachines()
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance);
        var player = playerSys?.GetLocalPlayerControlledGameObject();
        if (player == null)
        {
            return;
        }

        var evt = new RemoveOnDemandStateMachine();
        evt.stateMachineIdentifier.definitionName = new CName("Vehicle");

        player.QueueEvent(evt);
        Console.WriteLine("[EntityHelper] removed stale vehicle statemachine");
    }
}
