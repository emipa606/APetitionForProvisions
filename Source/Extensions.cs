using UnityEngine;
using RimWorld;
using Verse;
using System.Reflection;

namespace ItemRequests
{
    public static class ExtensionsRect
    {
        public static float MiddleX(this Rect rect)
        {
            return rect.x + rect.width * 0.5f;
        }
        public static float MiddleY(this Rect rect)
        {
            return rect.y + rect.height * 0.5f;
        }
        public static float HalfWidth(this Rect rect)
        {
            return rect.width * 0.5f;
        }
        public static float HalfHeight(this Rect rect)
        {
            return rect.height * 0.5f;
        }
        public static Rect OffsetBy(this Rect rect, Vector2 offset)
        {
            return new Rect(rect.position + offset, rect.size);
        }
        public static Rect OffsetBy(this Rect rect, float x, float y)
        {
            return new Rect(rect.position + new Vector2(x, y), rect.size);
        }
        public static Rect MoveTo(this Rect rect, Vector2 position)
        {
            return new Rect(position, rect.size);
        }
        public static Rect MoveTo(this Rect rect, float x, float y)
        {
            return new Rect(new Vector2(x, y), rect.size);
        }
        public static Rect InsetBy(this Rect rect, float left, float top, float right, float bottom)
        {
            return new Rect(rect.x + left, rect.y + top, rect.width - left - right, rect.height - top - bottom);
        }
        public static Rect InsetBy(this Rect rect, Vector2 topLeft, Vector2 bottomRight)
        {
            return new Rect(rect.x + topLeft.x, rect.y + topLeft.y, rect.width - topLeft.x - bottomRight.x, rect.height - topLeft.y - bottomRight.y);
        }
        public static Rect InsetBy(this Rect rect, float amount)
        {
            return rect.InsetBy(amount, amount, amount, amount);
        }
        public static Rect InsetBy(this Rect rect, Vector2 amount)
        {
            return rect.InsetBy(amount, amount);
        }
        public static Rect InsetBy(this Rect rect, float xAmount, float yAmount)
        {
            return rect.InsetBy(new Vector2(xAmount, yAmount), new Vector2(xAmount, yAmount));
        }
        public static Rect Combined(this Rect rect, Rect other)
        {
            Vector2 min = new Vector2(Mathf.Min(rect.xMin, other.xMin), Mathf.Min(rect.yMin, other.yMin));
            Vector2 max = new Vector2(Mathf.Max(rect.xMax, other.xMax), Mathf.Max(rect.yMax, other.yMax));
            return new Rect(min, max - min);
        }
        public static bool Mouseover(this Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        public static void SetQuality(this Thing thing, QualityCategory quality)
        {
            MinifiedThing minifiedThing = thing as MinifiedThing;
            CompQuality compQuality = (minifiedThing == null) ? thing.TryGetComp<CompQuality>() : minifiedThing.InnerThing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                typeof(CompQuality).GetField("qualityInt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(compQuality, quality);
            }
        }

        public static void SetRandomQualityWeighted(this Thing thing, QualityCategory minQuality = QualityCategory.Awful, QualityCategory maxQuality = QualityCategory.Legendary)
        {
            float val = Random.Range(0, 1f);
            int min = (int)minQuality;
            int max = (int)maxQuality;
            QualityCategory clamp(int toClamp) => (QualityCategory)Mathf.Max(Mathf.Min(toClamp, max), min);

            if (val < .05)
            {
                thing.SetQuality(clamp(0));
            }
            else if (val < .2)
            {
                thing.SetQuality(clamp(1));
            }
            else if (val < .6)
            {
                thing.SetQuality(clamp(2));
            }
            else if (val < .85)
            {
                thing.SetQuality(clamp(3));
            }
            else if (val < .95)
            {
                thing.SetQuality(clamp(4));
            }
            else if (val < .98)
            {
                thing.SetQuality(clamp(5));
            }
            else
            {
                thing.SetQuality(clamp(6));
            }
        }
    }

    public static class ExtensionsString
    {
        public static string GenderString(this ThingEntry entry)
        {
            return entry.gender.ToString().ToLower();
        }
    }

}
