using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Quinney
{
	internal class BushRevealer //By Beaving & Blm95 -- stolen from SAwarenessBeta by Screeder
	{
		private List<PlayerInfo> _playerInfo;
		private int _lastTimeWarded;
		private int lastGameUpdateTime = 0;
		private readonly MenuItem m_miEnabled;
		private readonly MenuItem m_miComboEnabled;
		private readonly float m_fWRange = 0;

		public BushRevealer(MenuItem enabled, MenuItem comboEnabled, float range)
		{
			_playerInfo = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy).Select(x => new PlayerInfo(x)).ToList();
			Game.OnGameUpdate += Game_OnGameUpdate;

			m_miEnabled = enabled;
			m_miComboEnabled = comboEnabled;
			m_fWRange = range;
		}

		~BushRevealer()
		{
			Game.OnGameUpdate -= Game_OnGameUpdate;
			_playerInfo = null;
		}

		public bool IsActive()
		{
			return m_miEnabled.GetValue<bool>();
		}

		private void Game_OnGameUpdate(EventArgs args)
		{
			if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
				return;

			lastGameUpdateTime = Environment.TickCount;

			int time = Environment.TickCount;

			foreach (PlayerInfo playerInfo in _playerInfo.Where(x => x.Player.IsVisible))
				playerInfo.LastSeen = time;

			if (!ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).IsReady())
				return;

			if (m_miComboEnabled.GetValue<KeyBind>().Active)
			{
				foreach (Obj_AI_Hero enemy in _playerInfo.Where(x =>
					x.Player.IsValid &&
					!x.Player.IsVisible &&
					!x.Player.IsDead &&
					x.Player.Distance(ObjectManager.Player.ServerPosition) < m_fWRange && //check real ward range later
					time - x.LastSeen < 2500).Select(x => x.Player))
				{
					ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W);
				}
			}
		}

		private class GrassLocation
		{
			public readonly int Index;
			public int Count;

			public GrassLocation(int index, int count)
			{
				Index = index;
				Count = count;
			}
		}

		private class PlayerInfo
		{
			public readonly Obj_AI_Hero Player;
			public int LastSeen;

			public PlayerInfo(Obj_AI_Hero player)
			{
				Player = player;
			}
		}
	}
}