using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ItemRequests;

public static class ExtensionsRect
{
    public static float MiddleY(this Rect rect)
    {
        return rect.y + (rect.height * 0.5f);
    }

    public static void SetRandomQualityWeighted(this Thing thing,
        QualityCategory minQuality = QualityCategory.Awful, QualityCategory maxQuality = QualityCategory.Legendary)
    {
        var val = Random.Range(0, 1f);
        var min = (int)minQuality;
        var max = (int)maxQuality;

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

        return;

        QualityCategory clamp(int toClamp)
        {
            return (QualityCategory)Mathf.Max(Mathf.Min(toClamp, max), min);
        }
    }

    private static void SetQuality(this Thing thing, QualityCategory quality)
    {
        var compQuality = thing is not MinifiedThing minifiedThing
            ? thing.TryGetComp<CompQuality>()
            : minifiedThing.InnerThing.TryGetComp<CompQuality>();
        if (compQuality != null)
        {
            typeof(CompQuality).GetField("qualityInt", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(compQuality, quality);
        }
    }
}