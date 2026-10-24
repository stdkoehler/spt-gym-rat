using System;
using System.Linq;
using EFT.Hideout;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GymRat.Client;

/// <summary>
/// Marker component to identify ProduceView instances modified by GymRat.
/// </summary>
public class GymRatLayoutMarker : MonoBehaviour { }

[HarmonyPatch(typeof(ProduceView))]
public static class ProduceViewPatch
{
    public const string ShowerRecipeId = "66f1e8100000000000000001";
    public const string NutritionRecipeId = "66f1e8100000000000000002";

    private const float ExtraBannerSpacing = 26f;
    private const float DefaultVanillaHeight = 110f;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ProduceView.Show))]
    public static void Postfix_Show(ProduceView __instance, ProductionScheme scheme)
    {
        ApplyCustomizations(__instance, scheme?._id);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ProduceView.UpdateButtonsActiveness))]
    public static void Postfix_UpdateButtonsActiveness(ProduceView __instance)
    {
        ApplyCustomizations(__instance, __instance?.Scheme?._id);
    }

    private static void ApplyCustomizations(ProduceView view, string? schemeId)
    {
        try
        {
            if (view == null || string.IsNullOrEmpty(schemeId)) return;

            bool isShower = schemeId == ShowerRecipeId;
            bool isNutrition = schemeId == NutritionRecipeId;

            var banner = view.transform.Find("GymRatBanner");

            // CRITICAL: Vanilla crafts must NEVER be touched or compressed.
            if (!isShower && !isNutrition)
            {
                if (banner != null) banner.gameObject.SetActive(false);

                // If this view instance was previously customized for a GymRat craft and recycled,
                // completely revert it back to vanilla state (remove LayoutElement & reset padding).
                var marker = view.GetComponent<GymRatLayoutMarker>();
                if (marker != null)
                {
                    var le = view.GetComponent<LayoutElement>();
                    if (le != null) UnityEngine.Object.Destroy(le);
                    UnityEngine.Object.Destroy(marker);

                    var hlg = view.GetComponent<HorizontalOrVerticalLayoutGroup>()
                              ?? view.GetComponent<LayoutGroup>() as HorizontalOrVerticalLayoutGroup;
                    if (hlg != null)
                    {
                        hlg.padding = new RectOffset(hlg.padding.left, hlg.padding.right, 0, hlg.padding.bottom);
                    }
                }
                return;
            }

            // --- GymRat Crafts (Recovery Shower & Nutrition Regime) Only ---

            // Mark this view as customized by GymRat
            if (view.GetComponent<GymRatLayoutMarker>() == null)
            {
                view.gameObject.AddComponent<GymRatLayoutMarker>();
            }

            var viewRect = view.GetComponent<RectTransform>();

            // Determine the true baseline height of a vanilla craft card in this station
            float baselineHeight = DefaultVanillaHeight;
            if (view.transform.parent != null)
            {
                foreach (Transform sibling in view.transform.parent)
                {
                    if (sibling != view.transform
                        && sibling.GetComponent<GymRatLayoutMarker>() == null
                        && sibling is RectTransform sr
                        && sr.rect.height > 90f)
                    {
                        baselineHeight = sr.rect.height;
                        break;
                    }
                }
            }
            if (baselineHeight <= 90f && viewRect != null && viewRect.rect.height > 90f)
            {
                baselineHeight = viewRect.rect.height;
            }

            float customHeight = baselineHeight + ExtraBannerSpacing;

            // 1. Give ONLY our craft extra vertical height in the station's scroll list
            var viewLayout = view.GetComponent<LayoutElement>() ?? view.gameObject.AddComponent<LayoutElement>();
            viewLayout.minHeight = customHeight;
            viewLayout.preferredHeight = customHeight;
            if (viewRect != null)
            {
                viewRect.sizeDelta = new Vector2(viewRect.sizeDelta.x, customHeight);
            }

            // 2. Add top padding to shift the items row down by ExtraBannerSpacing
            var layoutGroup = view.GetComponent<HorizontalOrVerticalLayoutGroup>()
                              ?? view.GetComponent<LayoutGroup>() as HorizontalOrVerticalLayoutGroup;
            if (layoutGroup != null)
            {
                layoutGroup.padding = new RectOffset(
                    layoutGroup.padding.left,
                    layoutGroup.padding.right,
                    (int)ExtraBannerSpacing,
                    layoutGroup.padding.bottom);
            }

            // 3. Ensure banner sits in the newly created top space above the items
            TextMeshProUGUI? bannerText = null;
            if (banner == null)
            {
                var bannerGo = new GameObject("GymRatBanner");
                bannerGo.transform.SetParent(view.transform, false);

                // Ignore parent layout so the banner doesn't become a horizontal column
                var layout = bannerGo.GetComponent<LayoutElement>() ?? bannerGo.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;

                // Span across the top of the card above the items
                var rect = bannerGo.GetComponent<RectTransform>() ?? bannerGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(15f, -4f);
                rect.sizeDelta = new Vector2(-30f, 20f);

                bannerText = bannerGo.AddComponent<TextMeshProUGUI>();
                if (view._expectedTime != null)
                {
                    bannerText.font = view._expectedTime.font;
                    bannerText.fontSharedMaterial = view._expectedTime.fontSharedMaterial;
                }
                bannerText.fontSize = 12;
                bannerText.alignment = TextAlignmentOptions.Left;
                bannerText.raycastTarget = false;
                bannerText.enableWordWrapping = false;
                bannerText.overflowMode = TextOverflowModes.Ellipsis;
            }
            else
            {
                banner.gameObject.SetActive(true);
                bannerText = banner.GetComponent<TextMeshProUGUI>();

                var layout = banner.GetComponent<LayoutElement>() ?? banner.gameObject.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;
            }

            if (bannerText != null)
            {
                if (isShower)
                {
                    bannerText.text = "<color=#54c5ff><b>[RECOVERY SHOWER]</b></color> <color=#b2bec3>• Resets Workout Muscle Soreness</color>";
                }
                else
                {
                    bannerText.text = "<color=#f5cd79><b>[NUTRITION REGIME]</b></color> <color=#b2bec3>• Accelerates Muscle Recovery (6x faster)</color>";
                }
            }

            // 4. Customize Start Button
            if (view._startButton != null)
            {
                if (isShower)
                {
                    view._startButton.SetRawText("TAKE SHOWER", 16);
                    view._startButton.SetTooltips("Wash away workout fatigue and completely reset gym muscle soreness.", "");
                }
                else
                {
                    view._startButton.SetRawText("START REGIME", 16);
                    view._startButton.SetTooltips("Prepare a high-nutrition meal that reduces remaining gym soreness by 6x.", "");
                }
            }

            // 5. Customize Get Items Button
            if (view._getItemsButton != null)
            {
                if (isShower)
                {
                    view._getItemsButton.SetRawText("TAKE SHOWER", 16);
                    view._getItemsButton.SetTooltips("Collect items and completely reset gym muscle soreness.", "");
                }
                else
                {
                    view._getItemsButton.SetRawText("FINISH REGIME", 16);
                    view._getItemsButton.SetTooltips("Collect items and reduce remaining gym soreness duration by 6x.", "");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log?.LogError($"[GymRat] Error customizing ProduceView: {ex}");
        }
    }
}
