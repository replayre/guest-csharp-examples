<script lang="ts">
  import { MatchState } from '$lib/types/events';
  import { dmStore } from '../stores/dm.svelte';
  import { fade } from 'svelte/transition';

  function formatTime(seconds: number): string {
    const total = Math.max(0, Math.floor(seconds));
    const m = Math.floor(total / 60);
    const s = total % 60;

    return m > 0 ? `${m}:${s.toString().padStart(2, '0')}` : `${s}s`;
  }
</script>

{#if dmStore.state && dmStore.state.matchState != MatchState.Uninitialized}
  {@const state = dmStore.state}
  {#if state.matchState == MatchState.Countdown}
    <div
      class="pointer-events-none fixed top-1/2 left-1/2 z-40 -translate-x-1/2 -translate-y-1/2 text-center select-none"
    >
      {#key state.countdownRemainingSeconds}
        <div
          class="hud-value text-8xl leading-none font-extrabold text-cyber-cyan"
          style="text-shadow: 0 0 30px #00f0ff, 0 0 60px #00f0ff50; animation: cyber-scale-in 0.15s ease-out"
        >
          {state.countdownRemainingSeconds > 0 ? state.countdownRemainingSeconds : 'GO!'}
        </div>
      {/key}
    </div>
  {/if}

  {#if state.matchState == MatchState.Active}
    {#if state.outsideArena}
      <div
        class="pointer-events-none fixed inset-0 z-[999] flex items-center justify-center bg-black/40 backdrop-blur-[2px]"
        transition:fade={{ duration: 200 }}
      >
        <div class="hud-panel px-5 py-4 text-center text-cyber-text">
          <div
            class="mb-2 text-xl font-bold tracking-wider text-cyber-red uppercase"
            style="text-shadow: 0 0 8px #ff2040"
          >
            Return to Arena
          </div>

          <div class="mb-3 text-xs text-cyber-text-dim">You are outside the playable area</div>

          <div
            class="hud-value text-3xl font-extrabold text-cyber-red"
            style={state.outsideArenaRemainingTime <= 3
              ? 'animation: cyber-flash 0.5s ease-in-out infinite'
              : ''}
          >
            {state.outsideArenaRemainingTime}s
          </div>
        </div>
      </div>
    {/if}

    <div
      class="pointer-events-none fixed top-1/2 right-6 z-40 -translate-y-1/2"
      style="animation: cyber-slide-in-right 0.25s ease-out both"
    >
      <div class="hud-panel min-w-[200px] px-5 py-3 text-cyber-text">
        <div class="hud-label">
          {state.matchTypeName}
        </div>

        <div
          class="hud-value mb-3 text-2xl leading-tight font-extrabold transition-colors duration-200 {state.matchRemainingSeconds <=
          10
            ? 'text-cyber-red'
            : 'text-cyber-cyan'}"
          style={state.matchRemainingSeconds <= 10
            ? 'animation: cyber-flash 0.6s ease-in-out infinite'
            : ''}
        >
          {formatTime(state.matchRemainingSeconds)}
        </div>

        <div class="space-y-1 text-sm">
          {#each state.leaderboard ?? [] as entry, i}
            <div class="flex justify-between">
              <span class={i === 0 ? 'font-extrabold text-white' : ''}>
                {entry.name}
              </span>
              {#if entry.score != null}
                <span class="hud-value">{entry.score}</span>
              {/if}
            </div>
          {/each}
        </div>
      </div>
    </div>
  {/if}

  {#if state.matchState == MatchState.Ending}
    <div
      class="pointer-events-none fixed inset-0 z-50 flex items-center justify-center"
      style="animation: cyber-scale-in 0.3s ease-out both"
    >
      <div class="hud-panel min-w-[300px] px-8 py-6 text-center text-cyber-text">
        {#if state.leaderboard?.length > 0}
          <div class="hud-label">Winner</div>
          <div
            class="mb-6 text-3xl font-extrabold tracking-wider text-white uppercase"
            style="text-shadow: 0 0 12px #00f0ff"
          >
            {state.leaderboard[0].name}
          </div>
        {/if}

        <div class="space-y-2 text-lg">
          {#each state.leaderboard ?? [] as entry, i}
            <div class="flex justify-between">
              <span class={i === 0 ? 'font-extrabold text-white' : ''}>
                {entry.name}
              </span>
              {#if entry.score != null}
                <span class="hud-value">{entry.score}</span>
              {/if}
            </div>
          {/each}
        </div>
      </div>
    </div>
  {/if}
{/if}
