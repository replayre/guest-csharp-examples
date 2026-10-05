<script lang="ts">
  import { chatStore, submitChatMessage } from '../stores/chat.svelte';
  import { votingStore } from '../stores/voting.svelte';
  import { fade } from 'svelte/transition';
  import { onMount } from 'svelte';

  let inputValue = $state('');
  let inputRef = $state<HTMLInputElement | null>(null);

  const hasOtherOverlay = $derived(votingStore.isActive);

  onMount(() => {
    function onGlobalKeydown(e: KeyboardEvent) {
      if (e.key === 'Enter' && !chatStore.state.isChatWindowOpenForInput) {
        e.preventDefault();
        chatStore.state.isChatWindowOpenForInput = true;
      }
    }
    window.addEventListener('keydown', onGlobalKeydown);
    return () => window.removeEventListener('keydown', onGlobalKeydown);
  });

  $effect(() => {
    if (chatStore.state.isChatWindowOpenForInput && inputRef) {
      const t = setTimeout(() => inputRef?.focus(), 50);
      return () => clearTimeout(t);
    }
  });

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault();
      const msg = inputValue.trim();
      const releaseFocus = !hasOtherOverlay;
      submitChatMessage(msg.length > 0 ? msg : null, releaseFocus);
      inputValue = '';
    } else if (e.key === 'Escape') {
      e.preventDefault();
      const releaseFocus = !hasOtherOverlay;
      submitChatMessage(null, releaseFocus);
      inputValue = '';
    }
  }

  const isOpen = $derived(
    chatStore.state.isChatWindowOpenForHistory || chatStore.state.isChatWindowOpenForInput
  );
  const visibleHistory = $derived(chatStore.state.history.slice(-8));
</script>

{#if isOpen}
  <div
    class="pointer-events-none fixed top-1/2 left-6 z-50 w-[360px] -translate-y-1/2 select-none"
    transition:fade={{ duration: 150 }}
  >
    {#if visibleHistory.length > 0}
      <div class="mb-2 flex flex-col gap-1">
        {#each visibleHistory as entry}
          <div class="text-sm text-cyber-text" style="text-shadow: 0 0 4px rgba(0,0,0,0.8)">
            <span class="hud-value font-bold text-cyber-cyan">{entry.author}</span><span
              class="text-cyber-text-dim">:</span
            >
            {entry.contents}
          </div>
        {/each}
      </div>
    {/if}

    {#if chatStore.state.isChatWindowOpenForInput}
      <div class="hud-panel pointer-events-auto flex items-center gap-2 px-3 py-2">
        <span class="hud-label text-[10px]">SAY</span>
        <input
          bind:this={inputRef}
          bind:value={inputValue}
          onkeydown={handleKeydown}
          type="text"
          class="flex-1 bg-transparent font-mono text-sm text-cyber-text outline-none select-auto placeholder:text-cyber-text-dim"
          placeholder="Type a message..."
          maxlength="256"
        />
      </div>
    {/if}
  </div>
{/if}
