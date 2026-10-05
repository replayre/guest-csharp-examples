public static class NoClipTools
{
    private const float speedIncrementStep = 0.1f;

    private static bool active = false;
    private static float analogForward = 0f;
    private static float analogBackwards = 0f;
    private static float analogRight = 0f;
    private static float analogLeft = 0f;
    private static float analogUp = 0f;
    private static float analogDown = 0f;
    private static float yaw = 0f;
    private static float speed = 1.0f;
    private static float angle = 0f;
    private static bool inputListenersSetup = false;

    public static void Register()
    {
        re.Con.RegisterCommandWithKeyBind(
            "debug:noclip",
            (string[] args) =>
            {
                if (!inputListenersSetup)
                {
                    ManageInputListeners(false);
                    inputListenersSetup = true;
                }

                active = !active;

                if (active)
                {
                    Future.Spawn(FlyLoop);
                }
            },
            EInputKey.IK_R,
            "[Debug] Toggle NoClip"
        );

        re.Hooks.Place(
            "PlayerPuppet",
            "OnAction",
            (PlayerPuppet_OnAction x) =>
            {
                var instance = ScriptGameInstance.Get();

                var settings = ScriptGameInstance.GetSettingsSystem(instance);

                var actionName = game.input.ScriptListenerAction.GetName(x.action);
                var actionType = game.input.ScriptListenerAction.GetType(x.action);

                bool isPressed = actionType == game.input.ActionType.BUTTON_PRESSED;
                bool isReleased = actionType == game.input.ActionType.BUTTON_RELEASED;
                bool isAxis = actionType == game.input.ActionType.AXIS_CHANGE;
                bool isHoldComplete = actionType == game.input.ActionType.BUTTON_HOLD_COMPLETE;

                if (actionName == new CName("MoveX"))
                {
                    float v = game.input.ScriptListenerAction.GetValue(x.action);
                    if (v < 0)
                    {
                        analogRight = 0;
                        analogLeft = -v;
                    }
                    else if (v > 0)
                    {
                        analogRight = v;
                        analogLeft = 0;
                    }
                    else
                    {
                        analogRight = 0;
                        analogLeft = 0;
                    }
                }
                else if (actionName == new CName("MoveY"))
                {
                    float v = game.input.ScriptListenerAction.GetValue(x.action);
                    if (v < 0)
                    {
                        analogForward = 0;
                        analogBackwards = -v;
                    }
                    else if (v > 0)
                    {
                        analogForward = v;
                        analogBackwards = 0;
                    }
                    else
                    {
                        analogForward = 0;
                        analogBackwards = 0;
                    }
                }
                else if (actionName == new CName("right_trigger") && isAxis)
                {
                    float v = game.input.ScriptListenerAction.GetValue(x.action);
                    analogUp = (v == 0f) ? 0f : v;
                }
                else if (actionName == new CName("left_trigger") && isAxis)
                {
                    float v = game.input.ScriptListenerAction.GetValue(x.action);
                    analogDown = (v == 0f) ? 0f : v;
                }
                if (actionName == new CName("Forward"))
                {
                    if (isPressed)
                        analogForward = 1f;
                    else if (isReleased)
                        analogForward = 0f;
                }
                else if (actionName == new CName("Back"))
                {
                    if (isPressed)
                        analogBackwards = 1f;
                    else if (isReleased)
                        analogBackwards = 0f;
                }
                else if (actionName == new CName("Right"))
                {
                    if (isPressed)
                        analogRight = 1f;
                    else if (isReleased)
                        analogRight = 0f;
                }
                else if (actionName == new CName("Left"))
                {
                    if (isPressed)
                        analogLeft = 1f;
                    else if (isReleased)
                        analogLeft = 0f;
                }
                else if (actionName == new CName("ToggleSprint"))
                {
                    if (isPressed)
                        analogDown = 1f;
                    else if (isReleased)
                        analogDown = 0f;
                }
                else if (actionName == new CName("Jump"))
                {
                    if (isPressed || isHoldComplete)
                        analogUp = 1f;
                    else if (isReleased)
                        analogUp = 0f;
                }
                else if (actionName == new CName("NextWeapon"))
                {
                    if (isPressed && active)
                    {
                        speed += speedIncrementStep;
                        if (speed < 0.001f)
                            speed = 0.001f;
                    }
                }
                else if (actionName == new CName("PreviousWeapon"))
                {
                    if (isPressed && active)
                    {
                        speed = Math.Max(speed - speedIncrementStep, 0.001f);
                    }
                }

                if (actionName == new CName("CameraMouseX"))
                {
                    float xVal = game.input.ScriptListenerAction.GetValue(x.action);

                    var sensVar = settings?.GetVar(
                        new CName("/controls/fppcameramouse"),
                        new CName("FPP_MouseX")
                    );

                    float sens = 1f;
                    try
                    {
                        sens =
                            (float)(sensVar?.Cast<userSettings.VarFloat>()?.GetValue() ?? 1f)
                            / 2.9f;
                    }
                    catch
                    {
                        sens = 1f;
                    }

                    yaw = -(xVal / 35f) * sens;
                }
                else if (actionName == new CName("right_stick_x"))
                {
                    float xVal = game.input.ScriptListenerAction.GetValue(x.action);

                    var sensVar = settings?.GetVar(
                        new CName("/controls/fppcamerapad"),
                        new CName("FPP_PadX")
                    );

                    float sens = 1f;
                    try
                    {
                        sens =
                            (float)(sensVar?.Cast<userSettings.VarFloat>()?.GetValue() ?? 1f) / 10f;
                    }
                    catch
                    {
                        sens = 1f;
                    }

                    yaw = -xVal * 1.7f * sens;
                }

                return re.Hooks.CallOriginal<re.msgpack.Bool>();
            }
        );
    }

    private static void ManageInputListeners(bool cleanup)
    {
        var instance = ScriptGameInstance.Get();
        var playerSys = ScriptGameInstance.GetPlayerSystem(instance)!;

        var puppet = playerSys.GetLocalPlayerControlledGameObject()?.Cast<PlayerPuppet>();
        if (puppet != null)
        {
            void Unreg(string name)
            {
                puppet.UnregisterInputListener(puppet, new CName(name));
            }
            void Reg(string name)
            {
                puppet.RegisterInputListener(puppet, new CName(name));
            }

            Unreg("Forward");
            Unreg("Back");
            Unreg("Right");
            Unreg("Left");
            Unreg("ToggleSprint");
            Unreg("Jump");
            Unreg("MoveX");
            Unreg("MoveY");
            Unreg("right_trigger");
            Unreg("left_trigger");
            Unreg("right_stick_x");
            Unreg("CameraMouseX");
            Unreg("NextWeapon");
            Unreg("PreviousWeapon");

            if (!cleanup)
            {
                Reg("Forward");
                Reg("Back");
                Reg("Right");
                Reg("Left");
                Reg("ToggleSprint");
                Reg("Jump");
                Reg("MoveX");
                Reg("MoveY");
                Reg("right_trigger");
                Reg("left_trigger");
                Reg("right_stick_x");
                Reg("CameraMouseX");
                Reg("NextWeapon");
                Reg("PreviousWeapon");
            }
        }
    }

    private static async Future FlyLoop()
    {
        var instance = ScriptGameInstance.Get();
        var playerSystem = ScriptGameInstance.GetPlayerSystem(instance);
        var cameraSystem = ScriptGameInstance.GetCameraSystem(instance);
        var teleportFacility = ScriptGameInstance.GetTeleportationFacility(instance);

        if (playerSystem == null || cameraSystem == null || teleportFacility == null)
        {
            active = false;
            return;
        }

        var timeLast = TimingHelper.GetTimeSeconds();

        while (active)
        {
            var timeNow = TimingHelper.GetTimeSeconds();
            var dt = (float)(timeNow - timeLast);
            timeLast = timeNow;

            var player = playerSystem.GetLocalPlayerControlledGameObject();
            if (player == null)
            {
                await Future.Yield();
                continue;
            }

            Vector4 newPos = player.GetWorldPosition();

            float step = speed * dt * 15f;

            newPos = CalculateNewPos("forward", newPos, step, cameraSystem, player);
            newPos = CalculateNewPos("backwards", newPos, step, cameraSystem, player);
            newPos = CalculateNewPos("right", newPos, step, cameraSystem, player);
            newPos = CalculateNewPos("left", newPos, step, cameraSystem, player);
            newPos = CalculateNewPos("up", newPos, step, cameraSystem, player);
            newPos = CalculateNewPos("down", newPos, step, cameraSystem, player);

            EulerAngles eul = new()
            {
                Roll = 0,
                Pitch = player.GetWorldYaw() + angle + yaw,
                Yaw = 0,
            };

            teleportFacility.Teleport(player, newPos, eul);

            await Future.Yield();
        }
    }

    private static Vector4 CalculateNewPos(
        string direction,
        Vector4 newPos,
        float baseSpeed,
        game.CameraSystem cameraSystem,
        game.Object player
    )
    {
        float s = baseSpeed;
        switch (direction)
        {
            case "forward":
                s *= analogForward;
                break;
            case "backwards":
                s *= analogBackwards;
                break;
            case "right":
                s *= analogRight;
                break;
            case "left":
                s *= analogLeft;
                break;
            case "up":
                s *= analogUp;
                break;
            case "down":
                s *= analogDown;
                break;
        }

        if (s == 0f)
        {
            return newPos;
        }

        Vector4 vec = new();

        if (direction is "forward" or "backwards")
        {
            vec = cameraSystem.GetActiveCameraForward();
        }
        else if (direction is "right" or "left")
        {
            vec = cameraSystem.GetActiveCameraRight();
        }

        if (direction == "forward" || direction == "right")
        {
            newPos.X += vec.X * s;
            newPos.Y += vec.Y * s;
            newPos.Z += vec.Z * s;
        }
        else if (direction == "backwards" || direction == "left")
        {
            newPos.X -= vec.X * s;
            newPos.Y -= vec.Y * s;
            newPos.Z -= vec.Z * s;
        }
        else if (direction == "up")
        {
            newPos.Z += 0.7f * s;
        }
        else if (direction == "down")
        {
            newPos.Z -= 0.7f * s;
        }

        return newPos;
    }
}

[Msgpack.Gen]
public readonly partial record struct PlayerPuppet_OnAction(
    PlayerPuppet self,
    game.input.ScriptListenerAction action,
    game.input.ScriptListenerActionConsumer consumer
);
