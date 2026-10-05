<script lang="ts">
  import { roamingStore } from '../stores/roaming.svelte';

  function formatTime(seconds: number): string {
    const total = Math.max(0, Math.floor(seconds));
    const m = Math.floor(total / 60);
    const s = total % 60;

    return `${m}:${s.toString().padStart(2, '0')}`;
  }
</script>

{#if roamingStore.state && roamingStore.state.isActive}
  {@const state = roamingStore.state}
  {#if state.isDead}
    <div
      class="pointer-events-none fixed bottom-6 left-1/2 z-40 -translate-x-1/2 text-center select-none"
    >
      {#key state.timeUntilRespawn}
        <div
          class="hud-value text-2xl leading-none font-extrabold text-cyber-cyan"
          style="text-shadow: 0 0 30px #00f0ff, 0 0 60px #00f0ff50; animation: cyber-scale-in 0.15s ease-out"
        >
          {#if state.timeUntilRespawn >= 1}
            Respawn available in {state.timeUntilRespawn}
            {state.timeUntilRespawn > 1 ? 'seconds' : 'second'}
          {:else}
            Press [Space] to respawn
          {/if}
        </div>
      {/key}
    </div>
  {/if}

  {#if state.isHelpScreenEnabled}
    <div
      class="pointer-events-none fixed top-6 left-1/2 z-40 bg-black/30 select-none"
      style="animation: cyber-slide-in-down 0.25s ease-out both"
    >
      <div class="hud-panel min-w-[280px] px-5 py-4 text-center text-cyber-text">
        <div class="hud-label mb-1">Roaming Help, Tips and Tricks</div>
        <ul
          class="hud-value list-inside list-disc space-y-1 text-left text-sm font-bold text-cyber-text"
        >
          <li>All <span class="text-cyber-cyan">Fast Travel</span> stations are unlocked</li>
          <li>
            All <span class="text-cyber-cyan">Attributes</span> are maxed out and all
            <span class="text-cyber-cyan">Perks</span> are unlocked
          </li>
          <li>
            You can equip Cyberware <span class="text-cyber-cyan">directly from Inventory</span>
          </li>
          <li>
            You can only damage or receive damage from other players if you <span
              class="text-cyber-cyan">enable combat</span
            >
          </li>
        </ul>
      </div>
    </div>
  {/if}

  <div
    class="pointer-events-none fixed top-1/2 right-6 z-40 -translate-y-1/2"
    style="animation: cyber-slide-in-right 0.25s ease-out both"
  >
    <div class="hud-panel min-w-[240px] px-5 py-4 text-cyber-text">
      <div class="hud-label mb-1">Roaming Mode</div>
      <div class="mb-1 text-xs text-cyber-text">
        <span class="font-mono font-bold text-cyber-cyan">6</span> - Toggle Help Screen [<span
          class="font-mono font-bold text-cyber-cyan"
          >{state.isHelpScreenEnabled ? 'ON' : 'OFF'}</span
        >]
      </div>
      <div class="mb-1 text-xs text-cyber-text">
        <span class="font-mono font-bold text-cyber-cyan">7</span>
        - Customize Character [<span
          class="font-mono font-bold text-cyber-cyan"
          class:text-cyber-red={!state.isCharacterCustomizationAvailable}
          class:text-cyber-cyan={state.isCharacterCustomizationAvailable}
          >{state.isCharacterCustomizationAvailable ? 'Available' : 'Blocked'}</span
        >]
      </div>
      <div class="mb-1 text-xs text-cyber-text">
        <span class="font-mono font-bold text-cyber-cyan">8</span> - Spawn Random Vehicle [<span
          class="font-mono font-bold text-cyber-cyan"
          class:text-cyber-red={state.timeUntilVehicleSpawnCooldownEnds > 0}
          class:text-cyber-cyan={state.timeUntilVehicleSpawnCooldownEnds <= 0}
          >{state.timeUntilVehicleSpawnCooldownEnds > 0
            ? formatTime(state.timeUntilVehicleSpawnCooldownEnds)
            : 'Available'}</span
        >]
      </div>
      <div class="mb-1 text-xs text-cyber-text">
        <span class="font-mono font-bold text-cyber-cyan">9</span> - Toggle Combat [<span
          class="font-mono font-bold text-cyber-cyan"
          >{state.isCombatEnabled ? 'ON - ' : 'OFF - '}</span
        >
        <span
          class="font-mono font-bold text-cyber-cyan"
          class:text-cyber-red={state.timeUntilCombatToggleCooldownEnds > 0}
          class:text-cyber-cyan={state.timeUntilCombatToggleCooldownEnds <= 0}
          >{state.timeUntilCombatToggleCooldownEnds > 0
            ? formatTime(state.timeUntilCombatToggleCooldownEnds)
            : 'Available'}</span
        >]
      </div>
      <div class="mt-4 mb-2 flex justify-between text-xs">
        <span class="hud-label">Time left</span>
      </div>
      <div
        class="hud-value text-right text-2xl font-black transition-all duration-200"
        class:text-cyber-red={state.timeUntilModeEnds <= 30}
        class:text-cyber-cyan={state.timeUntilModeEnds > 30}
        style={state.timeUntilModeEnds <= 10
          ? 'animation: cyber-flash 0.6s ease-in-out infinite'
          : ''}
      >
        {formatTime(state.timeUntilModeEnds)}
      </div>
    </div>
  </div>
{/if}
