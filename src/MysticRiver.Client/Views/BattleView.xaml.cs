using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;

using MysticRiver.Client.Services;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Views;

public partial class BattleView : UserControl {
    private string playerCreatureId = "player";
    private const string enemyId = "enemy";    
    private string playerDisplayName = "Player";
    private static readonly AbilityOption[] _placeholderAbilities =
    [
        new("Basic Attack", true, new ExecuteAbilityRequest("basic-attack"), 0),
        new("Fireball", false, null, 30),
        new("Ice Lance", false, null, 25),
        new("Healing Light", false, null, 20),
        new("Defense Buff", false, null, 15),
        new("Curse Debuff", false, null, 20),
        new("End Turn", false, null, 0)
    ];

    private readonly BattleApiClient _battleApiClient;
    private readonly BattleRealtimeClient _battleRealtimeClient;
    private readonly ObservableCollection<AbilityOption> _abilities = new();
    private readonly ObservableCollection<ActionLogEntry> _actionLog = new();
    private string? battleId;
    private bool isInitialized;
    private bool isAttackInProgress;
    private string? selectedTarget;
    private int playerCurrentMana;

    public IReadOnlyList<AbilityOption> Abilities => _abilities;
    public ObservableCollection<ActionLogEntry> ActionLog => _actionLog;

    public BattleView() {
        InitializeComponent();
        _battleApiClient = App.Services.GetRequiredService<BattleApiClient>();
        _battleRealtimeClient = App.Services.GetRequiredService<BattleRealtimeClient>();
        _battleRealtimeClient.BattleStateUpdated += BattleRealtimeClient_BattleStateUpdated;
        _battleRealtimeClient.Reconnected += BattleRealtimeClient_Reconnected;
        // Keep the action log scrolled to newest (bottom) when new entries arrive
        _actionLog.CollectionChanged += ActionLog_CollectionChanged;

SetAbilities(CreatePlaceholderBattleAbilities());
        DataContext = this;
    }

    // Cleanup so that abandoning a match resets state and allows re-entering a new battle
    public async Task CleanupAsync() {
        // Clear state and log; allow re-initialization on next StartBattle
        battleId = null;
        isInitialized = false;
        isAttackInProgress = false;
        selectedTarget = null;
        playerCurrentMana = 0;
        _actionLog.Clear();
        SetAbilities(CreatePlaceholderBattleAbilities());
        // Note: we don't dispose the shared BattleRealtimeClient here; just reset local state.
        await Task.CompletedTask;
    }

    public async Task AbandonBattleAsync() {
        if (battleId is null) {
            return;
        }

        SetStatus("Abandoning match...");
        await _battleApiClient.AbandonBattleAsync(battleId, new AbandonBattleRequest(playerCreatureId));
        await _battleRealtimeClient.DisconnectAsync();
        await CleanupAsync();
    }

    public async Task InitializeAsync() {
        if (isInitialized) {
            return;
        }

        var response = await _battleApiClient.StartBattleAsync();
        battleId = response.BattleId;
        selectedTarget = enemyId; // Default to enemy target
        ApplyState(response.State);

        await LoadAbilitiesAsync();
        // Ensure we have a persisted guest identity and use its display name when joining
        var identity = App.Services.GetRequiredService<GuestIdentityService>().GetOrCreateIdentity();
        playerDisplayName = identity.DisplayName;
                // Use the creature id from the state as the claimed player creature id when joining so server
        // authorization aligns the token's player id with the creature ids used in action requests.
        playerCreatureId = response.State.Creature1.CreatureId;
        var token = await _battleRealtimeClient.JoinBattleAsync(battleId, playerCreatureId, playerDisplayName);
        _battleApiClient.SetPlayerToken(token);
        SetStatus($"Connected as {playerDisplayName}. Real-time updates are active. Enemy selected as default target.");
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

        if (playerCurrentMana < ability.ManaCost) {
            SetStatus($"Insufficient mana: {ability.Label} costs {ability.ManaCost} but you only have {playerCurrentMana}.");
            return;
        }

        if (battleId is null || isAttackInProgress) {
            return;
        }

        try {
            isAttackInProgress = true;
            SetStatus($"Executing {ability.Label}...");

            // Create request: preserve explicit TargetId on ability (self-targeted), otherwise use selectedTarget
            var request = ability.AbilityRequest!;
            if (request.TargetId is null && selectedTarget is not null) {
                request = request with { TargetId = selectedTarget };
            }

            var state = await _battleApiClient.ExecuteAbilityAsync(battleId, request);
            ApplyState(state);
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

        _ = Dispatcher.InvokeAsync(() => {
            if (battleEvent.ActionSummaries is not null) {
                foreach (var summary in battleEvent.ActionSummaries) {
                    AppendActionSummary(summary, battleEvent.State);
                }
            }
            ApplyState(battleEvent.State);
        });
    }

    private async void BattleRealtimeClient_Reconnected(object? _, EventArgs __) {
        // On reconnect, re-fetch full battle state from the API to avoid missed events.
        if (battleId is null) { return; }
        try {
            SetStatus("Reconnected. Resyncing state...");
            var state = await _battleApiClient.GetBattleStateAsync(battleId);
            ApplyState(state);
            SetStatus("Resync complete.");
        }
        catch (Exception ex) {
            SetStatus($"Resync failed: {ex.Message}");
        }
    }

    private void ActionLog_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (e.Action == NotifyCollectionChangedAction.Add) {
            _ = Dispatcher.InvokeAsync(() => {
                try {
                    ActionLogScrollViewer?.ScrollToEnd();
                }
                catch { }
            });
        }
    }

