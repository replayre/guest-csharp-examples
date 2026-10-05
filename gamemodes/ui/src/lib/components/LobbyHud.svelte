<script lang="ts">
  import { lobbyStore } from '../stores/lobby.svelte';
  import { fade } from 'svelte/transition';

  function formatTime(seconds: number): string {
    const total = Math.max(0, Math.floor(seconds));
    const m = Math.floor(total / 60);
    const s = total % 60;

    return `${m}:${s.toString().padStart(2, '0')}`;
  }
</script>

{#if lobbyStore.state && lobbyStore.state.isClientInLobbyMode}
  {@const state = lobbyStore.state}
  <div
    class="pointer-events-none fixed top-1/2 right-6 z-40 -translate-y-1/2"
    style="animation: cyber-slide-in-right 0.25s ease-out both"
  >
    <div class="hud-panel min-w-[240px] px-5 py-4 text-cyber-text">
      <div class="hud-label mb-1">replay.re Alpha Test Lobby</div>
      <div class="mb-1 text-xs text-cyber-text-dim">
        <span class="font-mono font-bold text-cyber-cyan">F8</span> - Console
      </div>
      <div class="mb-1 text-xs text-cyber-text-dim">
        <span class="font-mono font-bold text-cyber-cyan">Enter</span> - Chat
      </div>
      {#if lobbyStore.state.isGameModeJoinable}
        <div class="mb-1 text-xs text-cyber-text-dim">
          <span class="font-mono font-bold text-cyber-cyan">F</span> - Join existing Mode
        </div>
      {/if}
      {#if lobbyStore.state.isGameModeActive}
        <div in:fade={{ duration: 200, delay: 50 }} out:fade={{ duration: 0 }}>
          <div class="hud-label mb-1 text-cyber-red">Match in Progress</div>

          <div class="mb-1 text-base font-bold tracking-wide text-cyber-text uppercase">
            {state.currentGameMode}
          </div>

          {#if !lobbyStore.state.isGameModeJoinable}
            <div class="mb-3 text-xs text-cyber-text-dim">
              A game mode is currently active. You will be able to join once it is finished.
            </div>
          {:else}
            <div class="mb-3 text-xs text-cyber-text-dim">
              A game mode is currently active. This mode allows delayed joining.
            </div>
          {/if}

          <div class="mb-2 flex justify-between text-xs">
            <span class="hud-label">
              {state.currentGameModeNumberOfPlayers} PLAYER{state.currentGameModeNumberOfPlayers ===
              1
                ? ''
                : 'S'}
            </span>
            <span class="hud-label">Time left</span>
          </div>

          <div
            class="hud-value text-right text-2xl font-black transition-all duration-200"
            class:text-cyber-red={state.currentGameModeRemainingDuration <= 30}
            class:text-cyber-cyan={state.currentGameModeRemainingDuration > 30}
            style={state.currentGameModeRemainingDuration <= 10
              ? 'animation: cyber-flash 0.6s ease-in-out infinite'
              : ''}
          >
            {formatTime(state.currentGameModeRemainingDuration)}
          </div>
        </div>
      {/if}
    </div>
  </div>
{/if}
