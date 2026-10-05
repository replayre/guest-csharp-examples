<script lang="ts">
  import { raceStore } from '../stores/race.svelte';
  import { fade } from 'svelte/transition';

  function formatTime(seconds: number): string {
    const total = Math.max(0, Math.floor(seconds));
    const m = Math.floor(total / 60);
    const s = total % 60;

    return m > 0 ? `${m}:${s.toString().padStart(2, '0')}` : `${s}s`;
  }
</script>

{#if raceStore.isActive && raceStore.state}
  {@const state = raceStore.state}

  {#if !state.isDone}
    <div
      class="pointer-events-none fixed top-6 left-1/2 z-40 select-none"
      style="animation: cyber-slide-in-down 0.25s ease-out both"
    >
      <div class="hud-panel min-w-[280px] px-5 py-4 text-center text-cyber-text">
        <div class="hud-label mb-1">
          {state.trackName}
        </div>

        {#if state.isSprint}
          <div class="hud-label text-cyber-cyan">Sprint</div>
        {:else}
          <div class="hud-label">Lap</div>

          <div class="hud-value text-2xl leading-tight font-extrabold text-cyber-cyan">
            {Math.min(state.currentLap, state.totalLaps)}
            <span class="text-lg font-bold text-cyber-text-dim">
              / {state.totalLaps}
            </span>
          </div>
        {/if}

        {#if state.lapTimeSeconds > 0}
          <div
            class="mt-4 grid gap-6 text-sm"
            class:grid-cols-1={state.isSprint || state.bestLapTimeSeconds <= 0}
            class:grid-cols-2={!state.isSprint && state.bestLapTimeSeconds > 0}
            in:fade={{ duration: 200, delay: 50 }}
            out:fade={{ duration: 0 }}
          >
            <div class={state.isSprint || state.bestLapTimeSeconds <= 0 ? 'text-center' : ''}>
              <div class="hud-label">
                {state.isSprint ? 'Time' : 'Lap Time'}
              </div>
              <div class="hud-value text-lg font-bold text-cyber-text">
                {formatTime(state.lapTimeSeconds)}
              </div>
            </div>

            {#if !state.isSprint && state.bestLapTimeSeconds > 0}
              <div>
                <div class="hud-label">Best</div>
                <div class="hud-value text-lg font-bold text-cyber-cyan">
                  {formatTime(state.bestLapTimeSeconds)}
                </div>
              </div>
            {/if}
          </div>
        {:else}
          <div class="hud-label text-cyber-text-dim" out:fade={{ duration: 0 }}>
            Waiting for start
          </div>
        {/if}

        {#if state.endTime != null}
          <div in:fade={{ duration: 200, delay: 50 }} out:fade={{ duration: 0 }}>
            <div class="hud-label mt-3">Ending in</div>
            <div class="hud-value text-lg font-extrabold text-cyber-cyan">
              {formatTime(state.endTime)}
            </div>
          </div>
        {/if}
      </div>
    </div>
  {:else}
    <div
      class="pointer-events-none fixed inset-0 z-50 flex items-center justify-center"
      style="animation: cyber-scale-in 0.3s ease-out both"
    >
      <div class="hud-panel min-w-[300px] px-8 py-6 text-center text-cyber-text">
        {#if state.endTime != null}
          <div class="hud-label">Ending in</div>
          <div class="hud-value mb-6 text-lg font-extrabold text-cyber-cyan">
            {formatTime(state.endTime)}
          </div>
        {/if}

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
              <span class={i === 0 ? 'font-extrabold text-white' : 'text-cyber-text'}>
                {entry.name}
              </span>
              {#if entry.score != null}
                <span class="hud-value">{formatTime(entry.score)}</span>
              {/if}
            </div>
          {/each}
        </div>
      </div>
    </div>
  {/if}
{/if}
