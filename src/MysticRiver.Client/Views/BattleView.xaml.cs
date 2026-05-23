using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.Extensions.DependencyInjection;

using MysticRiver.Client.Services;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Views;

public partial class BattleView : UserControl {
    private const string playerId = "player";
    private const string enemyId = "enemy";
    private static readonly AbilityOption[] _placeholderAbilities =
    [
        new("Basic Attack", true, new ExecuteAbilityRequest("basic-attack")),
        new("Fireball", false, null),
        new("Ice Lance", false, null),
        new("Healing Light", false, null),
        new("Defense Buff", false, null),
        new("Curse Debuff", false, null),
        new("End Turn", false, null)
    ];

    private readonly BattleApiClient _battleApiClient;
    private readonly BattleRealtimeClient _battleRealtimeClient;
    private readonly ObservableCollection<AbilityOption> _abilities = new();
    private string? battleId;
    private bool isInitialized;
    private bool isAttackInProgress;
    private AbilityOption? selectedAbility;
    private string? selectedTarget;

    public IReadOnlyList<AbilityOption> Abilities => _abilities;

    public BattleView() {
        InitializeComponent();
        _battleApiClient = App.Services.GetRequiredService<BattleApiClient>();
        _battleRealtimeClient = App.Services.GetRequiredService<BattleRealtimeClient>();
        _battleRealtimeClient.BattleStateUpdated += BattleRealtimeClient_BattleStateUpdated;

        SetAbilities(CreatePlaceholderBattleAbilities());
        DataContext = this;
    }

    public async Task InitializeAsync() {
        if (isInitialized) {
            return;
        }

        var response = await _battleApiClient.StartBattleAsync();
        battleId = response.BattleId;
        ApplyState(response.State);

        await LoadAbilitiesAsync();
        await _battleRealtimeClient.JoinBattleAsync(battleId);
        SetStatus("Connected. Real-time updates are active.");
        isInitialized = true;
    }

    private async void AbilityButton_Click(object sender, RoutedEventArgs e) {
        ArgumentNullException.ThrowIfNull(e);

        if (sender is not Button { DataContext: AbilityOption ability }) {
            return;
        }

        if (!ability.IsEnabled || ability.AbilityRequest is null) {
            SetStatus($"{ability.Label} is a placeholder and not wired yet.");
            return;
        }

        if (battleId is null || isAttackInProgress) {
            return;
        }

        // If this ability requires target selection, show target UI instead of executing immediately
        if (ability.RequiresTargetSelection && selectedTarget is null) {
            selectedAbility = ability;
            SetStatus($"Select a target for {ability.Label}");
            return;
        }

        try {
            isAttackInProgress = true;
            SetStatus($"Executing {ability.Label}...");

            // Create request with selected target if applicable
            var request = selectedTarget is not null
                ? ability.AbilityRequest with { TargetId = selectedTarget }
                : ability.AbilityRequest;

            var state = await _battleApiClient.ExecuteAbilityAsync(battleId, request);
            ApplyState(state);
            
            // Reset target selection after execution
            selectedAbility = null;
            selectedTarget = null;
        }
        catch (HttpRequestException exception) {
            SetStatus($"Request failed: {exception.Message}");
        }
        catch (InvalidOperationException exception) {
            SetStatus($"Action failed: {exception.Message}");
        }
        finally {
            isAttackInProgress = false;
        }
    }

    private void BattleRealtimeClient_BattleStateUpdated(object? _, BattleStateUpdatedEvent battleEvent) {
        if (battleId is null || !string.Equals(battleId, battleEvent.BattleId, StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        _ = Dispatcher.InvokeAsync(() => ApplyState(battleEvent.State));
    }

    private void ApplyState(BattleStateDto state) {
        RoundTextBlock.Text = $"Round {state.RoundNumber}";
        PlayerNameTextBlock.Text = state.Creature1.Name;
        EnemyNameTextBlock.Text = state.Creature2.Name;

        // Update player creature stats
        UpdateCreatureDisplay(state.Creature1, PlayerHpTextBlock, PlayerHpBar, PlayerManaTextBlock, PlayerManaBar, PlayerShieldTextBlock, PlayerCCTextBlock, PlayerStatusPanel);

        // Update enemy creature stats
        UpdateCreatureDisplay(state.Creature2, EnemyHpTextBlock, EnemyHpBar, EnemyManaTextBlock, EnemyManaBar, EnemyShieldTextBlock, EnemyCCTextBlock, EnemyStatusPanel);

        UpdateTurnOrder(state);

        if (state.BattleEnded) {
            var winnerLabel = string.Equals(state.WinnerCreatureId, state.Creature1.CreatureId, StringComparison.OrdinalIgnoreCase)
                ? state.Creature1.Name
                : state.Creature2.Name;
            SetStatus($"Battle ended. Winner: {winnerLabel}.");
        }
        else {
            SetStatus("Battle in progress.");
        }
    }

    private void UpdateCreatureDisplay(
        BattleCreatureDto creature,
        TextBlock hpTextBlock,
        Border hpBar,
        TextBlock manaTextBlock,
        Border manaBar,
        TextBlock shieldTextBlock,
        TextBlock ccTextBlock,
        StackPanel statusPanel) {
        
        // Update HP text and bar
        hpTextBlock.Text = $"HP {creature.CurrentHp}/{creature.MaxHp}";
        var hpPercent = creature.MaxHp > 0 ? (double)creature.CurrentHp / creature.MaxHp : 0;
        var hpParent = hpBar.Parent as Border;
        var hpMaxWidth = hpParent?.ActualWidth ?? 260; // Fallback to 260 if not yet rendered
        hpBar.Width = hpPercent * hpMaxWidth;
        
        // Update Mana text and bar
        manaTextBlock.Text = $"Mana {creature.CurrentMana}/{creature.MaxMana}";
        var manaPercent = creature.MaxMana > 0 ? (double)creature.CurrentMana / creature.MaxMana : 0;
        var manaParent = manaBar.Parent as Border;
        var manaMaxWidth = manaParent?.ActualWidth ?? 260; // Fallback to 260 if not yet rendered
        manaBar.Width = manaPercent * manaMaxWidth;
        
        // Update Shield
        shieldTextBlock.Text = creature.CurrentShield > 0 
            ? $"Shield {creature.CurrentShield}" 
            : "Shield 0";
        
        // Update Crowd Control
        ccTextBlock.Text = creature.CrowdControl != CrowdControlKind.None
            ? $"CC: {creature.CrowdControl} ({creature.CrowdControlTurnsRemaining})"
            : "CC: None";
        
        // Update Status Effects
        RenderStatusEffects(creature.StatusEffects, statusPanel);
    }

    private void RenderStatusEffects(IReadOnlyList<StatusEffectStateDto> effects, StackPanel statusPanel) {
        statusPanel.Children.Clear();
        
        if (effects.Count == 0) {
            return;
        }
        
        foreach (var effect in effects) {
            var badge = new Border {
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 4, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 99, 102, 241)),
                CornerRadius = new CornerRadius(3)
            };
            
            var textBlock = new TextBlock {
                Text = $"{effect.Effect} x{effect.Stacks} ({effect.RemainingTurns})",
                FontSize = 9,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 243, 244, 246))
            };
            
