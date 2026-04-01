using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace BalatroEffects;

public partial class InspectScreen
{
    private const string vboxName = "BalatroEffectsVBox";
    private const string paginatorName = "BalatroEffectsPaginator";
    private const string sliderName = "BalatroEffectsSlider";
    private static readonly Lazy<Font> LabelFont = new(() =>
        GD.Load<Font>("res://themes/kreon_regular_shared.tres")
    );

    private static string cardId = "UnknownCard";
    private static int currentEffectIndex;

    public static void AddButtons(Control root)
    {
        if (root.HasNode(vboxName))
            return;

        var slider = (NSlider)
            GD.Load<PackedScene>("res://scenes/ui/volume_slider.tscn").Instantiate();
        slider.Name = sliderName;
        slider.ValueChanged += OnSliderChanged;

        var paginator = (Control)
            GD.Load<PackedScene>("res://scenes/screens/paginator.tscn").Instantiate();
        var fxpaginator = new FXPaginator
        {
            Name = paginatorName,
            CustomMinimumSize = new Vector2(0, 64),
        };

        foreach (Node child in paginator.GetChildren().ToArray())
        {
            paginator.RemoveChild(child);
            fxpaginator.AddChild(child);
        }

        var vbox = new VBoxContainer { Name = vboxName };
        vbox.AddThemeConstantOverride("separation", 8);

        root.AddChild(vbox);

        vbox.AddChild(CreateLabel("Effect"));
        vbox.AddChild(fxpaginator);
        vbox.AddChild(CreateDivider());

        vbox.AddChild(CreateLabel("Intensity"));
        vbox.AddChild(slider);

        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        vbox.Position += 50 * Vector2.Left;
        vbox.Position += 25 * Vector2.Up;
    }

    private static ColorRect CreateDivider()
    {
        return new ColorRect()
        {
            CustomMinimumSize = new Vector2(0, 2),
            Color = new Color("E8DCBE40"),
        };
    }

    private static MegaLabel CreateLabel(string text)
    {
        var label = new MegaLabel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinFontSize = 28,
            MaxFontSize = 28,
        };
        label.AddThemeFontOverride(ThemeConstants.Label.Font, LabelFont.Value);
        label.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.cream);
        label.SetTextAutoSize(text);
        return label;
    }

    private static void OnSliderChanged(double value)
    {
        Config.SetIntensity(currentEffectIndex, (float)value / 100.0f);
    }

    public partial class FXPaginator : NPaginator
    {
        public override void _Ready()
        {
            _label = (MegaLabel)FindChild("Label");
            ref MegaLabel vfxField = ref AccessTools.FieldRefAccess<NPaginator, MegaLabel>(
                "_vfxLabel"
            )(this);
            vfxField = (MegaLabel)FindChild("VfxLabel");

            _options.AddRange(["None", "Foil", "Negative", "Polychrome", "Holographic"]);

            _currentIndex = Config.GetEffect(cardId);
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _options.Count - 1);
            _label.SetTextAutoSize(_options[_currentIndex]);
        }

        public void UpdateTargetCard(string newCardId)
        {
            cardId = newCardId;

            int savedIndex = Config.GetEffect(cardId);
            _currentIndex = Mathf.Clamp(savedIndex, 0, _options.Count - 1);

            _label?.SetTextAutoSize(_options[_currentIndex]);
            OnIndexChanged(_currentIndex);
        }

        protected override void OnIndexChanged(int index)
        {
            _currentIndex = index;
            currentEffectIndex = index;
            _label.SetTextAutoSize(_options[index]);
            Config.SetEffect(cardId, index);

            var slider = GetParent().GetNode<NSlider>(sliderName);
            UpdateSliderVisual(slider);
        }
    }

    private static void UpdateSliderVisual(NSlider slider)
    {
        if (slider == null)
            return;
        slider.SetBlockSignals(true);
        slider.Value = Config.GetIntensity(currentEffectIndex) * 100.0;
        slider.SetBlockSignals(false);
    }

    private static readonly AccessTools.FieldRef<NInspectCardScreen, NCard> CardFieldRef =
        AccessTools.FieldRefAccess<NInspectCardScreen, NCard>("_card");

    [HarmonyPatch(typeof(NInspectCardScreen), "SetCard")]
    public static class SetCardPatch
    {
        public static void Postfix(
            NInspectCardScreen __instance,
            int index,
            List<CardModel> ____cards
        )
        {
            if (____cards is null || index < 0 || index >= ____cards.Count)
                return;

            var cardNode = CardFieldRef(__instance);
            ShaderController.ApplyShader(cardNode);
            string cardId = ____cards[index].Id.ToString();

            var paginator = __instance.GetNodeOrNull<FXPaginator>(vboxName + "/" + paginatorName);
            paginator?.UpdateTargetCard(cardId);

            var slider = __instance.GetNodeOrNull<NSlider>(vboxName + "/" + sliderName);
            UpdateSliderVisual(slider);
        }
    }
}
