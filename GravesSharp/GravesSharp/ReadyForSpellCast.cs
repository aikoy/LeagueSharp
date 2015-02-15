using System;
using LeagueSharp;

namespace GravesSharp
{
	public static class ReadyForSpellCast
	{
		public delegate void ReadyForSpellCastEventHandler(ReadyForSpellCastEventArgs args);

		public static event ReadyForSpellCastEventHandler OnReadyForSpellCast;

		static ReadyForSpellCast()
		{
			LeagueSharp.Common.Orbwalking.AfterAttack += AfterAttack;
			LeagueSharp.Game.OnGameUpdate += Tick;
		}

		private static void Tick(EventArgs args)
		{
			if (!ObjectManager.Player.IsDead && CanCastSpells())
			{
				FireReady(ObjectManager.Player, null);
			}
		}

		public static bool CanCastSpells()
		{
			return (!ObjectManager.Player.Spellbook.IsAutoAttacking &&
			        !ObjectManager.Player.Spellbook.IsCastingSpell && 
					!ObjectManager.Player.Spellbook.IsChanneling &&
			        !ObjectManager.Player.Spellbook.IsCharging);
		}

		private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
		{
			FireReady(null, null);
		}

		private static void FireReady(AttackableUnit unit, AttackableUnit target)
		{
			ReadyForSpellCastEventArgs args = new ReadyForSpellCastEventArgs
			{
				target = target, 
				unit = unit
			};
			if (OnReadyForSpellCast != null)
			{
				OnReadyForSpellCast(args);
			}
		}
	}

	public class ReadyForSpellCastEventArgs
	{
		public AttackableUnit target = null;
		public AttackableUnit unit = null;
	}
}
