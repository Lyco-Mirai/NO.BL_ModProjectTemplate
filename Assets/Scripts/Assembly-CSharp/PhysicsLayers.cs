using UnityEngine;

public static class PhysicsLayers
{
	public static readonly int Default = 0;

	public static readonly int TransparentFX = 1;

	public static readonly int IgnoreRaycast = 2;

	public static readonly int Cockpit = 3;

	public static readonly int Water = 4;

	public static readonly int UI = 5;

	public static readonly int Statics = 6;

	public static readonly int PP = 7;

	public static readonly int TargetCamPP = 8;

	public static readonly int HUD = 9;

	public static readonly int Effects = 10;

	public static readonly int Ships = 11;

	public static readonly int Sun = 12;

	public static readonly int ExclusionZones = 13;

	public static readonly int CockpitAndExternal = 14;

	public static readonly int IgnoreCollisions = 15;

	public static readonly int PreviewRender = 16;

	public static readonly int EditorSelectOnly = 17;

	public static readonly int GrassBlockerProxy = 18;

	public static readonly LayerMask DefaultMask = 1 << Default;

	public static readonly LayerMask TransparentFXMask = 1 << TransparentFX;

	public static readonly LayerMask IgnoreRaycastMask = 1 << IgnoreRaycast;

	public static readonly LayerMask CockpitMask = 1 << Cockpit;

	public static readonly LayerMask WaterMask = 1 << Water;

	public static readonly LayerMask UIMask = 1 << UI;

	public static readonly LayerMask StaticsMask = 1 << Statics;

	public static readonly LayerMask PPMask = 1 << PP;

	public static readonly LayerMask TargetCamPPMask = 1 << TargetCamPP;

	public static readonly LayerMask HUDMask = 1 << HUD;

	public static readonly LayerMask EffectsMask = 1 << Effects;

	public static readonly LayerMask ShipsMask = 1 << Ships;

	public static readonly LayerMask SunMask = 1 << Sun;

	public static readonly LayerMask ExclusionZonesMask = 1 << ExclusionZones;

	public static readonly LayerMask CockpitAndExternalMask = 1 << CockpitAndExternal;

	public static readonly LayerMask IgnoreCollisionsMask = 1 << IgnoreCollisions;

	public static readonly LayerMask PreviewRenderMask = 1 << PreviewRender;

	public static readonly LayerMask EditorSelectOnlyMask = 1 << EditorSelectOnly;

	public static readonly LayerMask GrassBlockerProxyMask = 1 << GrassBlockerProxy;

	public static readonly LayerMask Everything = -1;
}