    private void ApplyState(BattleStateDto state) {
        RoundTextBlock.Text = $"Round {state.RoundNumber}";
        PlayerNameTextBlock.Text = state.Creature1.Name;
        EnemyNameTextBlock.Text = state.Creature2.Name;

        // Track player mana for button enable/disable
        playerCurrentMana = state.Creature1.CurrentMana;

        // Update player creature stats
        UpdateCreatureDisplay(state.Creature1, PlayerHpTextBlock, PlayerHpBar, PlayerManaTextBlock, PlayerManaBar, PlayerShieldTextBlock, PlayerCCTextBlock, PlayerStatusPanel);

        // Update enemy creature stats
        UpdateCreatureDisplay(state.Creature2, EnemyHpTextBlock, EnemyHpBar, EnemyManaTextBlock, EnemyManaBar, EnemyShieldTextBlock, EnemyCCTextBlock, EnemyStatusPanel);

        UpdateTurnOrder(state);
        UpdateTargetHighlight();
        UpdateAbilityButtonStates();

        if (state.BattleEnded) {
            var winnerLabel = string.Equals(state.WinnerCreatureId, state.Creature1.CreatureId, StringComparison.OrdinalIgnoreCase)
                ? state.Creature1.Name
                : state.Creature2.Name;
            var reasonText = state.EndReason switch {
                BattleEndReason.Forfeit => "forfeit",
                BattleEndReason.Disconnect => "disconnect",
                BattleEndReason.Eliminated => "elimination",
                _ => "battle resolution"
            };
            SetStatus($"Battle ended ({reasonText}). Winner: {winnerLabel}.");
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
        TurnOrderDescriptionTextBlock.Text = state.BattleEnded ? "Battle finished" : "Live next-turn preview";

        var line1Border = FindName("TurnOrderLine1Border") as Border;
        var line2Border = FindName("TurnOrderLine2Border") as Border;
        var line3Border = FindName("TurnOrderLine3Border") as Border;
        var line4Border = FindName("TurnOrderLine4Border") as Border;

        if (state.BattleEnded) {
            var winnerText = string.Equals(state.WinnerCreatureId, state.Creature1.CreatureId, StringComparison.OrdinalIgnoreCase)
                ? state.Creature1.Name
                : state.Creature2.Name;

            TurnOrderLine1TextBlock.Text = $"1. {winnerText} (Winner)";
            TurnOrderLine2TextBlock.Text = "2. -";
            TurnOrderLine3TextBlock.Text = "3. -";
            TurnOrderLine4TextBlock.Text = "4. -";

            if (line1Border is not null) { line1Border.Background = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)); }
            if (line2Border is not null) { line2Border.Background = new SolidColorBrush(Color.FromArgb(102, 40, 49, 73)); }
            if (line3Border is not null) { line3Border.Background = new SolidColorBrush(Color.FromArgb(102, 60, 42, 54)); }
            if (line4Border is not null) { line4Border.Background = new SolidColorBrush(Color.FromArgb(102, 40, 49, 73)); }
            return;
        }

        // Order creatures by EffectiveInitiative (higher acts first)
        var ordered = new List<BattleCreatureDto> { state.Creature1, state.Creature2 }
            .OrderByDescending(c => c.EffectiveInitiative)
            .ToList();

        // Build a repeating next-turn sequence of length 4
        var sequence = new List<BattleCreatureDto>();
        while (sequence.Count < 4) {
            foreach (var c in ordered) {
                sequence.Add(c);
                if (sequence.Count >= 4) { break; }
            }
        }

