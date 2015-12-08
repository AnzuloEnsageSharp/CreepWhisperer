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

        private static readonly Dictionary<Unit, ParticleEffect> Effects = new Dictionary<Unit, ParticleEffect>();

        private static bool isAttacking = false;
        public static void Main(string[] args)
        {
            Menu.AddItem(AggroKeyItem);
            Menu.AddItem(UnaggroKeyItem);
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
            if (isAttacking)
            {
                isAttacking = false;
                me.Hold();
                // TODO: Continue previous action
            }

            // creeps in aggro range
            var creeps = ObjectMgr.GetEntities<Creep>();

            if (Utils.SleepCheck("aggroSleep"))
            {
                // aggro
                if (Game.IsKeyDown(AggroKeyItem.GetValue<KeyBind>().Key))
                {
                    var enemy = GetHeroes(me).FirstOrDefault(x => x.Team != me.Team);
                    me.Attack(enemy);
                    isAttacking = true;
                }

                // unaggro
                if (Game.IsKeyDown(UnaggroKeyItem.GetValue<KeyBind>().Key))
                {
                    var ally = GetHeroes(me).FirstOrDefault(x => x.Team == me.Team);
                    me.Attack(ally);
                    isAttacking = true;
                }

                // apply range effect
                foreach (var creep in creeps)
                {
                    HandleEffect(creep, me);
                }

                Utils.Sleep(Game.Ping, "aggroSleep");
            }

        }

        private static bool IsAggroable(Unit x, Hero me)
        {
            return x != null && x.IsValid && x.IsAlive && x.Team != me.Team && me.Distance2D(x) <= 500 && me.IsVisibleToEnemies;
        }

        private static IOrderedEnumerable<Hero> GetHeroes(Hero me)
        {
            return ObjectMgr.GetEntities<Hero>()
                            .Where(x => x != null && x.IsValid && x.IsAlive && x.IsVisible)
                            .OrderBy(me.Distance2D);
        }

        static void HandleEffect(Unit unit, Hero me)
        {
            if (IsAggroable(unit, me) && GetHeroes(me).Any())
            {
                ParticleEffect effect;
                if (!Effects.TryGetValue(unit, out effect))
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
