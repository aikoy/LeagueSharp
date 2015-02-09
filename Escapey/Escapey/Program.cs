using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Escapey
{
	class Program
	{
		public static Menu m_mMenu;
		private const int m_iFlashRange = 400;
		private static Obj_AI_Hero myHero;
		private static SpellSlot m_iFlashSlot = SpellSlot.Unknown;

		static void Main(string[] args)
		{
			Game.OnGameStart += Load;
		}

		private static void Load(EventArgs args)
		{
			myHero = ObjectManager.Player;

			m_iFlashSlot = GetFlashSlot();
			if (m_iFlashSlot == SpellSlot.Unknown)
			{
				return;
			}

			m_mMenu = new Menu("Escapey", "Escapey", true);
			m_mMenu.AddItem(new MenuItem("enabled", "Enable Flash Escape").SetValue(new KeyBind('S', KeyBindType.Toggle))).ValueChanged += EnableChanged;
			m_mMenu.AddItem(new MenuItem("delay", "Turn Off After X Seconds").SetValue(new Slider(5, 0, 25)));
			m_mMenu.AddItem(new MenuItem("autoLowHP", "HP Percentage to Auto Enable").SetValue(new Slider(0, 0, 100)));
			m_mMenu.AddItem(new MenuItem("distance", "Distance to Flash Away").SetValue(new Slider(100, 50, 1000)));
			m_mMenu.AddItem(new MenuItem("drawEnabled", "Draw Escape Status").SetValue(Color.FromArgb(255, 255, 0, 0)));
			m_mMenu.AddItem(new MenuItem("drawRange", "Draw Flash Range").SetValue(Color.FromArgb(255, 255, 0, 0)));
			m_mMenu.AddToMainMenu();

			Obj_AI_Base.OnProcessSpellCast += ProcessSpell;
		}

		private static SpellSlot GetFlashSlot()
		{
			if (myHero.Spellbook.GetSpell(SpellSlot.Summoner1).Name == "summonerflash")
			{
				return SpellSlot.Summoner1;
			}
			else if (myHero.Spellbook.GetSpell(SpellSlot.Summoner2).Name == "summonerflash")
			{
				return SpellSlot.Summoner2;
			}
			return SpellSlot.Unknown;
		}

		private static void ProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			if (sender.Type == myHero.Type && !sender.IsDead && sender.IsEnemy && args.SData.Name == "summonerflash")
			{
				Vector3 startPos = args.Start;
				Vector3 endPos = args.End;

				Vector3 enemyFlashPos = GetFinalPos(startPos, endPos);
				if (myHero.Distance(enemyFlashPos) <= m_mMenu.Item("distance").GetValue<Slider>().Value)
				{
					Vector3 myPos = myHero.Position;
					Vector3 delta = (myPos - enemyFlashPos).Normalized();
					Vector3 flashPos = myPos + (delta * m_iFlashRange);

					myHero.Spellbook.CastSpell(m_iFlashSlot, flashPos);
				}
			}
		}

		private static Vector3 GetFinalPos(Vector3 start, Vector3 end)
		{
			if (start.Distance(end) <= m_iFlashRange)
			{
				return end;
			}

			return (start - ((start - end).Normalized() * m_iFlashRange));
		}

		private static void EnableChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
		{
			KeyBind val = onValueChangeEventArgs.GetNewValue<KeyBind>();
			if (val.Active && m_mMenu.Item("delay").GetValue<Slider>().Value > 0)
			{
				Utility.DelayAction.ActionList.Clear();
				Utility.DelayAction.Add(m_mMenu.Item("delay").GetValue<Slider>().Value, delegate
				{
					val.Active = false;
					m_mMenu.Item("enabled").SetValue(val);
				});
			}
		}
	}
}
