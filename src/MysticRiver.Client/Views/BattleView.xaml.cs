using System.Windows.Controls;

namespace MysticRiver.Client.Views;

public partial class BattleView : UserControl {
    private static readonly string[] _placeholderAbilities =
    [
        "Basic Attack",
        "Fireball",
        "Ice Lance",
        "Healing Light",
        "Defense Buff",
        "Curse Debuff",
        "End Turn"
    ];

    public IReadOnlyList<string> Abilities { get; }

    public BattleView() {
        InitializeComponent();
        Abilities = CreatePlaceholderBattleAbilities();
        DataContext = this;
    }

    private static IReadOnlyList<string> CreatePlaceholderBattleAbilities() {
        return _placeholderAbilities;
    }
}
