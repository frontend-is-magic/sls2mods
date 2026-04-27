using BaseLib.Config;
using BaseLib.Config.UI;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Sls2Mods.Utils.Config;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed partial class CardRewardEnchantConfigMenu : SimpleModConfig
{
    private static readonly KeywordBlacklistState BlacklistState = new();

    private readonly IReadOnlyList<string> _keywords;
    private readonly CardRewardEnchantConfig? _fallback;
    private readonly Action<CardRewardEnchantConfig>? _persist;

    public CardRewardEnchantConfigMenu(
        IEnumerable<string>? keywords = null,
        CardRewardEnchantConfig? fallback = null,
        Action<CardRewardEnchantConfig>? persist = null)
    {
        _keywords = KeywordBlacklistState.Normalize(keywords ?? Array.Empty<string>());
        _fallback = fallback;
        _persist = persist;
        if (_persist != null)
        {
            ConfigChanged += OnConfigChanged;
        }
    }

    public static bool Enabled { get; set; } = true;

    [ConfigSlider(0.0, 100.0, 0.1, Format = "{0:0.0}%")]
    public static double EnchantChancePercent { get; set; } = 100.0;

    public static bool LogRolls { get; set; } = true;

    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);
        AddKeywordBlacklistOptions(optionContainer);
        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }

    public static void InitializeFrom(CardRewardEnchantConfig config, IEnumerable<string>? keywords = null)
    {
        var normalized = config.Normalize();
        Enabled = normalized.Enabled;
        EnchantChancePercent = normalized.EnchantChance * 100.0;
        LogRolls = normalized.LogRolls;
        BlacklistState.Initialize(keywords ?? normalized.BlacklistedKeywords, normalized.BlacklistedKeywords);
    }

    public static CardRewardEnchantConfig ToRuntimeConfig(CardRewardEnchantConfig? fallback = null)
    {
        fallback ??= new CardRewardEnchantConfig();
        fallback.Enabled = Enabled;
        fallback.EnchantChance = PercentToProbability(EnchantChancePercent);
        fallback.LogRolls = LogRolls;
        fallback.BlacklistedKeywords = BlacklistState.ToBlacklist();
        return fallback.Normalize();
    }

    public static bool IsKeywordBlacklisted(string keyword)
    {
        return BlacklistState.IsBlacklisted(keyword);
    }

    private void AddKeywordBlacklistOptions(Control optionContainer)
    {
        if (_keywords.Count == 0)
        {
            return;
        }

        var section = CreateCollapsibleSection("Enchantment blacklist", collapsedByDefault: true);
        optionContainer.AddChild(section, forceReadableName: false, Node.InternalMode.Disabled);

        foreach (var keyword in _keywords)
        {
            var tickbox = new NConfigKeywordTickbox(keyword, BlacklistState, Changed);
            var label = CreateRawLabelControl(keyword, 28);
            var row = new NConfigOptionRow(ModPrefix, $"Blacklist {keyword}", label, tickbox)
            {
                UniqueNameInOwner = true,
                Owner = optionContainer
            };
            section.ContentContainer.AddChild(row, forceReadableName: false, Node.InternalMode.Disabled);
        }
    }

    private static double PercentToProbability(double percent)
    {
        if (double.IsNaN(percent))
        {
            return 0;
        }

        return Math.Clamp(percent, 0, 100) / 100.0;
    }

    private void OnConfigChanged(object? sender, EventArgs args)
    {
        var config = ToRuntimeConfig(_fallback);
        _persist?.Invoke(config);
    }

    private sealed partial class NConfigKeywordTickbox : NTickbox
    {
        private readonly string _keyword;
        private readonly KeywordBlacklistState _state;
        private readonly Action _onChanged;

        public NConfigKeywordTickbox(string keyword, KeywordBlacklistState state, Action onChanged)
        {
            _keyword = keyword;
            _state = state;
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
            IsTicked = _state.IsBlacklisted(_keyword);
            Toggled += OnToggled;
        }

        public override void _ExitTree()
        {
            Toggled -= OnToggled;
            base._ExitTree();
        }

        private void OnToggled(NTickbox tickbox)
        {
            _state.SetBlacklisted(_keyword, tickbox.IsTicked);
            _onChanged();
        }
    }
}
