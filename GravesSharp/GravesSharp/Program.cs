using System;
//using System.Collections.Generic;
using System.Linq;
//using System.Drawing;
//using System.Globalization;
//using System.Text;
//using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace GravesSharp
{
	class Program
	{
		public static Orbwalking.Orbwalker m_orbwalker;
		public static Menu m_config;
		private static Obj_AI_Hero myHero;
		private static int m_iTurnOffCastE = 0;

		public static Spell Q;
		//public static Spell Q1;
		public static Spell W;
		public static Spell E;
		public static Spell R;
		//public static Spell R1;

		static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += GameLoaded;
		}

		private static void GameLoaded(EventArgs args)
		{
			myHero = ObjectManager.Player;

			if (!myHero.BaseSkinName.Equals("Graves"))
				return;

			Q = new Spell(SpellSlot.Q, 840f);
			//Q1 = new Spell(SpellSlot.Q, 930f);
			W = new Spell(SpellSlot.W, 950f);
			E = new Spell(SpellSlot.E, 450f);
			R = new Spell(SpellSlot.R, 1000f);
			//R1 = new Spell(SpellSlot.R, 1600f);

			Q.SetSkillshot(0.26f, 20f * (float)Math.PI / 180, 1800f, false, SkillshotType.SkillshotCone);
			//Q1.SetSkillshot(0.26f, 50f, 1950f, false, SkillshotType.SkillshotLine);
			W.SetSkillshot(0.35f, 250f, 1650f, false, SkillshotType.SkillshotCircle);
			R.SetSkillshot(0.25f, 120f, 2100f, false, SkillshotType.SkillshotLine);
			//R1.SetSkillshot(0.26f, 20f * (float)Math.PI / 180, 2100f, false, SkillshotType.SkillshotCone);

			m_config = new Menu("GravesSharp", "GravesSharp", true);

			Menu tsMenu = new Menu("Target Selector", "ts");
			TargetSelector.AddToMenu(tsMenu);
			m_config.AddSubMenu(tsMenu);

			Menu orbwalkMenu = new Menu("Orbwalking", "orbwalk");
			m_config.AddSubMenu(orbwalkMenu);
			m_orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);

			Menu comboMenu = new Menu("Combo Options", "combo");
			m_config.AddSubMenu(comboMenu);
				comboMenu.AddItem(new MenuItem("enabled", "Combo Enabled").SetValue(new KeyBind(' ', KeyBindType.Press)));
				comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
				comboMenu.AddItem(new MenuItem("useW", "Use W").SetValue(true));
			Menu harassMenu = new Menu("Harass Options", "harass");
			m_config.AddSubMenu(harassMenu);
				harassMenu.AddItem(new MenuItem("enabled", "Harass Enabled").SetValue(new KeyBind('C', KeyBindType.Press)));
				harassMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
				harassMenu.AddItem(new MenuItem("useW", "Use W").SetValue(false));
				harassMenu.AddItem(new MenuItem("manaSlider", "Harass Mana Percent").SetValue(new Slider(15, 0, 100)));
			/*Menu farmMenu = new Menu("Farm Options", "farm");
			m_config.AddSubMenu(farmMenu);
				farmMenu.AddItem(new MenuItem("enabled", "Farm Enabled").SetValue(new KeyBind('V', KeyBindType.Press)));
			//farm for later
			 */

			MenuItem comboDmg = new MenuItem("comboDmg", "Draw HPBar Combo Damage").SetValue(true);
			Utility.HpBarDamageIndicator.DamageToUnit += delegate(Obj_AI_Hero hero)
			{
				float dmg = 0;
				if (Ready(Q))
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.Q);
				}
				if (Ready(W))
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.W);
				}
				if (Ready(R))
				{
					dmg += (float)myHero.GetSpellDamage(hero, SpellSlot.R);
				}
				dmg += (float)myHero.GetAutoAttackDamage(hero, true) * 3;
				return dmg;
			};
			Utility.HpBarDamageIndicator.Enabled = comboDmg.GetValue<bool>();
			comboDmg.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
			{
				Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
			};

			Menu drawMenu = new Menu("Draw Options", "draw");
			m_config.AddSubMenu(drawMenu);
				drawMenu.AddItem(new MenuItem("Q", "Draw Q Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("W", "Draw W Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("E", "Draw E Range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("R", "Draw R Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
				drawMenu.AddItem(new MenuItem("panicE", "Draw E Panic Status").SetValue(true));
				drawMenu.AddItem(comboDmg);
			Menu miscMenu = new Menu("Misc Options", "misc");
			m_config.AddSubMenu(miscMenu);
				miscMenu.AddItem(new MenuItem("castTime", "When to Cast Spells").SetValue(new StringList(new[] { "After Auto Attack", "Any Time" }, 0))).ValueChanged += CastTimeChanged;
				miscMenu.AddItem(new MenuItem("antiGapCloseE", "Use E as Anti-Gapclose").SetValue(true));
				miscMenu.AddItem(new MenuItem("castE", "Enable E Panic Mode").SetValue(new KeyBind('A', KeyBindType.Toggle))).ValueChanged += CastEChanged;
				miscMenu.AddItem(new MenuItem("castEDisable", "Disable Cast E After x Seconds").SetValue(new Slider(0, 0, 25)));
				miscMenu.AddItem(new MenuItem("useQ", "Use Q in E Panic Mode").SetValue(true));
				miscMenu.AddItem(new MenuItem("waitTimeQ", "Wait Time if Q is Down").SetValue(new Slider(3, 0, 15)));
				miscMenu.AddItem(new MenuItem("turnOffAfterAA", "Turn off E Panic Mode After AA").SetValue(true));
				miscMenu.AddItem(new MenuItem("useW", "Cast W in E Panic Mode").SetValue(true));
				miscMenu.AddItem(new MenuItem("autoUlt", "Auto Aim Ult").SetValue(true));
			Menu ksMenu = new Menu("KS Options", "ks");
			m_config.AddSubMenu(ksMenu);
				ksMenu.AddItem(new MenuItem("ksQ", "KS with Q").SetValue(true));
				ksMenu.AddItem(new MenuItem("ksW", "KS with W").SetValue(false));
				ksMenu.AddItem(new MenuItem("ksR", "KS with R").SetValue(true));

			m_config.AddToMainMenu();

			Game.OnGameUpdate += KillSteal;
			Drawing.OnDraw += Draw;
			Spellbook.OnCastSpell += CastSpell;
			AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
			Orbwalking.AfterAttack += PanicE;

			if (m_config.SubMenu("misc").Item("castTime").GetValue<StringList>().SelectedValue.Equals("After Auto Attack"))
			{
				Orbwalking.AfterAttack += Combo;
				Orbwalking.AfterAttack += Harass;
			}
			else if (m_config.SubMenu("misc").Item("castTime").GetValue<StringList>().SelectedValue.Equals("Any Time"))
			{
				Game.OnGameUpdate += Combo;
				Game.OnGameUpdate += Harass;
			}

			Game.OnGameUpdate += delegate(EventArgs eventArgs)
			{
				if (m_config.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0 && Environment.TickCount >= m_iTurnOffCastE)
				{
					KeyBind bind = m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>();
					bind.Active = false;
					m_config.SubMenu("misc").Item("castE").SetValue(bind);
				}
			};

			Game.PrintChat("<font color=\"#00BFFF\">GravesSharp -</font> <font color=\"#FFFFFF\">Loaded</font>");
		}

		private static void CastTimeChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
		{
			String newVal = onValueChangeEventArgs.GetNewValue<StringList>().SelectedValue;
			if (newVal.Equals("After Auto Attack"))
			{
				Game.OnGameUpdate -= Combo;
				Game.OnGameUpdate -= Harass;

				Orbwalking.AfterAttack += Combo;
				Orbwalking.AfterAttack += Harass;
			}
			else if (newVal.Equals("Any Time"))
			{
				Orbwalking.AfterAttack -= Combo;
				Orbwalking.AfterAttack -= Harass;

				Game.OnGameUpdate += Combo;
				Game.OnGameUpdate += Harass;
			}
		}

		private static void Combo(EventArgs args)
		{
			Obj_AI_Base target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
			
			if (target.IsValid)
				_Combo(target);
		}

		private static void Combo(AttackableUnit unit, AttackableUnit target)
		{
			if (unit.IsMe && target.GetType() == myHero.GetType())
				_Combo((Obj_AI_Base)target);
		}

		private static void _Combo(Obj_AI_Base target)
		{
			if (m_config.SubMenu("combo").Item("enabled").GetValue<KeyBind>().Active)
			{
				bool useQ = m_config.SubMenu("combo").Item("useQ").GetValue<bool>();
				bool useW = m_config.SubMenu("combo").Item("useW").GetValue<bool>();

				if (m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active && Ready(E))
				{
					useQ = (useQ && !(m_config.SubMenu("misc").Item("useQ").GetValue<bool>()));
					useW = (useW && !(m_config.SubMenu("misc").Item("useW").GetValue<bool>()));

					if (!useQ && myHero.Spellbook.GetSpell(SpellSlot.Q).Cooldown > m_config.SubMenu("misc").Item("waitTimeQ").GetValue<Slider>().Value)
					{
						useQ = true;
					}
				}

				if (useQ && Ready(Q))
				{
					Q.Cast(target);
				}
				else if (useW && Ready(W))
				{
					W.Cast(target);
				}
			}
		}

		private static void Harass(EventArgs args)
		{
			Obj_AI_Base target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

			if (target.IsValid)
				_Harass(target);
		}

		private static void Harass(AttackableUnit unit, AttackableUnit target)
		{
			if (unit.IsMe && target.GetType() == myHero.GetType())
				_Harass((Obj_AI_Base)target);
		}

		private static void _Harass(Obj_AI_Base target)
		{
			if (m_config.SubMenu("harass").Item("enabled").GetValue<KeyBind>().Active)
			{
				bool useQ = m_config.SubMenu("harass").Item("useQ").GetValue<bool>();
				bool useW = m_config.SubMenu("harass").Item("useW").GetValue<bool>();

				if (m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active && Ready(E))
				{
					useQ = (useQ && !(m_config.SubMenu("misc").Item("useQ").GetValue<bool>()));
					useW = (useW && !(m_config.SubMenu("misc").Item("useW").GetValue<bool>()));

					if (!useQ && myHero.Spellbook.GetSpell(SpellSlot.Q).Cooldown > m_config.SubMenu("misc").Item("waitTimeQ").GetValue<Slider>().Value)
					{
						useQ = true;
					}
				}

				if (useQ && Ready(Q))
				{
					Q.Cast(target);
				}
				else if (useW && Ready(W))
				{
					W.Cast(target);
				}
			}
		}

		private static void PanicE(AttackableUnit unit, AttackableUnit target)
		{
			if (unit.IsMe && target.GetType() == myHero.GetType())
			{
				_PanicE((Obj_AI_Base)target, new Vector3(0f, 0f, 0f), true);
			}
		}

		private static void _PanicE(Obj_AI_Base target, Vector3 pos, bool mousePos = false)
		{
			if (target.IsValid && target.IsTargetable && m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active && Ready(E))
			{
				if (m_config.SubMenu("misc").Item("useQ").GetValue<bool>())
				{
					if (Ready(Q))
					{
						Q.Cast(target);
						GameObjectProcessSpellCast castSpell = null;
						castSpell = delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
						{
							Utility.DelayAction.Add(1, delegate
							{
								if (sender.IsMe && args.SData.Name.Equals("GravesClusterShot"))
								{
									if (mousePos)
									{
										Vector3 mouseVec = Game.CursorPos;
										Vector3 myPos = myHero.Position;
										Vector3 delta = (myPos - mouseVec).Normalized();
										Vector3 castPos = myPos - (delta * E.Range);

										ForceCast(E, castPos);
										Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
										Utility.DelayAction.Add(1, delegate
										{
											if (m_config.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
											{
												KeyBind bind = m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>();
												bind.Active = false;
												m_config.SubMenu("misc").Item("castE").SetValue(bind);
											}
										});
										return;
									}
									ForceCast(E, pos);
									Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
									Utility.DelayAction.Add(1, delegate
									{
										if (m_config.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
										{
											KeyBind bind = m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>();
											bind.Active = false;
											m_config.SubMenu("misc").Item("castE").SetValue(bind);
										}
									});
								}
							});
						};
						Obj_AI_Base.OnProcessSpellCast += castSpell;
						Utility.DelayAction.Add(500 + Game.Ping, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
						return;
					}
					if ((myHero.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires - Game.Time) <= m_config.SubMenu("misc").Item("waitTimeQ").GetValue<Slider>().Value)
					{
						return;
					}
				}
				else if (m_config.SubMenu("misc").Item("useW").GetValue<bool>() && Ready(W))
				{
					W.Cast(target);
					GameObjectProcessSpellCast castSpell = null;
					castSpell = delegate(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
					{
						Utility.DelayAction.Add(1, delegate
						{
							if (sender.IsMe && args.SData.Name.Equals("GravesSmokeGrenade"))
							{
								if (mousePos)
								{
									Vector3 mouseVec = Game.CursorPos;
									Vector3 myPos = myHero.Position;
									Vector3 delta = (myPos - mouseVec).Normalized();
									Vector3 castPos = myPos - (delta * E.Range);

									ForceCast(E, castPos);
									Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
									Utility.DelayAction.Add(1, delegate
									{
										if (m_config.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
										{
											KeyBind bind = m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>();
											bind.Active = false;
											m_config.SubMenu("misc").Item("castE").SetValue(bind);
										}
									});
									return;
								}
								ForceCast(E, pos);
								Utility.DelayAction.Add(1, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
								Utility.DelayAction.Add(1, delegate
								{
									if (m_config.SubMenu("misc").Item("turnOffAfterAA").GetValue<bool>())
									{
										KeyBind bind = m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>();
										bind.Active = false;
										m_config.SubMenu("misc").Item("castE").SetValue(bind);
									}
								});
							}
						});
					};
					Obj_AI_Base.OnProcessSpellCast += castSpell;
					Utility.DelayAction.Add(50 + Game.Ping, () => Obj_AI_Base.OnProcessSpellCast -= castSpell);
					return;
				}
				if (mousePos)
				{
					Vector3 mouseVec = Game.CursorPos;
					Vector3 myPos = myHero.Position;
					Vector3 delta = (myPos - mouseVec).Normalized();
					Vector3 castPos = myPos - (delta * E.Range);

					E.Cast(castPos);
					return;
				}
				E.Cast(pos);
			}
		}

		public static void ForceCast(Spell s, Vector3 pos)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (Ready(s) && m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active)
				{
					s.Cast(pos);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}
		public static void ForceCast(Spell s, Obj_AI_Base enemy)
		{
			GameUpdate update = null;
			update = delegate(EventArgs args)
			{
				if (Ready(s) && m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active)
				{
					s.Cast(enemy);
					return;
				}
				Game.OnGameUpdate -= update;
			};
			Game.OnGameUpdate += update;
			Utility.DelayAction.Add(2000, () => Game.OnGameUpdate -= update);
		}

		public static bool Ready(Spell spell)
		{
			return (myHero.Spellbook.CanUseSpell(spell.Slot) == SpellState.Ready ||
			        myHero.Spellbook.CanUseSpell(spell.Slot) == SpellState.Surpressed);
		}

		private static void CastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
		{
			if (m_config.SubMenu("misc").Item("autoUlt").GetValue<bool>() && sender.Owner.IsMe)
			{
				if (args.Slot == SpellSlot.R)
				{
					args.Process = false;
					Utility.DelayAction.Add(25, CastR);
				}
			}
		}

		private static void CastR()
		{
			Obj_AI_Hero newTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
			R.Cast(newTarget);
		}

		private static void Draw(EventArgs args)
		{
			if (m_config.SubMenu("draw").Item("panicE").GetValue<bool>() && m_config.SubMenu("misc").Item("castE").GetValue<KeyBind>().Active)
			{
				double x = (Drawing.Width - (Drawing.Width * 0.58));
				double y = (Drawing.Height - (Drawing.Height * 0.78));

				String text = "PANIC MODE ON";
				if (m_config.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0)
				{
					text += String.Format(" - TURNING OFF IN {0} SECONDS", ((m_iTurnOffCastE - Environment.TickCount) / 1000));
				}
				Drawing.DrawText((float)x, (float)y, Color.FromArgb(255, 255, 0, 0), text);
			}

			foreach (MenuItem item in m_config.SubMenu("draw").Items)
			{
				if (item.Name.Length == 1 && item.GetValue<Circle>().Active)
				{
					Spell spell = GetSpell(item.Name[0]);
					if (spell != null)
					{
						Render.Circle.DrawCircle(myHero.Position, spell.Range, item.GetValue<Circle>().Color);
					}
				}
			}
		}

		private static Spell GetSpell(char spell)
		{
			switch (spell)
			{
				case 'Q':
					return Q;
				case 'W':
					return W;
				case 'E':
					return E;
				case 'R':
					return R;
			}
			return null;
		}

		private static void KillSteal(EventArgs args)
		{
			foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget()))
			{
				double damage = 0;
				if (m_config.SubMenu("ks").Item("ksW").GetValue<bool>() && Ready(W) && myHero.Distance(enemy) <= (W.Range - 5))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.W) - 5;
					if (enemy.Health <= damage)
					{
						W.Cast(enemy);
						continue;
					}
				}
				if (m_config.SubMenu("ks").Item("ksQ").GetValue<bool>() && Ready(Q) && myHero.Distance(enemy) <= (Q.Range - 5))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.Q) - 5;
					if (enemy.Health <= damage)
					{
						Q.Cast(enemy);
						continue;
					}
				}
				if (m_config.SubMenu("ks").Item("ksW").GetValue<bool>() && m_config.SubMenu("ks").Item("ksQ").GetValue<bool>() &&
				    Ready(W) && Ready(Q) && myHero.Distance(enemy) <= (Q.Range - 5))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.W) + myHero.GetSpellDamage(enemy, SpellSlot.Q) - 5;
					if (enemy.Health <= damage)
					{
						ForceCast(Q, enemy);
						ForceCast(W, enemy);
						continue;
					}
				}
				if (m_config.SubMenu("ks").Item("ksR").GetValue<bool>() && Ready(R) && myHero.Distance(enemy) <= R.Range && myHero.Distance(enemy) > (Q.Range / 2))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.R) - 5;
					if (enemy.Health <= damage)
					{
						R.Cast(enemy, true);
					}
				}
				if (m_config.SubMenu("ks").Item("ksQ").GetValue<bool>() && m_config.SubMenu("ks").Item("ksR").GetValue<bool>() &&
					Ready(Q) && Ready(R) && myHero.Distance(enemy) <= (Q.Range - 5))
				{
					damage = myHero.GetSpellDamage(enemy, SpellSlot.Q) + myHero.GetSpellDamage(enemy, SpellSlot.R) - 5;
					if (enemy.Health <= damage)
					{
						ForceCast(Q, enemy);
						ForceCast(R, enemy);
						continue;
					}
				}
			}
		}

		private static void CastEChanged(object sender, OnValueChangeEventArgs onValueChangeEventArgs)
		{
			if (m_config.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value > 0 && onValueChangeEventArgs.GetNewValue<KeyBind>().Active)
				m_iTurnOffCastE = Environment.TickCount + (m_config.SubMenu("misc").Item("castEDisable").GetValue<Slider>().Value * 1000);
		}

		static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
		{
			if (m_config.SubMenu("misc").Item("antiGapCloseE").GetValue<bool>() && Ready(E))
			{
				if (myHero.Distance(gapcloser.End) <= E.Range)
				{
					Vector3 endPos = gapcloser.End;
					Vector3 myPos = myHero.Position;
					Vector3 delta = (myPos - endPos).Normalized();
					Vector3 castPos = myPos + (delta * E.Range);

					_PanicE(gapcloser.Sender, castPos);
				}
			}
		}
	}
}