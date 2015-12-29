using System;
using System.Collections.Generic;
using System.Linq;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

using SharpDX;

namespace CreepWhisperer
{
    class Program
    {
        private static readonly Menu Menu = new Menu("CreepWhisperer", "creepWhisperer", true);

        private static readonly MenuItem AggroKeyItem =
            new MenuItem("aggroKey", "Aggro Key").SetValue(new KeyBind('D', KeyBindType.Press));
        private static readonly MenuItem UnaggroKeyItem =
            new MenuItem("unaggroKey", "Unaggro Key").SetValue(new KeyBind('F', KeyBindType.Press));

        private static readonly MenuItem AggroRangeEffectItem =
            new MenuItem("aggroRangeEffect", "Mark aggroable creeps").SetValue(true);

        private static readonly MenuItem AttackCooldown =
            new MenuItem("AttackCooldown", "AttackCooldown").SetValue(new Slider(1000, 0, 2000));

        private static readonly Dictionary<Unit, ParticleEffect> Effects = new Dictionary<Unit, ParticleEffect>();

        private static Vector3 _currentMovePosition = new Vector3(0, 0, 0);

        private static bool _isAttacking = false;
        private static Vector3 _facingDirection;

        public static void Main(string[] args)
        {
            AggroRangeEffectItem.ValueChanged += Item_ValueChanged;

            Menu.AddItem(AggroRangeEffectItem);
            Menu.AddItem(AggroKeyItem);
            Menu.AddItem(UnaggroKeyItem);
            Menu.AddItem(AttackCooldown);
            Menu.AddToMainMenu();
            
            Game.OnIngameUpdate += Game_OnUpdate;
        }

        // ReSharper disable once InconsistentNaming
        private static void Item_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            foreach (var particleEffect in Effects.Values)
            {
                particleEffect.Dispose();
            }
            Effects.Clear();
        }

        private static void Game_OnUpdate(System.EventArgs args)
        {
            var player = ObjectMgr.LocalPlayer;
            if (!Game.IsInGame || player == null || player.Team == Team.Observer)
            {
                return;
            }

            var me = ObjectMgr.LocalHero;
            if (me == null || !me.IsAlive)
            {
                return;
            }

            // instantly cancel attack
            if (_isAttacking)
            {
                _isAttacking = false;
                me.Move(_facingDirection);
                me.Hold(true);
            }

            var creeps = ObjectMgr.GetEntities<Creep>().ToList();

            if (Utils.SleepCheck("attackSleep"))
            {
                Unit target = null;

                // aggro
                if (Game.IsKeyDown(AggroKeyItem.GetValue<KeyBind>().Key))
                {
                    target = GetHeroes(me).FirstOrDefault(x => x.Team != me.Team);
                }

                // unaggro
                if (Game.IsKeyDown(UnaggroKeyItem.GetValue<KeyBind>().Key))
                {
                    target = GetHeroes(me).FirstOrDefault(x => x.Team == me.Team);
                }

                if (target != null)
                {
                    me.Attack(target);
                    _facingDirection = Prediction.InFront(me, 10);
                    _isAttacking = true;
                    Utils.Sleep(Game.Ping + AttackCooldown.GetValue<Slider>().Value, "attackSleep");
                }
            }

            if (Utils.SleepCheck("aggroDrawing"))
            {
                // apply range effect
                foreach (var creep in creeps)
                {
                    HandleEffect(creep, me);
                }

                Utils.Sleep(Game.Ping + 200, "aggroDrawing");
            }
        }

        private static bool IsAggroable(Unit x, Hero me)
        {
            return x != null && x.IsValid && x.IsAlive && x.Team != me.Team
                && me.Distance2D(x) <= 500 && me.IsVisibleToEnemies;
        }

        private static List<Hero> GetHeroes(Hero me)
        {
            return ObjectMgr.GetEntities<Hero>()
                .Where(x => x != null && x.IsValid && x.IsAlive && x.IsVisible)
                .OrderBy(me.Distance2D).ToList();
        }

        static void HandleEffect(Unit unit, Hero me)
        {
            if (IsAggroable(unit, me) && GetHeroes(me).Any())
            {
                ParticleEffect effect;
                if (!Effects.TryGetValue(unit, out effect) && AggroRangeEffectItem.GetValue<bool>())
                {
                    effect = unit.AddParticleEffect(@"particles\units\heroes\hero_beastmaster\beastmaster_wildaxe_glow.vpcf");
                    Effects.Add(unit, effect);
                }
            }
            else
            {
                ParticleEffect effect;
                if (Effects.TryGetValue(unit, out effect))
                {
                    effect.Dispose();
                    Effects.Remove(unit);
                }
            }
        }

    }
}
