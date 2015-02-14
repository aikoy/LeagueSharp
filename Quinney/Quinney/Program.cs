using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Quinney
{
	class Program
	{
		public static Menu m_mMenu;
		public static Orbwalking.Orbwalker m_oOrbwalker;
		public static Obj_AI_Hero myHero;

		private const float m_fWRange = 2100f;

		enum QuinnForm
		{
			Human,
			Bird
		}
		private static QuinnForm m_qfForm
		{
			get
			{
				return ((ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name.Equals("QuinnRFinale")) ? QuinnForm.Bird : QuinnForm.Human);
			}
		}

		private static SpellEx Q;
		private static SpellEx E;
		private static SpellEx R;

		private static float m_fQRange
		{
			get
			{
				return ((m_qfForm == QuinnForm.Bird) ? 275f : Q.Range);
			}
		}

		static void Main(string[] args)
		{
			Game.OnGameStart += Load;
		}

		private static void Load(EventArgs args)
		{
			if (!myHero.BaseSkinName.Equals("Quinn"))
			{
				return;
			}

			Q = new SpellEx(SpellSlot.Q, 1010);
			E = new SpellEx(SpellSlot.E, 800);
			R = new SpellEx(SpellSlot.R, 550);

			Q.SetSkillshot(0.25f, 160f, 1150, true, SkillshotType.SkillshotLine);
			E.SetTargetted(0.25f, 2000f);


			myHero = ObjectManager.Player;
			m_mMenu = new Menu("Quinney", "Quinney", true);

			Menu tsMenu = new Menu("Target Selector", "ts");
			TargetSelector.AddToMenu(tsMenu);
			m_mMenu.AddSubMenu(tsMenu);

			Menu orbwalkMenu = new Menu("Orbwalking", "orbwalk");
			m_mMenu.AddSubMenu(orbwalkMenu);
			m_oOrbwalker = new Orbwalking.Orbwalker(orbwalkMenu);

			Menu comboMenu = new Menu("Combo Options", "combo");
			m_mMenu.AddSubMenu(comboMenu);
				comboMenu.AddItem(new MenuItem("enabled", "Combo Enabled").SetValue(new KeyBind(' ', KeyBindType.Press)));
				comboMenu.AddItem(new MenuItem("useQ", "Use Q Human").SetValue(true));
				comboMenu.AddItem(new MenuItem("useE", "Use E Human").SetValue(new KeyBind('S', KeyBindType.Toggle, true)));
				comboMenu.AddItem(new MenuItem("useQValor", "Use Q Valor").SetValue(true));
				comboMenu.AddItem(new MenuItem("useEValor", "Use E Valor").SetValue(new KeyBind('S', KeyBindType.Toggle, false)));
			Menu harassMenu = new Menu("Harass Options", "harass");
			m_mMenu.AddSubMenu(harassMenu);
				harassMenu.AddItem(new MenuItem("enabled", "Harass Enabled").SetValue(new KeyBind('C', KeyBindType.Press)));
				harassMenu.AddItem(new MenuItem("useQ", "Use Q Human").SetValue(true));
				harassMenu.AddItem(new MenuItem("useE", "Use E Human").SetValue(new KeyBind('S', KeyBindType.Toggle, true)));
			
			MenuItem comboDmg = new MenuItem("comboDmg", "Draw HPBar Combo Damage").SetValue(true);
			Utility.HpBarDamageIndicator.DamageToUnit += delegate(Obj_AI_Hero hero)
			{
				float dmg = 0;
				if (Q.IsReady())
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.Q);
				}
				if (R.IsReady())
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.R);
				}
				dmg += (float)myHero.GetAutoAttackDamage(hero, true) * 4;
				return dmg;
			};
			Utility.HpBarDamageIndicator.Enabled = comboDmg.GetValue<bool>();
			comboDmg.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
			{
				Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
			};

			Menu drawMenu = new Menu("Draw Options", "draw");
			m_mMenu.AddSubMenu(drawMenu);
				drawMenu.AddItem(new MenuItem("Q", "Draw Q Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("W", "Draw W Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("E", "Draw E Range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("R", "Draw R Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("eStatus", "Draw E Combo/Harass Status").SetValue(true));
				drawMenu.AddItem(comboDmg);
			Menu ksMenu = new Menu("KS Options", "ks");
			m_mMenu.AddSubMenu(ksMenu);
				ksMenu.AddItem(new MenuItem("useQ", "KS with Q").SetValue(true));
				ksMenu.AddItem(new MenuItem("useRValor", "KS with R").SetValue(true));
			Menu miscMenu = new Menu("Misc Options", "misc");
			miscMenu.AddSubMenu(miscMenu);
				miscMenu.AddItem(new MenuItem("bushRevealer", "Use W to reveal stealth enemies in brushes").SetValue(true));
				miscMenu.AddItem(new MenuItem("antigapcloser", "Anti-Gapcloser with E").SetValue(true));
				miscMenu.AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
				miscMenu.AddItem(
					new MenuItem("interruptDangerLevel", "Danger level for interrupter").SetValue(new StringList(new string[]
					{
						"Low",
						"Medium",
						"High"
					}, 0)));

			miscMenu.AddToMainMenu();

			BushRevealer revealer = new BushRevealer(m_mMenu.SubMenu("misc").Item("bushRevealer"),
				m_mMenu.SubMenu("combo").Item("enabled"), m_fWRange);
			Orbwalking.BeforeAttack += delegate(Orbwalking.BeforeAttackEventArgs eventArgs)
			{
				if (eventArgs.Target.Type.Equals(myHero.Type) && eventArgs.Target.IsValid<Obj_AI_Hero>() && eventArgs.Target.IsEnemy && HasEBuff((Obj_AI_Hero)eventArgs.Target))
				{
					Orbwalking.AfterAttackEvenH afterAttack = null;
					afterAttack = delegate(AttackableUnit unit, AttackableUnit target)
					{
						if (target == eventArgs.Target)
						{
							AfterEnhancedAttack(unit, target);
							Orbwalking.AfterAttack -= afterAttack;
						}
					};
					Orbwalking.AfterAttack += afterAttack;
					Utility.DelayAction.Add(500, () => Orbwalking.AfterAttack -= afterAttack);
				}
			};
			ReadyForSpellCast.OnReadyForSpellCast += ReadyForSpellCastEvent;
			Game.OnGameUpdate += KillSteal;
			Drawing.OnDraw += Draw;
			Interrupter2.OnInterruptableTarget += InterruptableTarget;
			AntiGapcloser.OnEnemyGapcloser += EnemyGapCloser;
		}

		private static void EnemyGapCloser(ActiveGapcloser gapcloser)
		{
			if (m_mMenu.SubMenu("misc").Item("antigapcloser").GetValue<bool>() && m_qfForm == QuinnForm.Human && E.IsReady() && gapcloser.End.Distance(myHero.Position) <= 275)
			{
				E.Cast(gapcloser.Sender);
			}
		}

		private static void InterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
		{
			if (m_mMenu.SubMenu("misc").Item("interrupt").GetValue<bool>() && m_qfForm == QuinnForm.Human && E.IsReady() &&
			    args.DangerLevel >= ((Interrupter2.DangerLevel)(m_mMenu.SubMenu("misc").Item("interruptDangerLevel").GetValue<StringList>().SelectedIndex)))
			{
				E.Cast(sender);
			}
		}

		private static void Draw(EventArgs args)
		{
			if (m_mMenu.SubMenu("draw").Item("eStatus").GetValue<bool>())
			{
				double x = (Drawing.Width - (Drawing.Width*0.10));
				double y = (Drawing.Height - (Drawing.Height*0.78));

				String text = String.Format("E STATUS: COMBO: {0} - HARASS: {1}",
					(m_mMenu.SubMenu("combo").Item("useE" + (m_qfForm == QuinnForm.Bird ? "Valor" : "")).GetValue<bool>()
						? "ON"
						: "OFF"),
					(m_mMenu.SubMenu("harass").Item("useE" + (m_qfForm == QuinnForm.Bird ? "Valor" : "")).GetValue<bool>()
						? "ON"
						: "OFF"));

				Drawing.DrawText((float)x, (float)y, Color.FromArgb(255, 255, 0, 0), text);
			}	

			foreach (MenuItem item in m_mMenu.SubMenu("draw").Items)
			{
				if (item.Name.Length == 1 && item.GetValue<Circle>().Active)
				{
					float range = GetSpellRange(item.Name[0]);
					if (range != 0.0f)
					{
						Render.Circle.DrawCircle(myHero.Position, range, item.GetValue<Circle>().Color);
					}
				}
			}
		}

		private static float GetSpellRange(char spell)
		{
			switch (spell)
			{
				case 'Q':
					return m_fQRange;
				case 'W':
					return m_fWRange;
				case 'E':
					return E.Range;
				case 'R':
					return R.Range;
			}
			return 0.0f;
		}

		private static void KillSteal(EventArgs args)
		{
			if (m_mMenu.SubMenu("ks").Item("enabled").GetValue<bool>() && !myHero.IsRecalling())
			{
				foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget()))
				{
					if (m_mMenu.SubMenu("ks").Item("useQ").GetValue<bool>() && Q.IsReady() && myHero.Distance(enemy) <= m_fQRange && myHero.GetSpellDamage(enemy, SpellSlot.Q) >= enemy.Health)
					{
						if (m_qfForm == QuinnForm.Human)
							Q.Cast(enemy);
						else
							myHero.Spellbook.CastSpell(SpellSlot.Q);
					}
					if (m_mMenu.SubMenu("ks").Item("useRValor").GetValue<bool>() && myHero.Distance(enemy) <= R.Range && GetRDmg(enemy) >= enemy.Health)
					{
						R.Cast();
					}
				}
			}
		}

		private static float GetRDmg(Obj_AI_Hero target)
		{
			Tuple<float, float> dmgTuple = GetRDmgMinMax(target);
			float dmg = ((target.MaxHealth - target.Health) / target.MaxHealth) * (dmgTuple.Item2 - dmgTuple.Item1);
			dmg += dmgTuple.Item1;
			return dmg;
		}

		private static Tuple<float, float> GetRDmgMinMax(Obj_AI_Hero target)
		{
			if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level > 0)
			{
				float ValorMinDamage = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level * 50 + 50;
				ValorMinDamage += ObjectManager.Player.BaseAttackDamage * 50;

				float ValorMaxDamage = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level * 100 + 100;
				ValorMaxDamage += ObjectManager.Player.BaseAttackDamage * 100;

				return Tuple.Create(ValorMinDamage, ValorMaxDamage);
			}

			return Tuple.Create(0.0f, 0.0f);
		}

		private static void ReadyForSpellCastEvent(ReadyForSpellCastEventArgs args)
		{
			if (args.unit.IsMe)
			{
				Obj_AI_Hero target = (Obj_AI_Hero)args.target;
				if (target == null || target.IsValid<Obj_AI_Hero>())
				{
					target = TargetSelector.GetTarget(m_fQRange, TargetSelector.DamageType.Physical);
					if (target == null)
						return;
				}

				switch (m_qfForm)
				{
					case QuinnForm.Human:
						if (m_mMenu.SubMenu("combo").Item("enabled").GetValue<KeyBind>().Active)
							ComboHuman(target);
						else if (m_mMenu.SubMenu("harass").Item("enabled").GetValue<KeyBind>().Active)
							Harass(target);
						break;
					case QuinnForm.Bird:
						if (m_mMenu.SubMenu("combo").Item("enabled").GetValue<KeyBind>().Active)
							ComboBird(target);
						break;
				}
			}
		}

		private static void ComboHuman(Obj_AI_Hero target)
		{
			if (m_mMenu.SubMenu("combo").Item("useQ").GetValue<bool>() && myHero.Distance(target) <= m_fQRange)
			{
				Q.Cast(target);
			}
			else if (m_mMenu.SubMenu("combo").Item("useE").GetValue<bool>() && myHero.Distance(target) <= E.Range && ShouldCastE(target))
			{
				E.Cast(target);
			}
		}

		private static void ComboBird(Obj_AI_Hero target)
		{
			if (m_mMenu.SubMenu("combo").Item("useQValor").GetValue<bool>() && myHero.Distance(target) <= m_fQRange)
			{
				Q.Cast(target);
			}
			else if (m_mMenu.SubMenu("combo").Item("useEValor").GetValue<bool>() && myHero.Distance(target) <= E.Range && ShouldCastE(target))
			{
				E.Cast(target);
			}
		}

		private static void Harass(Obj_AI_Hero target)
		{
			if (m_mMenu.SubMenu("harass").Item("useQ").GetValue<bool>() && myHero.Distance(target) <= m_fQRange)
			{
				Q.Cast(target);
			}
		}

		private static bool ShouldCastE(AttackableUnit target)
		{
			if (m_qfForm == QuinnForm.Bird)
				return (myHero.Distance(target) > (myHero.AttackRange + 50));
			if (myHero.Distance(target) <= 200)
				return true;
			PredictionOutput output = Prediction.GetPrediction(new PredictionInput
			{
				Unit = (Obj_AI_Hero)target, 
				Delay = E.Delay, 
				Speed = E.Speed
			});
			if (myHero.Distance(output.CastPosition) > myHero.Distance(target.Position))
			{
				return (myHero.Distance(target) >= (E.Range * 0.98));
			}
			return false;
		}

		private static void AfterEnhancedAttack(AttackableUnit unit, AttackableUnit target)
		{
			if (unit.IsMe)
			{
				if (target == null || target.IsValid<Obj_AI_Hero>())
				{
					target = TargetSelector.GetTarget(m_fQRange, TargetSelector.DamageType.Physical);
					if (target == null)
						return;
				}

				if ((myHero.Distance(target) <= E.Range) && 
					(m_mMenu.SubMenu("combo").Item("enabled").GetValue<KeyBind>().Active &&
				     m_mMenu.SubMenu("combo").Item("useE").GetValue<KeyBind>().Active) ||
				    (m_mMenu.SubMenu("combo").Item("enabled").GetValue<KeyBind>().Active &&
				     m_mMenu.SubMenu("harass").Item("useE").GetValue<KeyBind>().Active))
				{
					E.Cast((Obj_AI_Hero)target);
				}
			}
		}

		private static bool HasEBuff(Obj_AI_Hero target)
		{
			return target.Buffs.Any(buffInstance => buffInstance.Name == "");
		}
	}
}
