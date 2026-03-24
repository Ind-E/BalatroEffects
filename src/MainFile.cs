using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Skew;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Skew";
    private static SceneTree? tree;

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        new Harmony(ModId).PatchAll();
        Config.Load();

        tree = Engine.GetMainLoop() as SceneTree;
        if (tree is not null)
        {
            tree.NodeAdded += OnNodeAdded;
        }
    }

    private static async void OnNodeAdded(Node node)
    {
        if (tree is null || node is not Control control)
            return;

        if (!control.IsNodeReady())
            await tree.ToSignal(node, Node.SignalName.Ready);

        switch (control.Name)
        {
            case "Card":
                Shaders.ApplyShader(control);
                break;
            case "InspectCardScreen":
                InspectScreen.AddButtons(control);
                break;
            default:
                return;
        }
    }
}
