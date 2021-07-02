﻿using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace egocarib_AutoMapMarkers
{
    public class WaypointUtil
    {
        private readonly WaypointMapLayer WaypointMapLayer;
        private readonly IServerPlayer ServerPlayer;
        private static readonly MethodInfo ResendWaypointsMethod =
            typeof(WaypointMapLayer).GetMethod("ResendWaypoints", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool Valid { get { return ServerPlayer != null && WaypointMapLayer != null && ResendWaypointsMethod != null; } }

        public WaypointUtil(IServerPlayer serverPlayer)
        {
            ServerPlayer = serverPlayer;
            WaypointMapLayer = MapMarkerMod.MapManager.MapLayers.FirstOrDefault((MapLayer ml) => ml.GetType() == typeof(WaypointMapLayer)) as WaypointMapLayer;
        }

        public void AddWaypoint(Vec3d position, MapMarkerConfig.Settings.AutoMapMarkerSetting settings)
        {
            if (!Valid)
            {
                MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Unable to create waypoint - ServerPlayer or WaypointMapLayer is inaccessible.");
                return;
            }
            if (position == null || settings == null)
            {
                MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Unable to create waypoint - missing position or settings data.");
                return;
            }
            if (!settings.Enabled)
            {
                return;
            }

            foreach (Waypoint waypoint in WaypointMapLayer.Waypoints)
            {
                double xDiff = Math.Abs(waypoint.Position.X - position.X);
                double zDiff = Math.Abs(waypoint.Position.Z - position.Z);
                if (Math.Max(xDiff, zDiff) < settings.MarkerCoverageRadius)
                {
                    bool sameTitle = waypoint.Title == settings.MarkerTitle;
                    bool sameIcon = waypoint.Icon == settings.MarkerIcon;
                    if (sameTitle && sameIcon)
                    {
                        int? settingColor = settings.MarkerColorInteger;
                        if (settingColor == null || waypoint.Color == settingColor)
                        {
                            return; // Don't create another waypoint - this spot is too close to an existing waypoint
                        }
                    }
                }
            }

            AddWaypointToMap(position, settings.MarkerTitle, settings.MarkerIcon, settings.MarkerColorInteger);
        }

        private void AddWaypointToMap(Vec3d pos, string title, string icon, int? color, bool pinned = false)
        {
            if (!Valid)
            {
                return;
            }
            if (pos == null || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(icon))
            {
                MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Unable to create waypoint - missing position, title, or icon.");
                return;
            }
            if (color == null)
            {
                MapMarkerMod.CoreAPI.Logger.Error("Map Marker Mod: Unable to create waypoint - invalid color.");
                return;
            }

            Waypoint waypoint = new Waypoint()
            {
                Color = (int)color,
                OwningPlayerUid = ServerPlayer.PlayerUID,
                Position = pos,
                Title = title,
                Icon = icon,
                Pinned = pinned
            };

            WaypointMapLayer.Waypoints.Add(waypoint);

            if (MapMarkerConfig.GetSettings(MapMarkerMod.CoreAPI).ChatNotifyOnWaypointCreation)
            {
                Waypoint[] ownwpaypoints = WaypointMapLayer.Waypoints.Where((p) => p.OwningPlayerUid == ServerPlayer.PlayerUID).ToArray();
                MapMarkerMod.Chat(ServerPlayer, Lang.Get("Ok, waypoint nr. {0} added", ownwpaypoints.Length - 1));
            }
            ResendWaypoints();
        }

        private void ResendWaypoints()
        {
            if (Valid)
            {
                ResendWaypointsMethod.Invoke(WaypointMapLayer, new object[] { (ServerPlayer as IServerPlayer) });
            }
        }
    }
}
