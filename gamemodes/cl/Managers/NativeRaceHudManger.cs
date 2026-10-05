public static class NativeRaceHudManager
{
    private static hud.CarRaceController? _controller = null;
    private static bool _raceActive = false;

    public static void Register()
    {
        re.Hooks.Place(
            "hudCarRaceController",
            "OnInitialize",
            (hudCarRaceController_OnInitialize self) =>
            {
                _controller = self.self;
                return re.Hooks.CallOriginal<re.msgpack.Bool>();
            }
        );

        re.Hooks.Place(
            "hudCarRaceController",
            "OnUninitialize",
            (hudCarRaceController_OnUninitialize self) =>
            {
                _controller = null;
                return re.Hooks.CallOriginal<re.msgpack.Bool>();
            }
        );

        re.Hooks.Place(
            "hudCarRaceController",
            "OnForwardVehicleRaceUIEvent",
            (hudCarRaceController_OnForwardVehicleRaceUIEvent self) =>
            {
                _controller = self.self;
                if (self.evt != null && self.evt.mode == vehicle.RaceUI.CountdownStart)
                {
                    var instance = ScriptGameInstance.Get();

                    var timeTillStart =
                        EngineTime.ToFloat(ScriptGameInstance.GetTimeSystem(instance)!.GetSimTime())
                        + 5.0f;
                    _controller.raceStartEngineTime = EngineTime.FromFloat(timeTillStart);
                }

                return re.Hooks.CallOriginal<re.msgpack.Bool>();
            }
        );
    }

    public static void PreRaceSetup(int totalCheckpoints, int playerCount, int gridPosition)
    {
        Console.WriteLine(
            $"[NativeRaceHudManager] PreRaceSetup ({totalCheckpoints} checkpoints, {playerCount} players)"
        );
        _raceActive = true;

        var instance = ScriptGameInstance.Get();

        ScriptGameInstance
            .GetUISystem(instance)
            ?.QueueEvent(
                new ForwardVehicleRaceUIEvent
                {
                    maxPosition = playerCount,
                    maxCheckpoints = totalCheckpoints,
                    mode = vehicle.RaceUI.PreRaceSetup,
                }
            );

        ScriptGameInstance.GetUISystem(instance)?.PushGameContext(UIGameContext.VehicleRace);

        var bbDefs = game.bb.AllScriptDefinitions.Get()!.UI_ActiveVehicleData;
        if (bbDefs != null)
        {
            ScriptGameInstance
                .GetBlackboardSystem(instance)
                ?.Get(bbDefs)
                ?.SetInt(bbDefs.PositionInRace, gridPosition + 1, true);
            OnCheckpointPassed(0);
        }

        Console.WriteLine(
            $"[NativeRaceHudManager] Queued PreRaceSetup ({totalCheckpoints} checkpoints, {playerCount} players)"
        );
    }

    public static void StartRace()
    {
        if (_controller == null)
        {
            return;
        }

        var instance = ScriptGameInstance.Get();
        _controller.raceStartEngineTime = ScriptGameInstance.GetTimeSystem(instance)!.GetSimTime();
    }

    public static void Tick(int positionInRace, int totalPlayers)
    {
        if (_controller == null)
        {
            return;
        }

        var instance = ScriptGameInstance.Get();
        var bbDefs = game.bb.AllScriptDefinitions.Get()!.UI_ActiveVehicleData;
        if (bbDefs != null)
        {
            ScriptGameInstance
                .GetBlackboardSystem(instance)
                ?.Get(bbDefs)
                ?.SetInt(bbDefs.PositionInRace, positionInRace, false);
        }

        _controller.playerPosition = 999; //Force a mismatch between bb and stored value to re-render
        _controller.maxPosition = totalPlayers;

        _controller.OnVehicleForwardRaceClockUpdateEvent(new VehicleForwardRaceClockUpdateEvent());
        _controller.OnVehicleForwardRaceCheckpointFactEvent(
            new VehicleForwardRaceCheckpointFactEvent()
        );
    }

    public static void OnCheckpointPassed(int cpNumber)
    {
        var instance = ScriptGameInstance.Get();
        ScriptGameInstance
            .GetQuestsSystem(instance)
            ?.SetFact(new CName("sq024_current_race_checkpoint_fact_add"), cpNumber);
    }

    public static void EndRace()
    {
        if (!_raceActive)
        {
            return;
        }

        _raceActive = false;
        _controller?.EndRace();

        ScriptGameInstance
            .GetUISystem(ScriptGameInstance.Get())
            ?.QueueEvent(new ForwardVehicleRaceUIEvent { mode = vehicle.RaceUI.RaceEnd });

        ScriptGameInstance
            .GetUISystem(ScriptGameInstance.Get())
            ?.QueueEvent(new ForwardVehicleRaceUIEvent { mode = vehicle.RaceUI.Disable });

        var instance = ScriptGameInstance.Get();
        ScriptGameInstance.GetUISystem(instance)?.PopGameContext(UIGameContext.VehicleRace);
        ScriptGameInstance.GetUISystem(instance)?.ResetGameContext();
    }
}

[Msgpack.Gen]
public readonly partial record struct hudCarRaceController_OnInitialize(hud.CarRaceController self);

[Msgpack.Gen]
public readonly partial record struct hudCarRaceController_OnUninitialize(
    hud.CarRaceController self
);

[Msgpack.Gen]
public readonly partial record struct hudCarRaceController_OnForwardVehicleRaceUIEvent(
    hud.CarRaceController self,
    ForwardVehicleRaceUIEvent? evt
);
