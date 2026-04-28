using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Sls2Mods.Utils.Config;

public sealed partial class NConfigBooleanTickbox : NTickbox
{
    private readonly Func<bool> _getValue;
    private readonly Action<bool> _setValue;
    private readonly Action _onChanged;

    public NConfigBooleanTickbox(Func<bool> getValue, Action<bool> setValue, Action onChanged)
    {
        _getValue = getValue;
        _setValue = setValue;
        _onChanged = onChanged;
        CustomMinimumSize = new Vector2(324f, 64f);
        SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        SizeFlagsVertical = SizeFlags.Fill;
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Pass;
        this.TransferAllNodes(SceneHelper.GetScenePath("screens/settings_tickbox"));
    }

    public override void _Ready()
    {
        ConnectSignals();
        IsTicked = _getValue();
        Toggled += OnToggled;
    }

    public override void _ExitTree()
    {
        Toggled -= OnToggled;
        base._ExitTree();
    }

    private void OnToggled(NTickbox tickbox)
    {
        _setValue(tickbox.IsTicked);
        _onChanged();
    }
}
