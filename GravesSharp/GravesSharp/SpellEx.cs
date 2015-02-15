using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace GravesSharp
{
	internal class SpellEx : LeagueSharp.Common.Spell
	{
		public delegate void SpellCastedEventHandler(GameObjectProcessSpellCastEventArgs args);

		public event SpellCastedEventHandler OnSpellExCasted;

		public SpellEx(SpellSlot slot, float range = 3.402823E+38f, TargetSelector.DamageType damageType = TargetSelector.DamageType.Physical)
			: base(slot, range, damageType)
		{
			Obj_AI_Base.OnProcessSpellCast += ProcessSpell;
		}

		public void ForceCastOnUnit(Obj_AI_Base unit, bool packetCast = false)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.CastOnUnit(unit, packetCast);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public void ForceCast(Obj_AI_Base unit, bool packetCast = false, bool aoe = false)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.Cast(unit, packetCast, aoe);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public void ForceCast(bool packetCast = false)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.Cast(packetCast);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public void ForceCast(Vector2 fromPosition, Vector2 toPosition)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.Cast(fromPosition, toPosition);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public void ForceCast(Vector3 fromPosition, Vector3 toPosition)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.Cast(fromPosition, toPosition);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public void ForceCast(Vector2 position, bool packetCast = false)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.Cast(position, packetCast);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public void ForceCast(Vector3 position, bool packetCast = false)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (this.IsReady())
				{
					this.Cast(position, packetCast);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}

		private void ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.IsMe && args.SData.Name == ObjectManager.Player.GetSpell(this.Slot).SData.Name)
			{
				if (OnSpellExCasted != null)
					OnSpellExCasted(args);
			}
		}
	}
}
