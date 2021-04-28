using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests
{
    public class ThingEntry : IExposable
    {
        public bool animal;
        public Color color = Color.white;
        public double cost;
        public ThingDef def;
        public bool gear;
        public Gender gender = Gender.None;
        public bool hideFromPortrait;
        private string label;
        public PawnKindDef pawnDef;
        public bool stacks = true;
        public int stackSize;
        public ThingDef stuffDef;
        public Thing thing;
        public Tradeable tradeable;
        public ThingType type;

        public bool Minifiable => def.Minifiable && def.building != null;

        public string Label
        {
            get
            {
                if (label != null)
                {
                    return label;
                }

                if (thing != null && animal)
                {
                    return LabelForAnimal;
                }

                return GenLabel.ThingLabel(def, stuffDef, stackSize).CapitalizeFirst();
            }
        }

        public string LabelNoCount
        {
            get
            {
                if (label != null)
                {
                    return label;
                }

                if (thing != null && animal)
                {
                    return LabelForAnimal;
                }

                return GenLabel.ThingLabel(def, stuffDef).CapitalizeFirst();
            }
        }

        private string LabelForAnimal
        {
            get
            {
                var pawn = thing as Pawn;
                if (pawn != null && pawn.def.race.hasGenders)
                {
                    return pawn.kindDef.label.CapitalizeFirst();
                }

                return pawn?.LabelCap;
            }
        }

        public ThingKey ThingKey => new ThingKey(def, stuffDef, gender);

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Defs.Look(ref stuffDef, "stuffDef");
            Scribe_Values.Look(ref gender, "gender");
            Scribe_Deep.Look(ref thing, "thing");
            Scribe_Deep.Look(ref tradeable, true, "tradeable", null, thing);
            Scribe_Defs.Look(ref pawnDef, "pawnDef");
            Scribe_Values.Look(ref type, "type", ThingType.Other);
            Scribe_Values.Look(ref stackSize, "stackSize");
            Scribe_Values.Look(ref cost, "cost");
            Scribe_Values.Look(ref color, "color");
            Scribe_Values.Look(ref stacks, "stacks");
            Scribe_Values.Look(ref gear, "gear");
            Scribe_Values.Look(ref animal, "animal");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref hideFromPortrait, "hideFromPortrait");
        }

        public ThingEntry Clone()
        {
            var cloned = new ThingEntry
            {
                def = def,
                stuffDef = stuffDef,
                gender = gender,
                tradeable = tradeable == null
                    ? null
                    : new Tradeable(tradeable.FirstThingColony, tradeable.FirstThingTrader),
                thing = thing,
                pawnDef = pawnDef,
                type = type,
                stackSize = stackSize,
                cost = cost,
                color = color,
                stacks = stacks,
                gear = gear,
                animal = animal,
                label = label,
                hideFromPortrait = hideFromPortrait
            };
            return cloned;
        }

        public override string ToString()
        {
            return
                $"[ThingEntry: def = {(def != null ? def.defName : "null")}, stuffDef = {(stuffDef != null ? stuffDef.defName : "null")}, gender = {gender}]";
        }
    }
}