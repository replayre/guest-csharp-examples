<script lang="ts">
  import { votingStore, submitVote } from '../stores/voting.svelte';
  import { fade } from 'svelte/transition';

  let isSubmitting = $state(false);
  let showWinner = $state(false);

  $effect(() => {
    if (votingStore.isCompleted && votingStore.winningGameModeId) {
      showWinner = true;
      // Hide winner message after 5 seconds
      const timeout = setTimeout(() => {
        showWinner = false;
      }, 5000);
      return () => clearTimeout(timeout);
    } else {
      showWinner = false;
    }
  });

  async function handleVote(gameModeId: string) {
    votingStore.selectGameMode(gameModeId);

    try {
      isSubmitting = true;
      await submitVote();
    } catch (error) {
      console.error('[Voting UI] Failed to submit vote:', error);
    } finally {
      isSubmitting = false;
    }
  }

  function formatTime(ms: number): string {
    const seconds = Math.ceil(ms / 1000);
    return `${seconds}s`;
  }

  const winnerName = $derived(
    votingStore.gameModes.find((gm) => gm.id === votingStore.winningGameModeId)?.name || 'Unknown'
  );

  const selectedName = $derived(
    votingStore.gameModes.find((gm) => gm.id === votingStore.selectedGameModeId)?.name
  );
</script>

{#if votingStore.isActive}
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/30 select-none"
    transition:fade={{ duration: 200 }}
  >
    <div
      class="hud-panel mx-4 w-full max-w-2xl p-6"
      style="animation: cyber-scale-in 0.3s ease-out both"
    >
      <div class="mb-6">
        <h2 class="mb-2 text-xl font-bold tracking-wider text-cyber-cyan uppercase">
          Select Game Mode
        </h2>
        <div class="flex items-center justify-between">
          <p class="text-xs text-cyber-text-dim">Choose your preferred game mode</p>
          {#if votingStore.timerExpired}
            <div
              class="hud-value text-lg font-bold text-cyber-red"
              style="animation: cyber-text-glow 0.8s ease-in-out infinite; --glow-color: #ff2040"
            >
              {formatTime(votingStore.remainingMs)}
            </div>
          {:else}
            <div class="hud-value text-lg font-bold text-cyber-cyan">
              {formatTime(votingStore.remainingMs)}
            </div>
          {/if}
        </div>
      </div>

      <div class="mb-8 space-y-3">
        {#each votingStore.gameModes as gameMode (gameMode.id)}
          {@const isWinner =
            votingStore.isCompleted && gameMode.id === votingStore.winningGameModeId}
          {@const isSelected = votingStore.selectedGameModeId === gameMode.id}
          <button
            onclick={() => handleVote(gameMode.id)}
            disabled={votingStore.timerExpired || isSubmitting || votingStore.isCompleted}
            class={`w-full rounded-sm border p-3 text-left transition-all duration-150 ${
              isWinner
                ? 'winner-flash border-cyber-cyan bg-cyber-cyan/15 text-cyber-text'
                : isSelected
                  ? 'border-cyber-cyan bg-cyber-cyan/15 text-cyber-text'
                  : 'border-cyber-cyan-dim/30 bg-cyber-bg text-cyber-text hover:border-cyber-cyan hover:bg-cyber-cyan/10'
            } ${(votingStore.timerExpired || isSubmitting || votingStore.isCompleted) && 'cursor-not-allowed opacity-40'}`}
            style={isSelected && !isWinner ? 'filter: drop-shadow(0 0 8px #00f0ff50)' : ''}
          >
            <div class="flex items-start gap-3">
              {#if gameMode.icon}
                {gameMode.icon}
              {/if}
              <div class="flex-1">
                <h3 class="text-base font-bold tracking-wide uppercase">
                  {gameMode.name}
                  {#if isWinner}
                    <span class="hud-label ml-2 text-cyber-cyan">[WINNER]</span>
                  {/if}
                </h3>
                {#if gameMode.description}
                  <p class="mt-1 text-xs tracking-normal text-cyber-text-dim normal-case">
                    {gameMode.description}
                  </p>
                {/if}
              </div>
            </div>
          </button>
        {/each}
      </div>

      <div class="text-center text-sm">
        {#if votingStore.isCompleted}
          <p class="winner-text text-cyber-cyan">
            Voting completed. Winner: <strong class="text-cyber-cyan">{winnerName}</strong>
          </p>
        {:else if votingStore.timerExpired}
          <p class="text-cyber-cyan">Time's up. Waiting for results…</p>
        {:else if votingStore.selectedGameModeId && isSubmitting}
          <p class="text-cyber-cyan">Submitting vote…</p>
        {:else if votingStore.selectedGameModeId}
          <p class="text-cyber-text-dim">
            Selected: <strong>{selectedName}</strong>
          </p>
        {:else}
          <p class="text-cyber-text-dim">Select a game mode to vote</p>
        {/if}
      </div>
    </div>
  </div>
{/if}

{#if showWinner && votingStore.winningGameModeId}
  <div
    class="pointer-events-none fixed inset-0 z-[60] flex items-center justify-center"
    transition:fade={{ duration: 300 }}
  >
    <div class="winner-announcement">
      <div class="winner-text text-4xl font-extrabold tracking-widest text-cyber-cyan uppercase">
        {winnerName} WINS!
      </div>
    </div>
  </div>
{/if}

<style>
  .winner-flash {
    animation: cyber-glow-pulse 1s ease-in-out infinite;
    --glow-color: #00f0ff;
  }

  .winner-text {
    animation: cyber-text-glow 1s ease-in-out infinite;
    --glow-color: #00f0ff;
  }

  .winner-announcement {
    animation:
      cyber-scale-in 0.3s ease-out forwards,
      cyber-text-glow 1.2s 0.3s ease-in-out infinite;
    --glow-color: #00f0ff;
  }
</style>