            badge.Child = textBlock;
            statusPanel.Children.Add(badge);
        }
    }

    private void UpdateTurnOrder(BattleStateDto state) {
        TurnOrderDescriptionTextBlock.Text = state.BattleEnded
            ? "Battle finished"
            : "Live next-turn preview";

        if (state.BattleEnded) {
            var winnerText = string.Equals(state.WinnerCreatureId, state.Creature1.CreatureId, StringComparison.OrdinalIgnoreCase)
                ? state.Creature1.Name
                : state.Creature2.Name;
            TurnOrderLine1TextBlock.Text = $"1. {winnerText} (Winner)";
            TurnOrderLine2TextBlock.Text = "2. -";
            TurnOrderLine3TextBlock.Text = "3. -";
            TurnOrderLine4TextBlock.Text = "4. -";
            return;
        }

        var first = state.Creature1.Initiative >= state.Creature2.Initiative ? state.Creature1 : state.Creature2;
        var second = ReferenceEquals(first, state.Creature1) ? state.Creature2 : state.Creature1;

        TurnOrderLine1TextBlock.Text = $"1. {first.Name}";
        TurnOrderLine2TextBlock.Text = $"2. {second.Name}";
        TurnOrderLine3TextBlock.Text = $"3. {first.Name}";
        TurnOrderLine4TextBlock.Text = $"4. {second.Name}";
    }

    private void PlayerCreature_Click(object sender, RoutedEventArgs e) {
        if (selectedAbility is null || !selectedAbility.RequiresTargetSelection) {
            return;
        }

        SelectTarget(playerId);
    }

    private void EnemyCreature_Click(object sender, RoutedEventArgs e) {
        if (selectedAbility is null || !selectedAbility.RequiresTargetSelection) {
            return;
        }

        SelectTarget(enemyId);
    }

    private void SelectTarget(string targetId) {
        selectedTarget = targetId;
        SetStatus($"Target selected. Click to confirm {selectedAbility?.Label}.");
    }

    private void SetStatus(string status) {
        BattleStatusTextBlock.Text = status;
    }

    private static IReadOnlyList<AbilityOption> CreatePlaceholderBattleAbilities() {
        return _placeholderAbilities;
    }

    private async Task LoadAbilitiesAsync() {
        try {
            var abilities = await _battleApiClient.GetAbilitiesAsync();
            if (abilities.Count == 0) {
                SetStatus("No abilities are available from the server yet.");
                return;
            }

            var options = abilities.Select(CreateAbilityOption).ToList();
            SetAbilities(options);
        }
        catch (HttpRequestException exception) {
            SetStatus($"Failed to load abilities: {exception.Message}");
        }
        catch (InvalidOperationException exception) {
            SetStatus($"Failed to load abilities: {exception.Message}");
        }
    }

    private static AbilityOption CreateAbilityOption(AbilityDefinitionDto ability) {
        // Enemy-targeted abilities need target selection; self-targeted abilities don't
        var requiresTargetSelection = ability.Target == AbilityTarget.Enemy;
        var targetId = ability.Target == AbilityTarget.Self ? playerId : null;
        var request = new ExecuteAbilityRequest(ability.Id, TargetId: targetId);
        return new AbilityOption(ability.Name, true, request, requiresTargetSelection);
    }

    private void SetAbilities(IEnumerable<AbilityOption> abilities) {
        _abilities.Clear();
        foreach (var ability in abilities) {
            _abilities.Add(ability);
        }
    }

    public sealed record AbilityOption(
        string Label,
        bool IsEnabled,
        ExecuteAbilityRequest? AbilityRequest,
        bool RequiresTargetSelection = false);
}
