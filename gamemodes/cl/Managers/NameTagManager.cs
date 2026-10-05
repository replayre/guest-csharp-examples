public static class NameTagManager
{
    class Entry
    {
        public bool initialized;
        public bool hiddenDueToDistance;
        public bool hiddenDueToRequest = true;
        public bool isFriendly;
        public double lastUpdate;
        public ScriptedPuppet? puppet;
        public ink.ScreenProjection? projection;
        public ink.TextWidget? nametagTextWidget;
        public string text = "<Empty>";
        public bool textChanged;
    }

    static ScriptGameInstance instance = ScriptGameInstance.Get();
    static Dictionary<ulong, Entry> objs = [];
    static ink.ScreenProjectionData? customProjectionData;
    static game.ui.NpcNameplateGameController? controller;
    static ink.CompoundWidget? virtualWindow;
    static bool delayedInit = false;

    public static void Register()
    {
        re.Hooks.Place(
            "gameuiNpcNameplateGameController",
            "OnScreenProjectionUpdate",
            (NpcNameplateGameController_OnScreenProjectionUpdate x) =>
            {
                var retval = re.Hooks.CallOriginal<re.msgpack.Bool>();

                if (!delayedInit)
                {
                    customProjectionData = new ink.ScreenProjectionData
                    {
                        slotComponentName = new CName("UI_Slots"),
                        slotName = new CName("Nameplate"),
                        adjustWithDistance = true,
                        fixedWorldOffset = new Vector4(),
                    };

                    controller = x.self;

                    // Scan for already spawned puppets once in case this bundle was started at runtime
                    //
                    foreach (var ent in ScriptGameInstance.GetEntityList(instance))
                    {
                        var puppet = ent?.Cast<PlayerPuppet>();
                        if (puppet == null)
                        {
                            continue;
                        }

                        InitEntry(puppet);
                    }

                    delayedInit = true;
                }

                var localPlayer = ScriptGameInstance
                    .GetPlayerSystem(instance)
                    ?.GetLocalPlayerControlledGameObject();
                if (localPlayer == null)
                {
                    return retval;
                }

                if (x.projections == null)
                {
                    return retval;
                }

                foreach (var projection in x.projections.data)
                {
                    if (projection == null || projection.GetEntity() == null)
                    {
                        continue;
                    }

                    var puppetId = projection.GetEntity()?.GetEntityID().id ?? 0;
                    if (puppetId == 0 || !objs.ContainsKey(puppetId))
                    {
                        continue;
                    }

                    var widget = objs[puppetId].nametagTextWidget!;

                    widget.SetTranslation(projection.currentPosition);
                    widget.SetScale(ScaleToDistance(projection.distanceToCamera));

                    objs[puppetId].hiddenDueToDistance = projection.distanceToCamera > 10;
                    widget.SetVisible(
                        !objs[puppetId].hiddenDueToDistance && !objs[puppetId].hiddenDueToRequest
                    );

                    if (objs[puppetId].textChanged)
                    {
                        widget.SetText(objs[puppetId].text);
                        objs[puppetId].textChanged = true;
                    }

                    var projectionObject = projection.GetEntity()?.Cast<game.Object>();
                    if (projectionObject == null)
                    {
                        continue;
                    }

                    var timeNow = TimingHelper.GetTimeSeconds();
                    if ((timeNow - objs[puppetId].lastUpdate) < 1)
                    {
                        continue;
                    }

                    widget.BindProperty(
                        new CName("tintColor"),
                        new CName(
                            objs[puppetId].isFriendly
                                ? "MainColors.ActiveGreen"
                                : "MainColors.Neutral"
                        )
                    );

                    //TEMP: replace once out params don't crash anymore
                    //if (SpatialQueriesHelper.IsTargetReachable(localPlayer, projectionObject, new Vector4(), true, true))

                    objs[puppetId].lastUpdate = timeNow;
                }

                return retval;
            }
        );
    }

    public static void Shutdown()
    {
        foreach (var entry in objs)
        {
            var widget = entry.Value.nametagTextWidget?.Cast<ink.Widget>();
            if (virtualWindow != null && widget != null)
            {
                virtualWindow.RemoveChild(widget.Downgrade());
            }

            if (controller != null && entry.Value.projection != null)
            {
                controller.UnregisterScreenProjection(entry.Value.projection!);
            }
        }
        objs.Clear();
    }

    private static void InitEntry(ScriptedPuppet puppet)
    {
        if (puppet == null || controller == null || customProjectionData == null)
        {
            return;
        }

        var puppetId = puppet.GetEntityID().id;
        if (objs.ContainsKey(puppetId))
        {
            return;
        }

        if (puppet.IsControlledByLocalPeer())
        {
            return;
        }

        var rootWidget = controller?.GetRootCompoundWidget()?.Upgrade()?.Cast<ink.Widget>();
        while (true)
        {
            var parent = rootWidget?.parentWidget.Upgrade();
            if (parent == null)
            {
                break;
            }

            rootWidget = parent;
        }

        var rootCompoundWidget = rootWidget?.Cast<ink.CompoundWidget>();
        if (rootCompoundWidget == null)
        {
            return;
        }

        virtualWindow = rootCompoundWidget;

        Entry entry = new Entry
        {
            puppet = puppet,
            projection = controller?.RegisterScreenProjection(customProjectionData),
        };

        if (entry.projection == null)
        {
            return;
        }

        entry.projection.SetEntity(entry.puppet);
        entry.projection.SetEnabled(true);

        entry.nametagTextWidget = new ink.TextWidget();
        entry.nametagTextWidget.SetName(new CName($"Nametag-{puppetId}"));
        entry.nametagTextWidget.SetFontFamily("base\\gameplay\\gui\\fonts\\raj\\raj.inkfontfamily");
        entry.nametagTextWidget.SetFontStyle(new CName("Medium"));
        entry.nametagTextWidget.SetFontSize(15);
        entry.nametagTextWidget.SetLetterCase(text.LetterCase.UpperCase);
        entry.nametagTextWidget.SetText(entry.text);
        entry.nametagTextWidget.SetStyle(
            red.ResourceReferenceScriptToken.FromString(
                "base\\gameplay\\gui\\common\\main_colors.inkstyle"
            )
        );
        entry.nametagTextWidget.BindProperty(
            new CName("tintColor"),
            new CName("MainColors.ActiveGreen")
        );
        entry.nametagTextWidget.SetAnchor(ink.EAnchor.TopLeft);
        entry.nametagTextWidget.SetVisible(true);
        entry.nametagTextWidget.SetAnchorPoint(new Vector2 { X = 0.5f, Y = 0.5f });
        entry.nametagTextWidget.SetHorizontalAlignment(text.HorizontalAlignment.Center);
        entry.nametagTextWidget.SetVerticalAlignment(text.VerticalAlignment.Center);

        virtualWindow.RemoveChildByName(new CName($"Nametag-{puppetId}"));
        entry.nametagTextWidget.Reparent(virtualWindow.Downgrade());

        entry.initialized = true;
        entry.lastUpdate = EngineTime.ToFloat(ScriptGameInstance.GetEngineTime(instance));
        objs.Add(puppetId, entry);
    }

    private static void CleanupEntry(ScriptedPuppet puppet)
    {
        if (puppet == null || controller == null)
        {
            return;
        }

        var puppetId = puppet.GetEntityID().id;
        if (!objs.ContainsKey(puppetId))
        {
            return;
        }

        var widget = objs[puppetId].nametagTextWidget?.Cast<ink.Widget>();
        if (virtualWindow != null && widget != null)
        {
            virtualWindow.RemoveChild(widget.Downgrade());
        }

        if (controller != null && objs[puppetId].projection != null)
        {
            controller.UnregisterScreenProjection(objs[puppetId].projection!);
        }

        objs[puppetId].puppet = null;
        objs[puppetId].initialized = false;
        objs.Remove(puppetId);
    }

    private static Vector2 ScaleToDistance(float distance)
    {
        const float minDistance = 6.0f;
        const float maxDistance = 250.0f;

        distance = Math.Max(minDistance, Math.Min(distance, maxDistance));
        var scale = Math.Max((maxDistance - distance) / (maxDistance - minDistance), 0.1f);

        return new Vector2 { X = scale, Y = scale };
    }

    public static void PuppetAttached(PlayerPuppet puppet)
    {
        InitEntry(puppet);
    }

    public static void PuppetDetached(PlayerPuppet puppet)
    {
        CleanupEntry(puppet);
    }

    public static void SetVisible(re.EntityId id, bool visible)
    {
        if (!id.IsValid())
        {
            return;
        }

        var entityId = re.EntitySystem.GetEntityIdFromNetId(id).id;

        objs.TryGetValue(entityId, out var state);
        if (state != null)
        {
            state.hiddenDueToRequest = !visible;
        }
    }

    public static void SetText(re.EntityId id, string text)
    {
        if (!id.IsValid())
        {
            return;
        }

        var entityId = re.EntitySystem.GetEntityIdFromNetId(id).id;

        objs.TryGetValue(entityId, out var state);
        if (state != null)
        {
            state.text = text;
            state.textChanged = true;
        }
    }

    public static void SetFriendly(re.EntityId id, bool friendly)
    {
        if (!id.IsValid())
        {
            return;
        }

        var entityId = re.EntitySystem.GetEntityIdFromNetId(id).id;

        objs.TryGetValue(entityId, out var state);
        if (state != null)
        {
            state.isFriendly = friendly;
        }
    }
}

[Msgpack.Gen]
public readonly partial record struct NpcNameplateGameController_OnScreenProjectionUpdate(
    game.ui.NpcNameplateGameController self,
    game.ui.ScreenProjectionsData? projections
);
