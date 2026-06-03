using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;

using MysticRiver.Client.Services;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Views;

public partial class BattleView : UserControl {
    private string playerCreatureId = "player";
    private string enemyCreatureId = "enemy";
    private string playerDisplayName = "Player";
    private bool isMultiplayer;
    private string? currentTurnCreatureId;
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
        _battleRealtimeClient.BattleLifecycleUpdated += BattleRealtimeClient_BattleLifecycleUpdated;
        _battleRealtimeClient.PlayerTokenRefreshed += BattleRealtimeClient_PlayerTokenRefreshed;
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
        enemyCreatureId = "enemy";
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

                await LoadAbilitiesAsync();
                // Ensure we have a persisted guest identity and use its display name when joining
                var identity = App.Services.GetRequiredService<GuestIdentityService>().GetOrCreateIdentity();
                playerDisplayName = identity.DisplayName;
                // Use the creature id from the state as the claimed player creature id when joining so server
                // authorization aligns the token's player id with the creature ids used in action requests.
                playerCreatureId = response.State.Creature1.CreatureId;
                enemyCreatureId = ResolveEnemyCreatureId(response.State);
                selectedTarget = enemyCreatureId; // Default to enemy target
                ApplyState(response.State);
                var token = await _battleRealtimeClient.JoinBattleAsync(battleId, playerCreatureId, playerDisplayName);
                _battleApiClient.SetPlayerToken(token);
                SetStatus($"Connected as {playerDisplayName}. Real-time updates are active. Enemy selected as default target.");
                isInitialized = true;
    }

    public async Task InitializeMultiplayerAsync(string battleId, BattleStateDto state, string localCreatureId, string localPlayerId, string localDisplayName) {
        if (isInitialized) {
            return;
        }

        this.battleId = battleId;
        playerCreatureId = localCreatureId;
        playerDisplayName = localDisplayName;
        isMultiplayer = true;
        enemyCreatureId = ResolveEnemyCreatureId(state);
        selectedTarget = enemyCreatureId; // Default to enemy target
        ApplyState(state);

        await LoadAbilitiesAsync();
        var token = await _battleRealtimeClient.JoinBattleAsync(battleId, localPlayerId, localDisplayName);
        _battleApiClient.SetPlayerToken(token);
        SetStatus($"Connected as {playerDisplayName} (multiplayer). Turn-based battle in progress.");
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

            // Simple local attack animation for physical moves (e.g., basic-attack)
            var abilityId = ability.AbilityRequest?.AbilityId ?? ability.Label;
            var isPhysical = string.Equals(abilityId, "basic-attack", StringComparison.OrdinalIgnoreCase) || (ability.Label?.Contains("Attack") ?? false);
            if (isPhysical) {
                try {
                    await AnimatePlayerAttackAsync();
                }
                catch { /* Swallow animation failures; don't block the action */ }
            }

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

        _ = Dispatcher.InvokeAsync(async () => {
            if (battleEvent.ActionSummaries is not null) {
                var (_, enemyCreature) = ResolveCreatures(battleEvent.State);
                foreach (var summary in battleEvent.ActionSummaries) {
                    var enemyDidDamage = string.Equals(summary.ActorId, enemyCreature.CreatureId, StringComparison.OrdinalIgnoreCase)
                        && summary.AppliedEffects.Any(effect => effect.Kind == AppliedEffectKind.Damage && effect.Amount > 0);
                    if (enemyDidDamage) {
                        await AnimateEnemyAttackAsync();
                    }

                    var isSpellAttack = IsSpellAttack(summary);
                    if (isSpellAttack) {
                        await AnimateSpellProjectileAsync(summary.ActorId, summary.TargetId);
                    }

                    AppendActionSummary(summary, battleEvent.State);
                }
            }
            ApplyState(battleEvent.State);
        });
    }

    private void BattleRealtimeClient_BattleLifecycleUpdated(object? _, BattleLifecycleEvent lifecycleEvent) {
        if (battleId is null || !string.Equals(battleId, lifecycleEvent.BattleId, StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        _ = Dispatcher.InvokeAsync(() => {
            var status = lifecycleEvent.Kind switch {
                BattleLifecycleEventKind.OpponentJoined => $"{lifecycleEvent.DisplayName ?? "Opponent"} joined the battle.",
                BattleLifecycleEventKind.BattleStarted => "Both players are connected. Battle started.",
                BattleLifecycleEventKind.OpponentDisconnected => $"{lifecycleEvent.DisplayName ?? "Opponent"} disconnected.",
                BattleLifecycleEventKind.BattleEnded => lifecycleEvent.EndReason switch {
                    BattleEndReason.Forfeit => "Battle ended by forfeit.",
                    BattleEndReason.Disconnect => "Battle ended by disconnect.",
                    BattleEndReason.Eliminated => "Battle ended by elimination.",
                    _ => "Battle ended."
                },
                _ => "Battle event received."
            };
            SetStatus(status);
        });
    }

    private void BattleRealtimeClient_PlayerTokenRefreshed(object? _, string token) {
        _battleApiClient.SetPlayerToken(token);
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
        enemyCreatureId = ResolveEnemyCreatureId(state);
        var (playerCreature, enemyCreature) = ResolveCreatures(state);
        RoundTextBlock.Text = $"Round {state.RoundNumber}";
        PlayerNameTextBlock.Text = playerCreature.Name;
        EnemyNameTextBlock.Text = enemyCreature.Name;

        // Track player mana for button enable/disable
        playerCurrentMana = playerCreature.CurrentMana;

        // Store current turn info for multiplayer gating
        currentTurnCreatureId = state.CurrentTurnCreatureId;

        // Update player creature stats
        UpdateCreatureDisplay(playerCreature, PlayerHpTextBlock, PlayerHpBar, PlayerManaTextBlock, PlayerManaBar, PlayerShieldTextBlock, PlayerCCTextBlock, PlayerStatusPanel);

        // Update enemy creature stats
        UpdateCreatureDisplay(enemyCreature, EnemyHpTextBlock, EnemyHpBar, EnemyManaTextBlock, EnemyManaBar, EnemyShieldTextBlock, EnemyCCTextBlock, EnemyStatusPanel);

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
            new SolidColorBrush(Color.FromArgb(255, 255, 215, 0)), // gold - next turn (or current turn in multiplayer)
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
            var isCurrentTurn = isMultiplayer && string.Equals(c.CreatureId, currentTurnCreatureId, StringComparison.OrdinalIgnoreCase);
            var highlightIndex = i == 0 || isCurrentTurn ? 0 : i;
            if (tb is not null) {
                var indicator = isCurrentTurn ? " ★" : "";
                tb.Text = $"{i + 1}. {c.Name} ({c.EffectiveInitiative}){indicator}";
            }
            if (bd is not null) { bd.Background = highlights[highlightIndex]; }
        }
    }

    private void PlayerCreature_Click(object sender, RoutedEventArgs e) {
        SelectTarget(playerCreatureId);
    }

    private void EnemyCreature_Click(object sender, RoutedEventArgs e) {
        SelectTarget(enemyCreatureId);
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
            enemyPanel.BorderBrush = selectedTarget == enemyCreatureId ? goldBrush : enemyPinkBrush;
        }
    }

    private Task AnimatePlayerAttackAsync() {
        // Simple short leap towards the enemy and back
        var transform = FindName("PlayerTranslate") as TranslateTransform ?? (PlayerCreaturePanel.RenderTransform as TranslateTransform);
        if (transform is null) {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        var distance = 120.0; // pixels to leap forward
        var anim = new DoubleAnimation {
            To = distance,
            Duration = TimeSpan.FromMilliseconds(120),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        anim.Completed += (_, __) => tcs.TrySetResult(true);
        transform.BeginAnimation(TranslateTransform.XProperty, anim);
        return tcs.Task;
    }

    private Task AnimateEnemyAttackAsync() {
        // Mirror of the player animation: short leap toward the player and back
        var transform = FindName("EnemyTranslate") as TranslateTransform ?? (EnemyCreaturePanel.RenderTransform as TranslateTransform);
        if (transform is null) {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        var anim = new DoubleAnimation {
            To = -120.0,
            Duration = TimeSpan.FromMilliseconds(120),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        anim.Completed += (_, __) => tcs.TrySetResult(true);
        transform.BeginAnimation(TranslateTransform.XProperty, anim);
        return tcs.Task;
    }

    private Task AnimateSpellProjectileAsync(string actorId, string? targetId) {
        var sourcePanel = string.Equals(actorId, playerCreatureId, StringComparison.OrdinalIgnoreCase) ? PlayerCreaturePanel : EnemyCreaturePanel;
        var targetCreatureId = targetId ?? (string.Equals(actorId, playerCreatureId, StringComparison.OrdinalIgnoreCase) ? enemyCreatureId : playerCreatureId);
        var targetPanel = string.Equals(targetCreatureId, playerCreatureId, StringComparison.OrdinalIgnoreCase) ? PlayerCreaturePanel : EnemyCreaturePanel;

        if (sourcePanel is null || targetPanel is null || BattleEffectCanvas is null) {
            return Task.CompletedTask;
        }

        var sourcePoint = sourcePanel.TranslatePoint(new Point(sourcePanel.ActualWidth / 2, sourcePanel.ActualHeight / 2), BattleEffectCanvas);
        var targetPoint = targetPanel.TranslatePoint(new Point(targetPanel.ActualWidth / 2, targetPanel.ActualHeight / 2), BattleEffectCanvas);

        var projectile = new Ellipse {
            Width = 30,
            Height = 30,
            Opacity = 0.0,
            IsHitTestVisible = false,
            Fill = new RadialGradientBrush(
                Color.FromRgb(255, 236, 179),
                Color.FromRgb(244, 114, 182))
            {
                GradientOrigin = new Point(0.35, 0.35),
                Center = new Point(0.45, 0.45),
                RadiusX = 0.55,
                RadiusY = 0.55
            },
            RenderTransform = new ScaleTransform(1.0, 1.0)
        };

        Canvas.SetLeft(projectile, sourcePoint.X - projectile.Width / 2);
        Canvas.SetTop(projectile, sourcePoint.Y - projectile.Height / 2);
        BattleEffectCanvas.Children.Add(projectile);

        var tcs = new TaskCompletionSource<bool>();
        var duration = TimeSpan.FromMilliseconds(1040);
        var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        var leftAnimation = new DoubleAnimation {
            To = targetPoint.X - projectile.Width / 2,
            Duration = duration,
            EasingFunction = easing
        };

        var topAnimation = new DoubleAnimation {
            To = targetPoint.Y - projectile.Height / 2,
            Duration = duration,
            EasingFunction = easing
        };

        var opacityAnimation = new DoubleAnimation {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(360),
            AutoReverse = true,
            EasingFunction = easing
        };

        var scaleAnimation = new DoubleAnimation {
            From = 1.0,
            To = 1.45,
            Duration = TimeSpan.FromMilliseconds(520),
            AutoReverse = true,
            EasingFunction = easing
        };

        topAnimation.Completed += (_, __) => {
            BattleEffectCanvas.Children.Remove(projectile);
            tcs.TrySetResult(true);
        };

        projectile.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        projectile.BeginAnimation(Canvas.LeftProperty, leftAnimation);
        projectile.BeginAnimation(Canvas.TopProperty, topAnimation);
        if (projectile.RenderTransform is ScaleTransform scaleTransform) {
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }
        return tcs.Task;
    }

    private static bool IsSpellAttack(BattleActionSummaryDto summary) {
        if (!summary.AppliedEffects.Any(effect => effect.Kind == AppliedEffectKind.Damage && effect.Amount > 0)) {
            return false;
        }

        return !string.Equals(summary.Ability.Id, "basic-attack", StringComparison.OrdinalIgnoreCase)
            && !summary.Ability.Name.Contains("Attack", StringComparison.OrdinalIgnoreCase);
    }

    private (BattleCreatureDto Player, BattleCreatureDto Enemy) ResolveCreatures(BattleStateDto state) {
        return string.Equals(playerCreatureId, state.Creature1.CreatureId, StringComparison.OrdinalIgnoreCase)
            ? (state.Creature1, state.Creature2)
            : (state.Creature2, state.Creature1);
    }

    private string ResolveEnemyCreatureId(BattleStateDto state) {
        var (_, enemyCreature) = ResolveCreatures(state);
        return enemyCreature.CreatureId;
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
        var isPlayerTurn = !isMultiplayer || string.Equals(currentTurnCreatureId, playerCreatureId, StringComparison.OrdinalIgnoreCase);
        var updatedAbilities = new List<AbilityOption>();
        foreach (var ability in _abilities) {
            var hasEnoughMana = playerCurrentMana >= ability.ManaCost;
            var canUse = hasEnoughMana && isPlayerTurn;
            // Create a new AbilityOption with updated IsEnabled
            var updatedAbility = ability with { IsEnabled = canUse };
            updatedAbilities.Add(updatedAbility);
        }

        // Update the collection
        _abilities.Clear();
        foreach (var ability in updatedAbilities) {
            _abilities.Add(ability);
        }

        if (isMultiplayer && !isPlayerTurn) {
            SetStatus("Waiting for opponent's turn...");
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
