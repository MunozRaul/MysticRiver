using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

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

        try {
            isAttackInProgress = true;
            SetStatus($"Executing {ability.Label}...");

            var state = await _battleApiClient.ExecuteAbilityAsync(battleId, ability.AbilityRequest);
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

        _ = Dispatcher.InvokeAsync(() => ApplyState(battleEvent.State));
    }

    private void ApplyState(BattleStateDto state) {
        RoundTextBlock.Text = $"Round {state.RoundNumber}";
        PlayerNameTextBlock.Text = state.Creature1.Name;
        EnemyNameTextBlock.Text = state.Creature2.Name;
        PlayerStatsTextBlock.Text = $"HP {state.Creature1.CurrentHp}/{state.Creature1.MaxHp} | Initiative {state.Creature1.Initiative}";
        EnemyStatsTextBlock.Text = $"HP {state.Creature2.CurrentHp}/{state.Creature2.MaxHp} | Initiative {state.Creature2.Initiative}";

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
        var targetId = ability.Target == AbilityTarget.Self ? playerId : enemyId;
        var request = new ExecuteAbilityRequest(ability.Id, TargetId: targetId);
        return new AbilityOption(ability.Name, true, request);
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
        ExecuteAbilityRequest? AbilityRequest);
}