        var highlights = new[] {
            new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)), // gold - next turn
            new SolidColorBrush(Color.FromArgb(102, 40, 49, 73)),
            new SolidColorBrush(Color.FromArgb(102, 60, 42, 54)),
            new SolidColorBrush(Color.FromArgb(102, 40, 49, 73))
        };

        var borders = new[] { line1Border, line2Border, line3Border, line4Border };
        var tbs = new[] { TurnOrderLine1TextBlock, TurnOrderLine2TextBlock, TurnOrderLine3TextBlock, TurnOrderLine4TextBlock };

        for (var i = 0; i < 4; i++) {
            var c = sequence[i];
            var tb = tbs[i];
            var bd = borders[i];
            if (tb is not null) { tb.Text = $"{i + 1}. {c.Name} ({c.EffectiveInitiative})"; }
            if (bd is not null) { bd.Background = highlights[i]; }
        }
    }

    private void PlayerCreature_Click(object sender, RoutedEventArgs e) {
        SelectTarget(playerCreatureId);
    }

    private void EnemyCreature_Click(object sender, RoutedEventArgs e) {
        SelectTarget(enemyId);
    }

    private void SelectTarget(string targetId) {
        selectedTarget = targetId;
        var targetName = selectedTarget == playerCreatureId ? "You" : "Enemy";
        SetStatus($"Target selected: {targetName}. Click an ability to attack.");
        UpdateTargetHighlight();
    }

    private void UpdateTargetHighlight() {
        var playerPanel = FindName("PlayerCreaturePanel") as Border;
        var enemyPanel = FindName("EnemyCreaturePanel") as Border;
        
        var goldBrush = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)); // Gold highlight
        var playerBlueBrush = new SolidColorBrush(Color.FromArgb(255, 176, 199, 255)); // Original blue
        var enemyPinkBrush = new SolidColorBrush(Color.FromArgb(255, 255, 188, 200)); // Original pink
        
        if (playerPanel is not null) {
            playerPanel.BorderBrush = selectedTarget == playerCreatureId ? goldBrush : playerBlueBrush;
        }
        
        if (enemyPanel is not null) {
            enemyPanel.BorderBrush = selectedTarget == enemyId ? goldBrush : enemyPinkBrush;
        }
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

    private AbilityOption CreateAbilityOption(AbilityDefinitionDto ability) {
        // Self-targeted abilities always target player; others will use selectedTarget
        var targetId = ability.Target == AbilityTarget.Self ? playerCreatureId : null;
        var request = new ExecuteAbilityRequest(ability.Id, playerCreatureId, TargetId: targetId);
        return new AbilityOption(ability.Name, true, request, ability.ManaCost);
    }

    private void SetAbilities(IEnumerable<AbilityOption> abilities) {
        _abilities.Clear();
        foreach (var ability in abilities) {
            _abilities.Add(ability);
        }
    }

    private void UpdateAbilityButtonStates() {
        var updatedAbilities = new List<AbilityOption>();
        foreach (var ability in _abilities) {
            var hasEnoughMana = playerCurrentMana >= ability.ManaCost;
            // Create a new AbilityOption with updated IsEnabled
            var updatedAbility = ability with { IsEnabled = hasEnoughMana };
            updatedAbilities.Add(updatedAbility);
        }

        // Update the collection
        _abilities.Clear();
        foreach (var ability in updatedAbilities) {
            _abilities.Add(ability);
        }
    }

    private void AppendActionSummary(BattleActionSummaryDto summary, BattleStateDto state) {
        try {
            var actorName = summary.ActorId == state.Creature1.CreatureId ? state.Creature1.Name : state.Creature2.Name;
            var targetName = summary.TargetId is null ? "(self)" : (summary.TargetId == state.Creature1.CreatureId ? state.Creature1.Name : state.Creature2.Name);

            var parts = new List<string>();
            foreach (var eff in summary.AppliedEffects) {
                switch (eff.Kind) {
                    case AppliedEffectKind.Damage:
                        parts.Add($"{eff.Amount} dmg to {targetName}");
                        break;
                    case AppliedEffectKind.Heal:
                        parts.Add($"{eff.Amount} heal to {targetName}");
                        break;
                    case AppliedEffectKind.Shield:
                        parts.Add($"Shield +{eff.Amount} to {targetName}");
                        break;
                    case AppliedEffectKind.ManaRestore:
                        parts.Add($"Mana +{eff.Amount} to {targetName}");
                        break;
                    case AppliedEffectKind.ManaBurn:
                        parts.Add($"Mana -{eff.Amount} to {targetName}");
                        break;
                    case AppliedEffectKind.ManaDrain:
                        parts.Add($"Mana drain {eff.Amount} from {targetName}");
                        break;
                    case AppliedEffectKind.Status:
                        parts.Add($"Applied {eff.StatusEffect} to {targetName} ({eff.Turns} turns)");
                        break;
                    case AppliedEffectKind.CrowdControl:
                        parts.Add($"{eff.CrowdControl} on {targetName} for {eff.Turns} turns");
                        break;
                    case AppliedEffectKind.Lifesteal:
                        parts.Add($"Lifesteal {eff.Amount} to {actorName}");
                        break;
                    default:
                        parts.Add($"{eff.Kind} on {targetName}");
                        break;
                }
            }

            var actionText = parts.Count == 0
                ? $"{actorName} forfeited the match."
                : summary.TargetId is null
                    ? $"{actorName} used {summary.Ability.Name}: {string.Join(", ", parts)}"
                    : $"{actorName} used {summary.Ability.Name} on {targetName}: {string.Join(", ", parts)}";

            var isPlayer = summary.ActorId == state.Creature1.CreatureId;
            _actionLog.Add(new ActionLogEntry(actionText, isPlayer));
            // Cap log length (remove oldest)
            while (_actionLog.Count > 200) { _actionLog.RemoveAt(0); }
        }
        catch { /* Don't let logging break UI */ }
    }

    public sealed record AbilityOption(
        string Label,
        bool IsEnabled,
        ExecuteAbilityRequest? AbilityRequest,
        int ManaCost);

    public sealed record ActionLogEntry(string Text, bool IsPlayer);
}
